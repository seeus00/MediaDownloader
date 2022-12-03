using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class FourChan : Site
    {
        private string _threadName;
        private string _board;
        private string _threadId;

        private string _threadTitle;

        public FourChan(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            var split = Url.Split('/');
            var threadId = split.Last();

            var mediaUrls = await GetMediaUrls();
            var newPath = (string.IsNullOrEmpty(_threadTitle)) ? 
                $"{DEFAULT_PATH}/4chan/{_board}/{threadId}" : $"{DEFAULT_PATH}/4chan/{_board}/{threadId} - {_threadTitle}";

            entry.StatusMsg = "Retrieving";
            entry.Name = "[4Chan] " + threadId;

            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry);
        }

        public async Task<IEnumerable<string>> GetMediaUrls()
        {
            _board = Url.Split('/')[3];
            string jsonUrl = Url + ".json";
            string jsonStr = await Requests.GetStr(jsonUrl);

            var data = JsonParser.Parse(jsonStr);
            var subVal = data["posts"].First()["sub"];

            _threadTitle = (subVal != null) ? RemoveIllegalChars(subVal.Value) : string.Empty;

            //_threadName = data["posts"].First()["sub"].Value;
            return 
                data["posts"]
                .Where(post => post["ext"] != null)
                .Select(post => $"https://i.4cdn.org/{_board}/{post["tim"].Value}{post["ext"].Value}"); 

            //return new Regex("fileThumb\" href=\"(.*?)\"")
            //    .Matches(html)
            //    .Select(match => "https:" + match.Groups[1].Value);
        }
    }
}
