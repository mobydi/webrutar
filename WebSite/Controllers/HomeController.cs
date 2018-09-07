using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using RuTarCommon;
using WebSite.Models;

namespace WebSite.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ReportContext db = ContextFactory.GetReportContext();
        private CloudQueue reportBuilderQueue;
        private ApplicationUserManager userManager;

        public HomeController()
        {
            InitializeStorage();
        }

        public HomeController(ApplicationUserManager userManager)
            : this()
        {
            this.userManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get { return userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { userManager = value; }
        }

        private void InitializeStorage()
        {
            // Open storage account using credentials from .cscfg file.
            var storageAccount =
                CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

            // Get context object for working with queues, and 
            // set a default retry policy appropriate for a web user interface.
            var queueClient = storageAccount.CreateCloudQueueClient();
            queueClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

            // Get a reference to the queue.
            reportBuilderQueue = queueClient.GetQueueReference("report");
        }

        [AllowAnonymous]
        public async Task<ActionResult> Index()
        {
            var reports = db.Reports.AsQueryable().OrderByDescending(k => k.ReportId);
            return View(await reports.ToListAsync());
        }

        [Authorize]
        public async Task<ActionResult> Create()
        {
            var userId = User.Identity.GetUserId();
            var claims = await UserManager.GetClaimsAsync(userId);
            var ctoken = claims.FirstOrDefault(x => x.Type == "token");
            Debug.Assert(ctoken != null);
            var token = ctoken.Value;

            var newReport = new Report
            {
                CurrentOffset = 0,
                Title = "Report for user " + User.Identity.Name,
                AccessToken = token,
                UserId = userId,
                State = "Processing",
                Finished = false
            };

            db.Reports.Add(newReport);
            await db.SaveChangesAsync();

            var queueMessage = new CloudQueueMessage(newReport.ReportId.ToString());
            await reportBuilderQueue.AddMessageAsync(queueMessage);
            Trace.TraceInformation("Created queue message for User {0}", userId);

            return View();
        }

        [Authorize]
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var report = await db.Reports.FindAsync(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            if (report.UserId != User.Identity.GetUserId())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            var top =
                await db.ReportItemMaxs.AsQueryable().Where(_ => _.ReportId == id)
                        .OrderByDescending(_ => _.LikesCount)
                        .FirstOrDefaultAsync();
            if (top == null)
            {
                return HttpNotFound();
            }
            var url = string.Format("http://vk.com/id{0}?w=wall{0}_{1}", report.UserId, top.PostId);
            return View(new DetailsModel(report, url));
        }

        [Authorize]
        public async Task<ActionResult> Chart(int? id)
        {
            var report = await db.Reports.FindAsync(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            if (report.UserId != User.Identity.GetUserId())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            if (id == null)
            {
                return HttpNotFound();
            }

            var reportItems = await db.ReportItems.AsQueryable()
                .Where(a => a.ReportId == id).ToListAsync();

            var xs = new List<int>();
            var ys = new List<int>();
            foreach (var item in reportItems.Distinct(ReportItemsEqualityComparer.Instance))
            {
                xs.Add(item.PostLength);
                ys.Add(item.LikesCount);
            }

            return View(new ChartModel(xs, ys));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}