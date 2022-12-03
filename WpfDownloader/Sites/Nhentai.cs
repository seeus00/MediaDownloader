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
using WpfDownloader.Util;
using WpfDownloader.Util.Database;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Nhentai : Site
    {
        private static CookieContainer _cookieContainer = null;

        private static readonly List<Tuple<string, string>> HEADERS =
            new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9"),
                new Tuple<string, string>("Accept-Language", "en-US,en;q=0.9"),
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


            entry.StatusMsg = "Retrieving";
            entry.Name = $"[Nhentai] " + _code;

            //string jsonPath = $"{DEFAULT_PATH}/nhentai/info.json";
            //if (!File.Exists(jsonPath))
            //{
            //    await File.WriteAllTextAsync(jsonPath, "[]");
            //}

            //_savedInfo = (JArray)JsonParser.Parse((await File.ReadAllTextAsync(jsonPath)));
            //if (_savedInfo.Where(entry => entry["code"].Value == _code).Any())
            //{
            //    entry.StatusMsg = "Exists";
            //    return;
            //}

            _newPath = $"{DEFAULT_PATH}/nhentai/{_code}";
            var mediaUrls = await GetMediaUrls(entry);
           

            //await TagWriter.WriteTags(_tags, newPath);
            await DownloadUtil.DownloadAllUrls(mediaUrls, _newPath, entry);
            
            
            //await File.WriteAllTextAsync(jsonPath, JsonParser.Serialize(_savedInfo).ToString());
        }

        public async Task<IEnumerable<string>> GetMediaUrls(UrlEntry entry)
        {
            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://nhentai.net");
                _cookieContainer = new CookieContainer();

                var cookies = ChromeCookies.GetCookies("nhentai.net");
                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("nhentai.net"));
                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".nhentai.net"));
                Requests.AddCookies(_cookieContainer, baseAddress);

                string userAgent = MainWindow.PERSONAL_CONFIG["user_agent"];

                HEADERS.Add(new Tuple<string, string>("User-Agent", string.IsNullOrEmpty(userAgent) ? "" : userAgent));
            }

            var jsonStr = await Requests.GetStr($"https://nhentai.net/api/gallery/{_code}", HEADERS);
            var data = JsonParser.Parse(jsonStr);

            _title = RemoveIllegalChars(data["title"]["pretty"].Value);
            entry.Name = $"[Nhentai] {_code} - {_title}";

            //var info = new JDict();
            //info["code"] = new JType(_code);
            //info["title"] = new JType(_title);
            //info["tags"] = new JArray(data["tags"].Select(tag => tag["name"].Value));

            //_savedInfo.Add(info);

            var tags = data["tags"]
                .Where(tag => tag["type"].Value == "tag")
                .Select(tag => tag["name"].Value);

            string artist = data["tags"].Where(tag => tag["type"].Value == "artist")
                .Select(tag => tag["name"].Value)
                .FirstOrDefault(string.Empty);

            //If the artist doesn't exist, use the group
            string group = data["tags"].Where(tag => tag["type"].Value == "group")
                .Select(tag => tag["name"].Value)
                .FirstOrDefault();

            artist = (string.IsNullOrEmpty(artist)) ? group : artist;

            var mediaId = data["media_id"].Value;
            int ind = 1;
            var urls = new List<string>();
            foreach (var pg in data["images"]["pages"])
            {
                string ext = string.Empty;
                switch (pg["t"].Value[0])
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
