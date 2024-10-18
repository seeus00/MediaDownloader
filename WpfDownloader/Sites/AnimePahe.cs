using Downloader.Util;
using Microsoft.ClearScript.V8;
using Microsoft.ClearScript;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WpfDownloader.WpfData;
using System.Diagnostics;
using System.Threading;
using ControlzEx.Standard;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Navigation;
using WpfDownloader.Util.UserAgent;
using System.Net;
using ChromeCookie;

namespace WpfDownloader.Sites
{
    public class AnimePaheInfo
    {
        public string Slug { get; set; }
        public string Title { get; set; }
    }


    public class AnimePaheEp
    {
        public int EpisodeNum { get; set; }
        public string EpLink { get; set; }
    }

    public class AnimePaheVidSource
    {
        public string DataSrc { get; set; }
        public string Resolution { get; set; }
        public string Language { get; set; }
    }


    internal class AnimePahe : Site
    {
        private const int DOWNLOAD_VIDEO_MAX_THREADS = 2;

        private const string API_URL = "https://animepahe.ru/api?m=release&id={0}&sort=episode_asc&page={1}";
        private const string ANIME_URL = "https://animepahe.ru/anime";

        //private const string KWIK_REFERER = "https://kwik.cx";

        private CookieContainer cookieContainer;

        public AnimePahe(string url, string args) : base(url, args)
        {
        }


        private async Task<string> GetTitleFromSlug(string animeSlug)
        {
            var resp = await Requests.Get(ANIME_URL);
            resp.EnsureSuccessStatusCode();

            string html = await resp.Content.ReadAsStringAsync();
            return new Regex("href=\"\\/anime\\/(.*?)\" title=\"(.*?)\"", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => new AnimePaheInfo()
                {
                    Slug = match.Groups[1].Value,
                    Title = match.Groups[2].Value,
                })
                .Where(info => info.Slug == animeSlug)
                .First().Title;
        }

        private async Task<string> GetVideoUrl(string epUrl)
        {
            var resp = await Requests.Get(epUrl);
            resp.EnsureSuccessStatusCode();

            string html = await resp.Content.ReadAsStringAsync();

            var highestQualVid = new Regex("data-src=\"(.*?)\".*?data-resolution=\"(.*?)\" data-audio=\"(.*?)\"",
                RegexOptions.Singleline)
                    .Matches(html)
                    .Skip(1)
                    .Select(match => new AnimePaheVidSource()
                        { 
                            DataSrc = match.Groups[1].Value,
                            Resolution = match.Groups[2].Value,
                            Language = match.Groups[3].Value,
                        })
                    .Where(vid => vid.Language == "jpn") //Get only japanese audio (eng dub sucks ass)
                    .Last(); //Last element is 1080p

            string kwikUrl = highestQualVid.DataSrc;
            string topLevelDomain = kwikUrl.Split(".").Last().Split("/").First();


            var headers = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Referer", $"https://kwik.{topLevelDomain}")
            };

            await Task.Delay(1000);

            resp = await Requests.Get(kwikUrl, headers: headers);
            resp.EnsureSuccessStatusCode();

            html = await resp.Content.ReadAsStringAsync();
            var match = new Regex("<script>eval\\((.*?)\\);eval\\((.*?)\\)\\n", RegexOptions.Singleline)
                .Match(html);

            ScriptEngine engine = new V8ScriptEngine();
            string first = engine.Evaluate($"let result = {match.Groups[1].Value}; result") as string;
            string sec = engine.Evaluate($"let result2 = {match.Groups[2].Value}; result2") as string;

            string m3u8Url = new Regex("const source='(.*?)'", RegexOptions.Singleline).Match(first + sec).Groups[1].Value.Trim();
            return m3u8Url;
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Getting urls";

            if (cookieContainer == null)
            {
                var baseAddress = new Uri("https://animepahe.ru");
                cookieContainer = new CookieContainer();

                cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("animepahe.ru"));
                cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".animepahe.ru"));

                Requests.AddCookies(cookieContainer, baseAddress);
            }

            string animeSlug = Url.Split('/').Last();
            string host = new Uri(Url).Host;

            string title = await GetTitleFromSlug(animeSlug);
            string newPath = $"{DEFAULT_PATH}/animePahe/{RemoveIllegalChars(title)}";

            entry.Name = $"[AnimePahe] {title}";

            var resp = await Requests.Get(string.Format(API_URL, animeSlug, 1));
            resp.EnsureSuccessStatusCode();

            var eps = new List<AnimePaheEp>();
            var data = JsonParser.Parse(await resp.Content.ReadAsStringAsync());

            while (true)
            {
                var ep = data["data"].First();
                eps.AddRange(data["data"].Select(ep => new AnimePaheEp()
                {
                    EpisodeNum = ep["episode"].ToInt,
                    EpLink = $"https://{host}/play/{animeSlug}/{ep["session"]}",
                }));


                if (data["current_page"].ToInt == data["last_page"].ToInt) break;

                resp = await Requests.Get(string.Format(API_URL, animeSlug, data["current_page"].ToInt + 1));
                resp.EnsureSuccessStatusCode();
                data = JsonParser.Parse(await resp.Content.ReadAsStringAsync());
            }

            //Only want specific episode
            if (!string.IsNullOrEmpty(Args) && int.TryParse(Args, out int epNum))
            {
                eps = eps.Where(ep => ep.EpisodeNum == epNum).ToList();
            }

            entry.FilesMsg = $"0/{eps.Count}";
            entry.StatusMsg = "Downloading";

            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

            entry.DownloadPath = newPath;

            foreach (var ep in eps)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    entry.SubItems.Add(new UrlEntry()
                    {
                        Name = $"Episode: {ep.EpisodeNum}",
                        StatusMsg = UrlEntry.DOWNLOADING
                    });
                });
            }

            var videoHeaders = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("User-Agent", UserAgentUtil.CURR_USER_AGENT),
                new Tuple<string, string>("Referer", "https://kwik.cx/"),
                new Tuple<string, string>("Origin", "https://kwik.cx/"),
            };

            var ss = new SemaphoreSlim(DOWNLOAD_VIDEO_MAX_THREADS);
            int ind = 1;
            var tasks = eps.Select(async (ep, i) => 
            {
                if (entry.CancelToken.IsCancellationRequested) return;
                try
                {
                    await ss.WaitAsync();


                    string m3u8Url = await GetVideoUrl(ep.EpLink);
                    string epTitle = $"{RemoveIllegalChars(title)} - {ep.EpisodeNum.ToString("D2")}";

                    entry.SubItems[i].Name = epTitle;

                    await VideoConverter.DownloadYoutubeVideo(m3u8Url, newPath, entry: entry.SubItems[i], 
                        showProgress: true, fileName: epTitle, headers: videoHeaders);

                    entry.SubItems[i].StatusMsg = UrlEntry.FINISHED;

                    int percent = (ind * 100) / eps.Count;
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        entry.Bar.Value = percent;

                        entry.FilesMsg = $"{ind}/{eps.Count}";
                    }));
                    ind++;
                }
                finally
                {
                    ss.Release();
                }
            });

            await Task.WhenAll(tasks);
            entry.StatusMsg = "Finished";
        }
    }
}
