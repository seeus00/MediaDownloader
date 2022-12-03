using ChromeCookie;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.Util;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class WebToonXYZ : Site
    {
        private static CookieContainer _cookieContainer = null;
        private List<Tuple<string, string>> HEADERS = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.81 Safari/537.36"),
            new Tuple<string, string>("Referer", "https://www.webtoon.xyz/"),
            new Tuple<string, string>("sec-ch-ua-platform", "Windows"),
            new Tuple<string, string>("sec-fetch-mode", "no-cors"),
            new Tuple<string, string>("sec-fetch-dest", "image"),
            new Tuple<string, string>("accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8"),
        };


        private string _title;
        

        public WebToonXYZ(string url, string args) : base(url, args)
        {
            _title = Url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = "[WebtoonXYZ] " + _title;

            var chapters = await GetChapterUrls();
            string newPath = $"{DEFAULT_PATH}/webtoonxyz/{_title}";

            int ind = 0;
            foreach (string chapterUrl in chapters)
            {
                string chapterTitle = chapterUrl.TrimEnd('/').Split('/').Last();
                string chapPath = $"{newPath}/{chapterTitle}";

                var imgUrls = await GetImgsFromChapter(chapterUrl);
                await DownloadUtil.DownloadAllUrls(imgUrls, chapPath, entry, 
                    headers: HEADERS, showProgress: false);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ++ind;
                    entry.Bar.Value = (ind * 100.0) / chapters.Count();
                }), DispatcherPriority.ContextIdle);

                await Task.Delay(500);
            }

            entry.StatusMsg = "Finished";
        }

        private async Task<IEnumerable<string>> GetImgsFromChapter(string chapterUrl)
        {
            string html = await Requests.GetStr(chapterUrl, HEADERS);
            var result = new Regex("<img id=\"image-[0-9]*\" data-src=\"(.*?)\"", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => match.Groups[1].Value.Trim());

            //Remove last element (invalid image)
            result = result.Take(result.Count() - 1);
            return result;
        }


        public async Task<IEnumerable<string>> GetChapterUrls()
        {
            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://www.webtoon.xyz/");
                _cookieContainer = new CookieContainer();

                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".webtoon.xyz"));
                Requests.AddCookies(_cookieContainer, baseAddress);

                for (int i = 1; i <= 3; i++)
                {
                    //Add cookies for cloudfare img server
                    baseAddress = new Uri($"https://cdn{i}.webtoon.xyz");
                    _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".webtoon.xyz"));
                    Requests.AddCookies(_cookieContainer, baseAddress);
                }
            }

            string html = await Requests.GetStr(Url, HEADERS);
            var chapters = new Regex("class=\"wp-manga-chapter.*?href=\"(.*?)\"", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => match.Groups[1].Value);

            return chapters;
        }
    }
}
