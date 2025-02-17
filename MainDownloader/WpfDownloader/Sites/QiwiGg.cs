using Downloader.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class QiwiGg : Site
    {
        private const string FILE_PROVIDER_HOST = "spyderrock.com";

        public QiwiGg(string url, string args) : base(url, args)
        {
            
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";

            var resp = await Requests.Get(Url);
            resp.EnsureSuccessStatusCode();


            string html = await resp.Content.ReadAsStringAsync();
            string title = 
                new Regex("page_TextHeading.*?children.*?\\\\\":\\\\\"(.*?)\\\\\"").Matches(html).Last().Groups[1].Value;

            entry.Name = $"[QiwiGg] {title}";

            string newPath = $"G:/qiwiGg/{RemoveIllegalChars(title)}";
            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

            var urls = new Regex("fileName\\\\\":\\\\\"(.*?)\\\\\".*?\\\\\"slug\\\\\":\\\\\"(.*?)\\\\\"")
                .Matches(html)
                .Select(match => new Downloader.Data.ImgData()
                {
                    Url = $"https://{FILE_PROVIDER_HOST}/{match.Groups[2].Value}.{match.Groups[1].Value.Split('.').Last()}",
                    Filename = match.Groups[1].Value
                });

            entry.StatusMsg = "Downloading";
            await DownloadUtil.DownloadAllUrls(urls, newPath, entry, delayInBetween: 500, maxThreads: 3);
        }
    }
}
