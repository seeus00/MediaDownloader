using ChromeCookie;
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
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class HiperdexChapter
    {
        public string Url { get; set; }
        public string Title { get; set; }
    }

    public class Hiperdex : Site
    {
        private static readonly string CHAPTER_API = "https://hiperdex.com/manga/{0}/ajax/chapters/";
        private static readonly string MANGA_API = "https://hiperdex.com/wp-admin/admin-ajax.php";

        private static CookieContainer _cookieContainer = null;

        private static readonly List<Tuple<string, string>> HEADERS =
            new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.64 Safari/537.36 Edg/101.0.1210.47"),
                new Tuple<string, string>("Origin", "https://hiperdex.com"),
            };

        private string _title;

        public Hiperdex(string url, string args) : base(url, args)
        {
            _title = Url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = UrlEntry.RETRIEIVING;
            entry.Name = $"[Hiperdex] {_title}";

            string newPath = $"{DEFAULT_PATH}/hiperdex/{_title}";
            var chapters = await GetAllChapterUrls();

            entry.DownloadPath = newPath;

            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
            entry.StatusMsg = UrlEntry.DOWNLOADING;

            int ind = 1;
            foreach (var chapter in chapters)
            {
                string chapPath = $"{newPath}/{chapter.Title}";

                var chapUrls = await GetChapterUrls(chapter.Url);
                await DownloadUtil.DownloadAllUrls(chapUrls, chapPath, entry, fileNameNumber: true,
                    showProgress: false, setDownloadPath: false);

                int percent = (ind * 100) / chapters.Count();
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    entry.Bar.Value = percent;

                    entry.FilesMsg = $"{ind}/{chapters.Count()}";
                }), DispatcherPriority.Background);

                ind++;
            }

            entry.StatusMsg = UrlEntry.FINISHED;
        }

        public async Task<IEnumerable<string>> GetChapterUrls(string chapterUrl)
        {
            string html = await Requests.GetStr(chapterUrl, HEADERS);
            return new Regex("page-break no-gaps.*?img.*?src=\"(.*?)\"", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => match.Groups[1].Value.Trim());
        }


        public async Task<IEnumerable<HiperdexChapter>> GetAllChapterUrls()
        {
            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://hiperdex.com/");
                _cookieContainer = new CookieContainer();

                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".hiperdex.com"));
                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("hiperdex.com"));
                Requests.AddCookies(_cookieContainer, baseAddress);
            }


            var data = new List<KeyValuePair<string, string>>()
            {
            };

            string chaptersResult = await Requests.GetStrPost(string.Format(CHAPTER_API, _title), data, HEADERS);
            return new Regex("wp-manga-chapter.*?href=\"(.*?)\">(.*?)<", RegexOptions.Singleline)
                .Matches(chaptersResult)
                .Select(match => new HiperdexChapter()
                {
                    Url = match.Groups[1].Value,
                    Title = RemoveIllegalChars(match.Groups[2].Value.Trim())
                });
        }
    }
}
