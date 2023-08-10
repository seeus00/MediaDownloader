using ChromeCookie;
using Downloader.Data;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.Util.StringHelpers;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class KemonoParty : Site
    {
        private static readonly string POSTS_API = "https://{0}.party/api/{1}/user/{2}?o={3}";
        private static readonly string CREATOR_API = "https://{0}.party/api/creators";
        private static readonly int DEFAULT_POST_LIMIT = 25;

        private static readonly List<Tuple<string, string>> HEADERS =
            new List<Tuple<string, string>>
        {
            new Tuple<string, string>("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36"),
        };
        private static readonly List<Tuple<string, string>> IMG_HEADERS =
            new List<Tuple<string, string>>
        {
            new Tuple<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36"),
            new Tuple<string, string>("Accept-Encoding", "br")
        };

        private static CookieContainer _cookieContainer = null;

        private string _domain;
        private string _service;
        private string _userId;
        private string _userName;

        public KemonoParty(string url, string args) : base(url, args)
        {
            var split = url.Split('/');
            _domain = split[2].Split('.')[0];
            _service = split[3];
            _userId = split.Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = $"[{_domain}] " + _userId;
            var mediaUrls = await GetMediaUrls(entry);

            var newPath = $"{DEFAULT_PATH}/{_domain}/{_userName}";

            if (IMG_HEADERS.Count < 3)
                IMG_HEADERS.Add(new Tuple<string, string>("referer", $"https://{_domain}.party"));
            
            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry, headers: IMG_HEADERS);
        }

        //For some reason, the kemono + coomer party api appends a newline character
        //at the end of the string. The trimEnd removes it in order for it to be 
        //properly parsed by my json parser
        public async Task<IEnumerable<string>> GetMediaUrls(UrlEntry entry)
        {
            if (_cookieContainer == null)
            {
                var baseAddress = new Uri($"https://www.{_domain}.party/");
                _cookieContainer = new CookieContainer();

                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies($".{_domain}.party"));
                Requests.AddCookies(_cookieContainer, baseAddress);

                var cookies = _cookieContainer.GetCookies(baseAddress);
            }

            

            string creatorsText = (await Requests.GetStr(string.Format(CREATOR_API, _domain)))
                .TrimEnd('\n');          
            var creatorData = JsonParser.Parse(creatorsText);

            _userName = UnicodeUtil.DecodeEncodedNonAsciiCharacters(creatorData
                .Where(creator => creator["id"].Value == _userId)
                .Select(creator => creator["name"].Value).FirstOrDefault(""));

            entry.Name = $"[{_domain}] " + _userName;

            var mediaUrls = new List<string>();
            int currPg = 0;
            while (true)
            {
                string getUrl = string.Format(POSTS_API, _domain, _service, _userId, currPg);
                //Strip newline at end of string
                string jsonStr = (await Requests.GetStr(getUrl, HEADERS))
                    .TrimEnd('\n');

                var data = JsonParser.Parse(jsonStr);

                if (!data.Any())
                    break;

                foreach (var post in data)
                {
                    if (!post["attachments"].IsEmpty())
                    {
                        mediaUrls.AddRange(post["attachments"].Select(attach => 
                            $"https://{_domain}.party/data{attach["path"].Value}?f={attach["name"].Value}"
                        ));
                    }
                    if (!post["file"].IsEmpty())
                    {
                        var file = post["file"];
                        mediaUrls.Add($"https://{_domain}.party/data{post["file"]["path"].Value}?f={post["file"]["name"].Value}");
                    }
                }

                currPg += DEFAULT_POST_LIMIT;
            }

            return mediaUrls;
        }
    }
}
