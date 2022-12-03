using WpfDownloader.Sites;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDownloader.WpfData;
using System.Threading;
using System.Collections.Concurrent;
using Downloader.Data;
using System.Net;
using ChromeCookie;

namespace WpfDownloader.Sites
{
    public class ArtStation : Site
    {
        private const string API_URL = "" +
            "https://www.artstation.com/users/{0}/projects.json?page={1}";

        private static List<Tuple<string, string>> HEADERS =
            new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.124 Safari/537.36 Edg/102.0.1245.41")
            };

        private static CookieContainer _cookieContainer = null;

        private static readonly SemaphoreSlim _slim = new SemaphoreSlim(10);
        private string _user;

        public ArtStation(string url, string args) : base(url, args)
        {
            _user = url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = "[ArtStation] " + _user;
            var mediaUrls = await GetMediaUrls();

            var newPath = $"{DEFAULT_PATH}/artstation/{_user}";

            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry);
        }

        public async Task<IEnumerable<string>> GetImgsFromHash(string hashId)
        {
            string url = $"https://www.artstation.com/projects/{hashId}.json";
            string dataStr = await Requests.GetStr(url);
            var data = JsonParser.Parse(dataStr);

            return data["assets"]
                .Where(asset => asset["asset_type"].Value == "image")
                .Select(asset => asset["image_url"].Value);
        }

        public async Task<IEnumerable<ImgData>> GetMediaUrls()
        {
            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://www.artstation.com/");
                _cookieContainer = new CookieContainer();

                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("artstation.com"));
                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".artstation.com"));
                Requests.AddCookies(_cookieContainer, baseAddress);
            }

            HEADERS.Add(new Tuple<string, string>("Referer", Url));

            var hashes = new List<string>();
            int page = 1;
            while (true)
            {
                string getUrl = string.Format(API_URL, _user, page);
                string jsonStr = await Requests.GetStr(getUrl, HEADERS);
                var data = JsonParser.Parse(jsonStr);

                if (!data["data"].Any())
                    break;

                var pgHashes = data["data"].Select(img => img["hash_id"].Value);
                hashes.AddRange(pgHashes);

                page++;
            }


            var urls = new BlockingCollection<ImgData>();
            var tasks = new List<Task>();
            foreach (var hash in hashes)
            {
                await _slim.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    var imgs = await GetImgsFromHash(hash);
                    try
                    {
                        int ind = 0;
                        foreach (var img in imgs)
                        {
                            urls.Add(new ImgData()
                            {
                                Url = img,
                                Filename = $"{hash}_{ind}"
                            });
                            ind++;
                        }
                    }
                    finally
                    {
                        _slim.Release();
                    }
                }));
            }

            return urls;
        }
    }
}
