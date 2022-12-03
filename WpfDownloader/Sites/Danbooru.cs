using ChromeCookie;
using Downloader.Data;
using Downloader.Util;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WpfDownloader.Data.Captions;
using WpfDownloader.Util;
using WpfDownloader.Util.Database;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Danbooru : Site
    {
        private static CookieContainer _cookieContainer = null;

        private static readonly List<Tuple<string, string>> HEADERS =
           new List<Tuple<string, string>>()
           {
                new Tuple<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36 Edg/99.0.1150.55"),
                new Tuple<string, string>("Referer", "https://danbooru.donmai.us/")
           };
        
        private static readonly string NOTES_API = "https://danbooru.donmai.us/notes.json?group_by=note&search[post_id]={0}";


        private string _escTags;
        private List<DanbooruCaptionContainer> _captionsContainer;

        private string _newPath;

        public Danbooru(string url, string args) : base(url, args)
        {
            _captionsContainer = new List<DanbooruCaptionContainer>();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            Uri myUri = new Uri(Url);
            _escTags = HttpUtility.ParseQueryString(myUri.Query).Get("tags");
            _escTags = Uri.UnescapeDataString(_escTags).Trim();

            entry.StatusMsg = "Retrieving";
            entry.Name = "[Danbooru] " + _escTags;

            var entries = await GetMediaUrls(entry);

            _escTags = RemoveIllegalChars(_escTags);
            _newPath = $"{DEFAULT_PATH}/danbooru/{_escTags}";

            await DownloadUtil.DownloadAllUrls(entries, _newPath, entry, headers: HEADERS);

            //if (_captionsContainer.Any())
            //{
            //    entry.StatusMsg = "Write -> Captions";
            //    await CaptionUtil.WriteCaptionsDanbooru(_captionsContainer);

            //    entry.StatusMsg = "Finished";
            //}
        }

        public async Task<IEnumerable<string>> GetMediaUrls(UrlEntry entry)
        {
            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://danbooru.donmai.us/");
                _cookieContainer = new CookieContainer();

                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".donmai.us"));
                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("danbooru.donmai.us"));
                Requests.AddCookies(_cookieContainer, baseAddress);
            }

            var urls = new List<string>();
            int currPg = 1;
            while (true)
            {
                if (entry.CancelToken.IsCancellationRequested)
                {
                    entry.CancelToken.ThrowIfCancellationRequested();
                    break;
                }

                var jsonUrl = $"https://danbooru.donmai.us/posts.json?tags={_escTags}&page={currPg}";
                var req = await Requests.GetReq(jsonUrl, cancelToken: entry.CancelToken);

                var data = JsonParser.Parse(await req.Content.ReadAsStringAsync());
                if (!data.Any()) break;


                var posts = data.Where(post => post["file_url"] != null && post["tag_string"] != null);

                foreach (var post in posts)
                {
                    //var info = new DanbooruInfo()
                    //{
                    //    Tags = post["tag_string"].Value,
                    //    Artist = post["tag_string_artist"].Value,
                    //    PostId = post["id"].Value,
                    //    FileName = post["md5"].Value,
                    //    FileExt = post["file_ext"].Value
                    //};
                    //await DanbooruDbLoader.LoadDbInfo(info);

                    if (post["last_noted_at"].Value != "null")
                    {
                        string notesJsonStr = await Requests.GetStr(string.Format(NOTES_API, 
                            post["id"].Value), cancelToken: entry.CancelToken);
                        var notesData = JsonParser.Parse(notesJsonStr);

                        string fileUrl = post["file_url"].Value;
                        string fileName = fileUrl.Split('/').Last();

                        var captions = notesData.Select(note => new DanbooruCaptionData()
                        {
                            Width = int.Parse(note["width"].Value),
                            Height = int.Parse(note["height"].Value),
                            PosX = int.Parse(note["x"].Value),
                            PosY = int.Parse(note["y"].Value),
                            CaptionText = note["body"].Value,
                            BgColor = MagickColors.Beige
                        });

                        _captionsContainer.Add(new DanbooruCaptionContainer()
                        {
                            OrigImagePath = $"{_newPath}/{fileName}",
                            OutputImagePath = $"{_newPath}/{fileName}",
                            Captions = captions
                        });
                    }

                    if (post["file_url"].Value.EndsWith(".zip") && post["large_file_url"] != null)
                    {
                        urls.Add(post["large_file_url"].Value);
                    }
                    else
                    {
                        urls.Add(post["file_url"].Value);
                    }
                }

                currPg++;
                await Task.Delay(500);
            }

            return urls;
        }
    }
}
