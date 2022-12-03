using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Downloader.Sites
{
    public class Vsco : Site
    {
        private static readonly List<Tuple<string, string>> _headers = 
            new List<Tuple<string, string>>()
        {
                new Tuple<string, string>("Authorization", "Bearer 7356455548d0a1d886db010883388d08be84d0c9")
        };

        public Vsco(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll()
        {
            using var messageBar = new MessageBar();
            messageBar.PrintMsg("[Vsco] downloading", Url);

            var mediaUrls = await GetMediaUrls();
            var name = Url.Split('/')[3];
            var newPath = $"{DEFAULT_PATH}/vsco/{name}";

            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, $"{name}", 
                messageBar, dulpicateFileName: true);
        }

        public async Task<IEnumerable<string>> GetMediaUrls()
        {
            var html = await Requests.GetStr(Url);
            var imgUrl =
                new Regex("property=\"og:image\" content=\"(.*?)\"").Match(html).Groups[1].Value;

            var siteId = imgUrl.Split('/')[5];
            var baseUrl = $"https://vsco.co/api/3.0/medias/profile?site_id={siteId}&limit=14";

            string jsonStr = await Requests.GetStr(baseUrl, _headers);
            var urls = new List<string>();
            while (true)
            {
                var data = JsonParser.Parse(jsonStr);
                var val = data["media"].First()["image"];
                urls.AddRange(data["media"]
                    .Where(img => img["type"].Value == "image")
                    .Select(img => "https://" + img["image"]["responsive_url"].Value));

                if (data["next_cursor"] == null) break;
                jsonStr = await Requests.GetStr($"{baseUrl}&cursor={HttpUtility.UrlEncode(data["next_cursor"].Value)}", 
                    _headers);
            }

            return urls;
        }
    }
}
