using ChromeCookie;
using Downloader.Data;
using Downloader.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.Data.Imaging;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Pixiv : Site
    {
        private static CookieContainer _cookieContainer = null;

        private static readonly List<Tuple<string, string>> _jsonHeaders = 
            new List<Tuple<string, string>>()
            {
                           
                new Tuple<string, string>("accept", "application/json"),
                new Tuple<string, string>("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/109.0"),

    };


        private static readonly string ILLUST_ID_API =
                "https://www.pixiv.net/ajax/user/{0}/profile/all?lang=en";

        private static readonly string PAGES_API =
                "https://www.pixiv.net/ajax/illust/{0}?lang=en";

        private static readonly string UGOIRA_API  =
                "https://www.pixiv.net/ajax/illust/{0}/ugoira_meta";

        private static readonly string VID_SIZE = "1920x1080";

        private static readonly SemaphoreSlim _slim = new SemaphoreSlim(10);

        private string _userId;
        private JDict _info;

        private string _newPath;

        private List<ZipToGifData> _gifEntries;

        //private UrlEntry _entry;

        private enum Types
        {
            IMAGE = 0,
            VIDEO = 2
        }

        public Pixiv(string url, string args) : base(url, args)
        {
            _userId = new Regex("([0-9]+)").Match(url).Groups[1].Value;
            _gifEntries = new List<ZipToGifData>();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            //_entry = entry;
            entry.StatusMsg = "Retrieving";
            entry.Name = "[Pixiv] " + _userId;

            // var jsonStr = await Requests.GetStr("https://www.pixiv.net/ajax/illust/87898719/pages?lang=en");
            var entries = await GetMediaUrls(entry);
            var headers = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Referer", "https://www.pixiv.net/")
            };


            await DownloadUtil.DownloadAllUrls(entries, _newPath, entry, headers, gifEntries: _gifEntries);
        }


        public async Task<IEnumerable<string>> GetEveryIllustIds(string profileId)
        {
            var jsonUrl = string.Format(ILLUST_ID_API, profileId);

            var jsonStr = await Requests.GetStr(jsonUrl, _jsonHeaders);
            var data = JsonParser.Parse(jsonStr);

            return data["body"]["illusts"].Select(illust => illust.Name);
        }

        public async Task<IEnumerable<string>> GetOrigImage(string illustId)
        {
            var jsonUrl = string.Format(PAGES_API, illustId);
            var jsonStr = await Requests.GetStr(jsonUrl, _jsonHeaders);

            var data = JsonParser.Parse(jsonStr);
            var entries = new List<string>();


            var tags = new List<string>();
            var allTags = data["body"]["tags"]["tags"];
            foreach (var tag in allTags)
            {
                if (tag["translation"] != null)
                {
                    tags.Add(tag["translation"]["en"].Value);
                }else if (tag["romaji"] != null)
                {
                    tags.Add(tag["romaji"].Value);
                }else
                {
                    tags.Add(tag["tag"].Value);
                }
            }


            string fileUrl = data["body"]["urls"]["original"].Value;
            switch (int.Parse(data["body"]["illustType"].Value))
            {
                case ((int)Types.IMAGE):
                    
                    int totalPgCount = int.Parse(data["body"]["pageCount"].Value);

                    for (int pg = 0; pg < totalPgCount; pg++)
                    {
                        entries.Add(Regex.Replace(fileUrl, "p[0-9]+", $"p{pg}"));
                    }
                    break;
                case ((int)Types.VIDEO):
                    string vidUrl = fileUrl.Replace("img-original", "img-zip-ugoira");
                    vidUrl = Regex.Replace(vidUrl, "0\\..*", $"{VID_SIZE}.zip");

                    string ugoriaJson = await Requests.GetStr(string.Format(UGOIRA_API, illustId), _jsonHeaders);
                    var ugoriaData = JsonParser.Parse(ugoriaJson);

                    var illustFrames = ugoriaData["body"]["frames"].Select(frame => new GifFrameData()
                    {
                        FrameName = frame["file"].Value,
                        BasePath = vidUrl.Split('/').Last().Split('.')[0],
                        FrameDelay = int.Parse(frame["delay"].Value)
                    });

                    _gifEntries.Add(new ZipToGifData()
                    {
                        TempPathName = $"{_newPath}/{vidUrl.Split('/').Last().Split('.')[0]}",
                        Frames = illustFrames
                    });

                    entries.Add(vidUrl);
                    break;
            }

            return entries;
        }

        public async Task<IEnumerable<string>> GetMediaUrls(UrlEntry entry)
        {
            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://www.pixiv.net/");
                _cookieContainer = new CookieContainer();

                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".pixiv.net"));
                Requests.AddCookies(_cookieContainer, baseAddress);
            }

            string html = await Requests.GetStr(Url, _jsonHeaders.Skip(1).ToList());
            html = html.Replace("\\", string.Empty).ToString();
            string name = new Regex("userId.*?name\":\"(.*?)\"")
                .Match(html).Groups[1].Value;

            _info = new JDict();
            _info["url"] = new JType(Url);
            _info["user_id"] = new JType(_userId);
            _info["name"] = new JType(name);

            _newPath = $"{DEFAULT_PATH}/pixiv/{_userId} - " + 
                $"{RemoveIllegalChars(_info["name"].Value)}";

            entry.Name = $"[Pixiv] {name} - {_userId}";

            var illustIds = await GetEveryIllustIds(_userId);
            if (illustIds == null) return null;

            
            var tasks = new List<Task>();
            var entries = new BlockingCollection<string>();

            foreach (var id in illustIds)
            {
                if (entry.CancelToken.IsCancellationRequested)
                {
                    entry.CancelToken.ThrowIfCancellationRequested();
                    break;
                }

                await _slim.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    var origImgs = await GetOrigImage(id);
                    if (origImgs == null || !origImgs.Any())
                        return;
                    try
                    {
                        foreach (var img in origImgs)
                            entries.Add(img);
                    }
                    finally
                    {
                        _slim.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return entries;
            
        }
    }
}
