using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Imgur : Site
    {
        private string _title;
        private string _slugId;

        public Imgur(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = "[Imgur] " + _slugId;

            var mediaUrls = await GetMediaUrls();
            var newPath = $"{DEFAULT_PATH}/imgur/{_title} - {_slugId}";

            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, 
                entry, fileNameNumber: true);
        }

        public async Task<IEnumerable<string>> GetMediaUrls()
        {
            string imgurId = Url.Split('/').Last();
            var jsonStr = await Requests.GetStr($"https://cubari.moe/read/api/imgur/series/{imgurId}");
            
            var data = JsonParser.Parse(jsonStr);

            _title = data["title"].Value;
            _slugId = data["slug"].Value;

            return data["chapters"]["1"]["groups"]["1"].Select(img => img["src"].Value);
        }
    }
}
