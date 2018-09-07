using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WorkerRole;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net;

namespace WebSite.Tests
{
    [TestClass]
    public class VkTest
    {
        [TestMethod]
        public async Task should_wallget_work()
        {
            var ownerId = "5029065";
            var accessToken = "674779bd8dafe589092aee5477fe171e6e62254822a745dbd33249da52d41ac4489d2d0550ed967a00e82";
            var offset = 0;
            var count = 100;
            string reqStr = string.Format("https://api.vkontakte.ru/method/wall.get?owner_id={0}&access_token={1}&offset={2}&count={3}&filter=owner", ownerId, accessToken, offset, count);

            var content = await new WebClient().DownloadStringTaskAsync(reqStr);
            dynamic result = await JsonConvert.DeserializeObjectAsync(content);


            if (result["error"] != null)
            {
                var code = result.error.error_code;
                var msg = result.error.error_msg;
                throw new VkException(code, msg);
            }

            int pcount = result.response.First;
            IEnumerable<JToken> posts = result.response.Children();
            foreach (dynamic p in posts.Skip(1))
            {
                var id = p.id;
                var text = p.text;
                var likes = p.likes.count;
            }
        }
    }
}
