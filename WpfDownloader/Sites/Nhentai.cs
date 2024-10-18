using ChromeCookie;
using ControlzEx.Standard;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.Util.Database;
using WpfDownloader.Util.LinkSaver;
using WpfDownloader.Util.UserAgent;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Nhentai : Site
    {
        private static CookieContainer _cookieContainer = null;

        private static readonly List<Tuple<string, string>> HEADERS =
            new List<Tuple<string, string>>()
            {

            };

        private string _title;

        private string _code;
        private JArray _savedInfo;

        private string _newPath;

        enum ImageTypes
        {
            Png = 'p',
            Jpg = 'j',
            Gif = 'g'
        }

        public Nhentai(string url, string args) : base(url, args)
        {
            _code = Url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://nhentai.net");
                _cookieContainer = new CookieContainer();

                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("nhentai.net"));
                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".nhentai.net"));


                Requests.AddCookies(_cookieContainer, baseAddress);

                string userAgent = UserAgentUtil.CURR_USER_AGENT;

                HEADERS.Add(new Tuple<string, string>("User-Agent", userAgent));
                HEADERS.Add(new Tuple<string, string>("Upgrade-Insecure-Requests", "1"));
                HEADERS.Add(new Tuple<string, string>("TE", "trailers"));
                HEADERS.Add(new Tuple<string, string>("Sec-Fetch-Site", "cross-site"));
                HEADERS.Add(new Tuple<string, string>("Sec-Fetch-Mode", "navigate"));
                HEADERS.Add(new Tuple<string, string>("Sec-Fetch-Dest", "document"));
                HEADERS.Add(new Tuple<string, string>("Host", "nhentai.net"));
                HEADERS.Add(new Tuple<string, string>("Connection", "keep-alive"));


            }

            var path = Url.Split('/')[3];
            if (path != "artist" && path != "group")
            {
                entry.StatusMsg = "Retrieving";
                entry.Name = $"[Nhentai] {_code}";

                _newPath = $"{DEFAULT_PATH}/nhentai/{_code}";
                var mediaUrls = await GetMediaUrls(_code, entry);

                if (mediaUrls == null)
                {
                    //entry.StatusMsg = "Doujin doesn't exist";
                    return;
                }

                //await TagWriter.WriteTags(_tags, newPath);
                await DownloadUtil.DownloadAllUrls(mediaUrls, _newPath, entry);
            }else 
            {
                entry.StatusMsg = "Retrieving codes";

                string artist = Url.Split('/').Last();
                string query = $"https://nhentai.net/search/?q={artist}+language:english";

                var galleryCodes = new List<string>();
                int currPg = 1;
                while (true)
                {
                    await Task.Delay(1000);
                    if (entry.CancelToken.IsCancellationRequested) return;


                    var resp = await Requests.Get($"{query}&page={currPg}", headers: HEADERS);
                    resp.EnsureSuccessStatusCode();

                    string galleryHtml = await resp.Content.ReadAsStringAsync();
                    var matches = new Regex("href=\\\"\\/g\\/(.*?)\\/", RegexOptions.Singleline)
                        .Matches(galleryHtml);

                    if (!matches.Any()) break;
                    var galleries = matches.Select(match => match.Groups[1].ToString().Trim());


                    galleryCodes.AddRange(galleries);
                    currPg++;
                }

                entry.DownloadPath = $"{DEFAULT_PATH}/nhentai/";
                foreach (var galleryCode in galleryCodes)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        entry.SubItems.Add(new UrlEntry()
                        {
                            Name = galleryCode,
                            StatusMsg = UrlEntry.DOWNLOADING
                        });
                    });
                }

                entry.StatusMsg = UrlEntry.DOWNLOADING;

                //Ignore any galleries that have already been downloaded
                //galleryCodes = galleryCodes.Where(code => !LinkSaveManager.LoadData().Where(data => data.DirName == code).Any()).ToList();

                //Download all codes concurrently (2 at a time)
                int ind = 1;
                var semaphoreSlim = new SemaphoreSlim(2);
                var tasks = galleryCodes.Select(async (code, i) =>
                {
                    if (entry.CancelToken.IsCancellationRequested) return;

                    await semaphoreSlim.WaitAsync();
                    try
                    {
                        string newPath = $"{DEFAULT_PATH}/nhentai/{code}";
                        var currEntry = entry.SubItems[i];
                        if (LinkSaveManager.LoadData().Where(data => data.DirName == code).Any())
                        {
                            currEntry.FilesMsg = "Already downloaded";
                            currEntry.Bar.Value = 100;
                            currEntry.StatusMsg = UrlEntry.FINISHED;

                            return;
                        }

                        var mediaUrls = await GetMediaUrls(code);
                        if (mediaUrls == null) { ind++; return; }

                        
                        currEntry.Name = $"[Nhentai] {code}";
                        currEntry.ImgIconPath = entry.ImgIconPath;
                        
                        await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, currEntry, showProgress: true, 
                            overrideDownloadedFiles: false);

                        int percent = (ind * 100) / galleryCodes.Count();
                        await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            entry.Bar.Value = percent;

                            entry.FilesMsg = $"{ind}/{galleryCodes.Count()}";
                        }), DispatcherPriority.Background);
                        ind++;
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                });
                await Task.WhenAll(tasks);
            }

            entry.StatusMsg = (entry.CancelToken.IsCancellationRequested) ? "Cancelled" : "Finished";
            //await File.WriteAllTextAsync(jsonPath, JsonParser.Serialize(_savedInfo).ToString());
        }

        private async Task<IEnumerable<string>> GetMediaUrls(string code)
        {
            await Task.Delay(1000);

            var resp = await Requests.Get($"https://nhentai.net/api/gallery/{code}", headers: HEADERS);
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var data = JsonParser.Parse(await resp.Content.ReadAsStringAsync());

            _title = RemoveIllegalChars(data["title"]["pretty"].ToString());
            var tags = data["tags"]
                .Where(tag => tag["type"].ToString() == "tag")
                .Select(tag => tag["name"].ToString());

            string artist = data["tags"].Where(tag => tag["type"].ToString() == "artist")
                .Select(tag => tag["name"].ToString())
                .FirstOrDefault(string.Empty);

            //If the artist doesn't exist, use the group
            string group = data["tags"].Where(tag => tag["type"].ToString() == "group")
                .Select(tag => tag["name"].ToString())
                .FirstOrDefault();

            artist = (string.IsNullOrEmpty(artist)) ? group : artist;

            var mediaId = data["media_id"].ToString();
            int ind = 1;
            var urls = new List<string>();
            foreach (var pg in data["images"]["pages"])
            {
                string ext = string.Empty;
                switch (pg["t"].ToString()[0])
                {
                    case (char)ImageTypes.Png:
                        ext = "png";
                        break;
                    case (char)ImageTypes.Jpg:
                        ext = "jpg";
                        break;
                    case (char)ImageTypes.Gif:
                        ext = "gif";
                        break;
                }
                urls.Add($"https://i.nhentai.net/galleries/{mediaId}/{ind}.{ext}");
                ind++;
            }


            var info = new NhentaiInfo()
            {
                CodeId = int.Parse(code.Trim()),
                Title = _title,
                Pages = urls.Count,
                Name = _title,
                CoverName = urls.First().Split('/').Last(),
                Tags = string.Join(' ', tags),
                Artist = artist
            };

            await TagWriter.WriteNhentaiTags(info);
            return urls;
        }

        public async Task<IEnumerable<string>> GetMediaUrls(string code, UrlEntry entry)
        {
            await Task.Delay(1000);

            var resp = await Requests.Get($"https://nhentai.net/api/gallery/{code}", headers: HEADERS);
            string test = await resp.Content.ReadAsStringAsync();

            resp.EnsureSuccessStatusCode();

            //if (resp.StatusCode != HttpStatusCode.OK)
            //{
            //    entry.StatusMsg = $"Status Code Response: {resp.StatusCode}";
            //    return null;
            //}

            var data = JsonParser.Parse(await resp.Content.ReadAsStringAsync());

            _title = RemoveIllegalChars(data["title"]["pretty"].ToString());
            entry.Name = $"[Nhentai] {_code} - {_title}";

            //var info = new JDict();
            //info["code"] = new JType(_code);
            //info["title"] = new JType(_title);
            //info["tags"] = new JArray(data["tags"].Select(tag => tag["name"].ToString()));

            //_savedInfo.Add(info);

            var tags = data["tags"]
                .Where(tag => tag["type"].ToString() == "tag")
                .Select(tag => tag["name"].ToString());

            string artist = data["tags"].Where(tag => tag["type"].ToString() == "artist")
                .Select(tag => tag["name"].ToString())
                .FirstOrDefault(string.Empty);

            //If the artist doesn't exist, use the group
            string group = data["tags"].Where(tag => tag["type"].ToString() == "group")
                .Select(tag => tag["name"].ToString())
                .FirstOrDefault();

            artist = (string.IsNullOrEmpty(artist)) ? group : artist;

            var mediaId = data["media_id"].ToString();
            int ind = 1;
            var urls = new List<string>();
            foreach (var pg in data["images"]["pages"])
            {
                string ext = string.Empty;
                switch (pg["t"].ToString()[0])
                {
                    case (char) ImageTypes.Png:
                        ext = "png";
                        break;
                    case (char) ImageTypes.Jpg:
                        ext = "jpg";
                        break;
                    case (char) ImageTypes.Gif:
                        ext = "gif";
                        break;
                }
                urls.Add($"https://i.nhentai.net/galleries/{mediaId}/{ind}.{ext}");
                ind++;
            }


            var info = new NhentaiInfo()
            {
                CodeId = int.Parse(_code.Trim()),
                Title = _title,
                Pages = urls.Count,
                Name = _title,
                CoverName = urls.First().Split('/').Last(),
                Tags = string.Join(' ', tags),
                Artist = artist
            };

            //await NhentaiDbLoader.LoadDbInfo(new NhentaiInfo()
            //{
            //    CodeId = int.Parse(_code.Trim()),
            //    Title = _title,
            //    Pages = urls.Count,
            //    Name = _title,
            //    CoverName = urls.First().Split('/').Last(),
            //    Tags = string.Join(' ', tags),
            //    Artist = artist
            //});

            await TagWriter.WriteNhentaiTags(info);

            return urls;
        }
    }
}
