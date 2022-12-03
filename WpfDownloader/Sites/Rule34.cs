using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Rule34 : Site
    {
        private static readonly List<Tuple<string, string>> HEADERS =
            new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36 Edg/99.0.1150.55"),
                new Tuple<string, string>("Referer", "https://rule34.xxx")
            };

        private string _escTags;
        private string _newPath;

        public Rule34(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            Uri myUri = new Uri(Url);
            _escTags = HttpUtility.ParseQueryString(myUri.Query).Get("tags");
            _escTags = Uri.UnescapeDataString(_escTags).Trim();

            entry.StatusMsg = "Retrieving";
            entry.Name = "[Rule34] " + _escTags;

            var entries = await GetMediaUrls(entry);
           
            _escTags = RemoveIllegalChars(_escTags);
            _newPath = $"{DEFAULT_PATH}/rule34/{_escTags}";

            await DownloadUtil.DownloadAllUrls(entries, _newPath, entry, headers: HEADERS);

            //if (_captionsContainer.Any())
            //{
            //    entry.StatusMsg = "Write -> Captions";
            //    await CaptionUtil.WriteCaptionsDanbooru(_captionsContainer);

            //    entry.StatusMsg = "Finished";
            //}
        }

        public async Task<IEnumerable<string>> GetMediaUrls(UrlEntry entry)
        {
            var urls = new List<string>();
            int currPid = 0;
            while (true)
            {
                if (entry.CancelToken.IsCancellationRequested)
                {
                    entry.CancelToken.ThrowIfCancellationRequested();
                    break;
                }

                var url = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&tags={_escTags}&pid={currPid}&json=1";
                var jsonStr = await Requests.GetStr(url, HEADERS, entry.CancelToken);

                var data = JsonParser.Parse(jsonStr);
                if (!data.Any()) break;
                

                //var matches = new Regex("file_url=\"(.*?)\"", RegexOptions.Singleline).Matches(data);
                urls.AddRange(data.Select(post => post["file_url"].Value));

                currPid++;
                await Task.Delay(500);
            }

            return urls;
        }
    }
}
