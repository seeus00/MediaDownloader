using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class EightMuses : Site
    {
        private string _title;

        public EightMuses(string url, string args) : base(url, args)
        {
            _title = Url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = "[8Muses Comics] " + _title;
            var mediaUrls = await GetMediaUrls();

            var newPath = $"{DEFAULT_PATH}/8muses/{_title}";
            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry, 
                fileNameNumber: true);
        }

        public async Task<IEnumerable<string>> GetMediaUrls()
        {
            string html = await Requests.GetStr(Url);
            var imgs = new Regex("lazyload.*?data-src=\"/image/.*?/(.*?)\"")
                .Matches(html)
                .Select(m => $"https://comics.8muses.com/image/fm/{m.Groups[1].Value}");

            return imgs;
        }
    }
}
