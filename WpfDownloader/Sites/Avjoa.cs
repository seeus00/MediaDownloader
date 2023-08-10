using Azure.Core;
using Downloader.Util;
using MonoTorrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WpfDownloader.Config;
using WpfDownloader.Util.UserAgent;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Avjoa : Site
    {
        private List<Tuple<string, string>> HEADERS =
            new List<Tuple<string, string>>()
            {

                    new Tuple<string, string>("User-Agent", UserAgentUtil.CURR_USER_AGENT),
                    new Tuple<string, string>("Accept-Encoding", "identity"),
            };

        public Avjoa(string url, string args): base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving video";
            string html = await Requests.GetStr(Url);

            string title = new Regex("property=\"og:title\" content=\"(.*?)\"", RegexOptions.Singleline)
                .Match(html).Groups[1].Value;

            string coverUrl = new Regex("property=\"og:image\" content=\"(.*?)\"", RegexOptions.Singleline)
               .Match(html).Groups[1].Value;

            string videoUrl = new Regex("contentURL.*?content=\"(.*?)\"", RegexOptions.Singleline)
                .Match(html).Groups[1].Value;

            string code = videoUrl.Split('/').Last().Split('.')[0];
            string newPath = $"{DEFAULT_PATH}/Avjoa/{RemoveIllegalChars(code)}";

            string videoHostDomain = new Uri(videoUrl).Host;
            string urlHostDomain = new Uri(Url).Host;
            HEADERS.Add(new Tuple<string, string>("Referer", $"https://{urlHostDomain}"));
            HEADERS.Add(new Tuple<string, string>("Host", videoHostDomain));

            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
            
            entry.DownloadPath = newPath;
            entry.Name = $"[Avjoa] {code}";

            entry.StatusMsg = "Downloading cover";
            await Requests.DownloadFileFromUrl(coverUrl, newPath, cancelToken: entry.CancelToken, entry: entry);

            entry.StatusMsg = "Downloading video";
            await Requests.DownloadFileFromUrl(videoUrl, newPath, headers: HEADERS, cancelToken: entry.CancelToken, entry: entry);


            entry.StatusMsg = "Finished";
        }
    }
}
