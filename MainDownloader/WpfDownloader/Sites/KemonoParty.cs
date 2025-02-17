using ChromeCookie;
using Downloader.Data;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.Util.StringHelpers;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class KemonoParty : Site
    {
        private const string POST_API = "https://{0}.su/api/v1/{1}/user/{2}/post/{3}";
        private const string POSTS_API = "https://{0}.su/api/v1/{1}/user/{2}?o={3}";
        private const string CREATOR_API = "https://{0}.su/api/v1/creators";
        private const int DEFAULT_POST_LIMIT = 50;

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

        private static CookieContainer cookieContainer = null;

        private string domain;
        private string service;
        private string userId;
        private string userName;

        private string newPath;

        public KemonoParty(string url, string args) : base(url, args)
        {
            var split = url.Split('/');
            domain = split[2].Split('.')[0];
            service = split[3];
            userId = split[5];
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = $"[{domain}] " + userId;

            if (cookieContainer == null)
            {
                var baseAddress = new Uri($"https://www.{domain}.su/");
                cookieContainer = new CookieContainer();

                cookieContainer.Add(baseAddress, ChromeCookies.GetCookies($".{domain}.su"));
                Requests.AddCookies(cookieContainer, baseAddress);
            }

            if (IMG_HEADERS.Count < 3)
                IMG_HEADERS.Add(new Tuple<string, string>("referer", $"https://{domain}.su"));

            if (Url.Contains("post"))
            {
                await DownloadSinglePost(entry);
            }else
            {
                var mediaUrls = await GetMediaUrls(entry);

                newPath = $"{DEFAULT_PATH}/{domain}/{userName}";
                await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry, headers: IMG_HEADERS);
            }

            entry.StatusMsg = (entry.CancelToken.IsCancellationRequested) ? "Cancelled" : "Finished";
        }


        public async Task DownloadSinglePost(UrlEntry entry)
        {
            string creatorsText = (await Requests.GetStr(string.Format(CREATOR_API, domain)))
                .TrimEnd('\n');
            var creatorData = JsonParser.Parse(creatorsText);

            userName = UnicodeUtil.DecodeEncodedNonAsciiCharacters(creatorData
                .Where(creator => creator["id"].ToString() == userId)
                .Select(creator => creator["name"].ToString()).FirstOrDefault(""));

            newPath = $"{DEFAULT_PATH}/{domain}/{userName}";
            entry.Name = $"[{domain}] " + userName;

            string postId = Url.Split('/').Last();

            string getUrl = string.Format(POST_API, domain, service, userId, postId);
            //Strip newline at end of string
            string jsonStr = (await Requests.GetStr(getUrl, HEADERS))
                .TrimEnd('\n');

            var data = JsonParser.Parse(jsonStr);
            if (!data.Any())
                return;


            var mediaUrls = new List<string>();
            var post = data.First();
            if (!post["attachments"].IsEmpty())
            {
                mediaUrls.AddRange(post["attachments"].Select(attach =>
                    $"https://{domain}.su/data{attach["path"].ToString()}?f={attach["name"].ToString()}"
                ));
            }
            if (!post["file"].IsEmpty())
            {
                var file = post["file"];
                mediaUrls.Add($"https://{domain}.su/data{post["file"]["path"].ToString()}?f={post["file"]["name"].ToString()}");
            }

            entry.StatusMsg = "Downloading Imgs";
            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry, headers: IMG_HEADERS, showProgress: false);

            var urls = new Regex("href=\\\\\"(.*?)\\\\\"")
                .Matches(post["content"].ToString())
                .Select(match => match.Groups[1].ToString());

            entry.StatusMsg = "Downloading video urls";
            int ind = 0;
            foreach (string url in urls)
            {
                if (url.Contains("vimeo"))
                {
                    var getResp = await Requests.Get(url);

                    string redirectUrl = getResp.RequestMessage.RequestUri.ToString();
                    await VideoConverter.DownloadYoutubeVideo(redirectUrl, newPath, entry, args: Args, showProgress: false);
                }

               
                ind++;

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    entry.Bar.Value = ind / urls.Count() * 100.0;
                }), DispatcherPriority.ContextIdle);
            }
        }



        //For some reason, the kemono + coomer party api appends a newline character
        //at the end of the string. The trimEnd removes it in order for it to be 
        //properly parsed by my json parser
        public async Task<IEnumerable<string>> GetMediaUrls(UrlEntry entry)
        {
            string creatorsText = (await Requests.GetStr(string.Format(CREATOR_API, domain)))
                .TrimEnd('\n');          
            var creatorData = JsonParser.Parse(creatorsText);

            userName = UnicodeUtil.DecodeEncodedNonAsciiCharacters(creatorData
                .Where(creator => creator["id"].ToString() == userId)
                .Select(creator => creator["name"].ToString()).FirstOrDefault(""));

            entry.Name = $"[{domain}] " + userName;

            var mediaUrls = new List<string>();
            int currPg = 0;
            while (true)
            {
                string getUrl = string.Format(POSTS_API, domain, service, userId, currPg);
                var resp = await Requests.Get(getUrl, headers: HEADERS);

                if (!resp.IsSuccessStatusCode) break;

                //Strip newline at end of string
                string jsonStr = (await resp.Content.ReadAsStringAsync()).TrimEnd('\n');

                var data = JsonParser.Parse(jsonStr);

                if (!data.Any())
                    break;

                foreach (var post in data)
                {
                    if (!post["attachments"].IsEmpty())
                    {
                        mediaUrls.AddRange(post["attachments"].Select(attach => 
                            $"https://{domain}.su/data{attach["path"].ToString()}?f={attach["name"].ToString()}"
                        ));
                    }
                    if (!post["file"].IsEmpty())
                    {
                        var file = post["file"];
                        mediaUrls.Add($"https://{domain}.su/data{post["file"]["path"].ToString()}?f={post["file"]["name"].ToString()}");
                    }
                }

                currPg += DEFAULT_POST_LIMIT;
            }

            return mediaUrls;
        }
    }
}
