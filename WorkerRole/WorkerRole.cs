using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using RuTarCommon;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private const int MaxFetchCount = 90;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private readonly VkApi vkApi = new VkApi();
        private CloudQueue reportsQueue;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole is running");

            try
            {
                RunAsync(cancellationTokenSource.Token).Wait();
            }
            finally
            {
                runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            // Open storage account using credentials from .cscfg file.
            var storageAccount = CloudStorageAccount.Parse
                (RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

            // Create the queue client.
            var queueClient = storageAccount.CreateCloudQueueClient();
            reportsQueue = queueClient.GetQueueReference("report");
            reportsQueue.CreateIfNotExists();

            var result = base.OnStart();

            Trace.TraceInformation("WorkerRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole is stopping");

            cancellationTokenSource.Cancel();
            runCompleteEvent.WaitOne();
            vkApi.Dispose();

            base.OnStop();
            Trace.TraceInformation("WorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                var tasks = new List<Task>(5);
                foreach (var message in reportsQueue.GetMessages(5, TimeSpan.FromMinutes(5)))
                {
                    var tmp = message;
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessQueueMessage(tmp);
                        }
                        catch (AggregateException ex)
                        {
                            Trace.TraceError("Error processing task: {0}", ex);
                        }
                        finally
                        {
                            try
                            {
                                reportsQueue.DeleteMessageAsync(tmp).Wait();
                            }
                            catch (AggregateException ex)
                            {
                                Trace.TraceError("Error deleting task: {0}", ex);
                            }
                        }
                    });
                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray());
                sw.Stop();
                int towait = (1000/5*tasks.Count) - (int)sw.ElapsedMilliseconds;
                if (towait < 0)
                    towait = 1;
                await Task.Delay(towait, cancellationToken);
            }
        }

        private async Task ProcessQueueMessage(CloudQueueMessage msg)
        {
            Trace.TraceInformation("Processing queue message {0}", msg);

            using (var db = ContextFactory.GetReportContext())
            {
                var reportId = int.Parse(msg.AsString);
                var report = db.Reports.Find(reportId);
                if (report == null)
                {
                    Trace.TraceError(string.Format("Report {0} not found", reportId));
                    return;
                }
                if (report.Finished) return;
                try
                {
                 
                    var max = new ReportItemMax
                    {
                        ReportId = reportId,
                        LikesCount = -1,
                        PostLength = -1,
                        PostId = -1
                    };

                    var wall = await vkApi.WallGet(report.UserId, report.AccessToken, report.CurrentOffset, MaxFetchCount);
                    foreach (var p in wall.Posts)
                    {
                        var postLength = p.Text.Length;
                        var newItem = new ReportItem
                        {
                            ReportId = reportId,
                            LikesCount = p.Likes,
                            PostLength = postLength,
                            PostId = p.Id
                        };

                        db.ReportItems.Add(newItem);

                        if (max.LikesCount < p.Likes)
                        {
                            max.LikesCount = p.Likes;
                            max.PostLength = postLength;
                            max.PostId = p.Id;
                        }
                    }

                    report.CurrentOffset += wall.Posts.Count();
                    if (report.CurrentOffset >= wall.AllPostsCount)
                    {
                        report.State = "Succeed";
                        report.Finished = true;
                    }
                    else
                    {
                        report.State = "Waiting for next chunk";
                    }

                    db.ReportItemMaxs.Add(max);
                    await db.SaveChangesAsync();

                    if (!report.Finished)
                    {
                        var queueMessage = new CloudQueueMessage(reportId.ToString());
                        await reportsQueue.AddMessageAsync(queueMessage);
                    }
                }
                catch (Exception ex)
                {
                    report.State = "Failed";
                    report.Finished = true;
                    db.SaveChanges();
                    Trace.TraceError("Processing queue message {0}", ex.Message);
                }
            }
        }
    }
}