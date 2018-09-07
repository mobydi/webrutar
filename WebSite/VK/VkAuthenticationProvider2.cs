using System.Threading.Tasks;
using Duke.Owin.VkontakteMiddleware.Provider;
using Microsoft.WindowsAzure;
using RuTarCommon;
using System.Security.Claims;

namespace WebSite
{
    public class VkAuthenticationProvider2 : VkAuthenticationProvider
    {
        public override async Task Authenticated(VkAuthenticatedContext context)
        {
            context.Identity.AddClaim(new Claim("token", context.AccessToken));
            await base.Authenticated(context);
        }

        public override Task ReturnEndpoint(VkReturnEndpointContext context)
        {
            return base.ReturnEndpoint(context);
        }
    }
}