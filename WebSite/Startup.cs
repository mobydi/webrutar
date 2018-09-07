using Microsoft.Owin;
using Owin;
using WebSite;

[assembly: OwinStartup(typeof (Startup))]

namespace WebSite
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}