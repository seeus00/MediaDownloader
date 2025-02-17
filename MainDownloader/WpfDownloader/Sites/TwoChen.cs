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
    public class TwoChen : Site
    {
        private string _board;
        private string _threadId;


        public TwoChen(string url, string args) : base(url, args)
        {
            var split = Url.Split('/');
            _board = split[3];
            _threadId = split[4];
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = "[2Chen] " + _threadId;

            string path = $"{DEFAULT_PATH}/2chen/{_board}/{_threadId}";
            var mediaUrls = await GetMediaUrls();

            await DownloadUtil.DownloadAllUrls(mediaUrls, path, entry);
        }

        public async Task<IEnumerable<string>> GetMediaUrls()
        {
            string html = await Requests.GetStr(Url);
            return new Regex("fileinfo.*?a.*?href=\"(.*?)\"").Matches(html)
                .Skip(1)
                .Select(match => $"https://2chen.moe{match.Groups[1].Value}");
        }
    }
}
