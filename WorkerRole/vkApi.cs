using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole
{
    public class VkApi : IDisposable
    {
        private WebClient webClient = new WebClient();

        public async Task<Wall> WallGet(string ownerId, string accessToken, int offset, int count)
        {
            string reqStr = string.Format("https://api.vkontakte.ru/method/wall.get?owner_id={0}&access_token={1}&offset={2}&count={3}&filter=owner", ownerId, accessToken, offset, count);

            var content = await webClient.DownloadStringTaskAsync(reqStr);
            dynamic json = await JsonConvert.DeserializeObjectAsync(content);

            if (json["error"] != null)
            {
                var code = json.error.error_code;
                var msg = json.error.error_msg;
                throw new VkException(code, msg);
            }
            
            int pcount = json.response.First;
            IEnumerable<JToken> posts = json.response.Children();
            
            List<Wall.Post> list = new List<Wall.Post>();
            foreach (dynamic p in posts.Skip(1))
            {
                int id = p.id;
                string text = p.text;
                int likes = p.likes.count;
                list.Add(new Wall.Post(id, text, likes));
            }
            Wall wall = new Wall(pcount, list);

            return await Task.FromResult<Wall>(wall);
        }

        public void Dispose()
        {
            webClient.Dispose();
        }
    }

    public class Wall
    {
        public int AllPostsCount { get; private set; }
        public List<Post> Posts {get; private set;}
        public Wall(int allPostsCount, List<Post> posts)
        {
            AllPostsCount = allPostsCount;
            Posts = posts;
        }

        public class Post 
        {
            public int Id { get; private set; }
            public string Text { get; private set; }
            public int Likes { get; private set; }

            public Post(int id, string text, int likes)
            {
                Id = id;
                Text = text;
                Likes = likes;
            }
        }
    }

    public class VkException : Exception
    {
        public string Code { get; private set; }

        public VkException(string code, string msg) : base(msg)
        {
            Code = code;
        }
    }
}
