using Downloader.Util;
using MySql.Data.MySqlClient.Replication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.Util.UserAgent;
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

            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry, overrideDownloadedFiles: false);
        }

        public async Task<IEnumerable<string>> GetMediaUrls()
        {
            _board = Url.Split('/')[3];
            string jsonUrl = Url + ".json";

            var resp = await Requests.Get(jsonUrl);
            string locationUrl = resp.Headers.Location.AbsoluteUri;

            resp = await Requests.Get(locationUrl);
            locationUrl = resp.Headers.Location.AbsoluteUri;

            var headers = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("User-Agent", UserAgentUtil.CURR_USER_AGENT),
                new Tuple<string, string>("Host", "a.4cdn.org"),
            };

            string jsonStr = await Requests.GetStr(locationUrl);

            var data = JsonParser.Parse(jsonStr);
            var subVal = data["posts"].First()["sub"];

            _threadTitle = (subVal != null) ? RemoveIllegalChars(subVal.ToString()) : string.Empty;

            //_threadName = data["posts"].First()["sub"].ToString();
            return 
                data["posts"]
                .Where(post => post["ext"] != null)
                .Select(post => $"https://i.4cdn.org/{_board}/{post["tim"].ToDouble}{post["ext"]}"); 

            //return new Regex("fileThumb\" href=\"(.*?)\"")
            //    .Matches(html)
            //    .Select(match => "https:" + match.Groups[1].ToString());
        }
    }
}
