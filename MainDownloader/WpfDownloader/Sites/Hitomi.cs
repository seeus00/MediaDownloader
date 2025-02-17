using ChromeCookie;
using Downloader.Util;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.Util;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Hitomi : Site
    {
        private static string GG_JS_URL = "https://ltn.hitomi.la/gg.js";
        private static string GG_SCRIPT_FUNC = "";

        private static ScriptEngine _engine = new V8ScriptEngine();

        private static CookieContainer _cookieContainer = null;

        private string _title;
        private string _galleryId;

        private JToken _tags;

        private UrlEntry _entry;

        private List<Tuple<string, string>> _headers =
            new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Accept", "image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8"),
                new Tuple<string, string>("Accept-Encoding", "gzip, deflate, br"),
                new Tuple<string, string>("Accept-Language", "en-US,en;q=0.9"),
                new Tuple<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36 Edg/99.0.1150.55"),
                new Tuple<string, string>("Sec-Fetch-Site", "same-site"),
                new Tuple<string, string>("Sec-Fetch-Mode", "no-cors"),
                new Tuple<string, string>("Sec-Fetch-Dest", "image"),
                new Tuple<string, string>("sec-ch-ua-platform", "\"Windows\""),
                new Tuple<string, string>("sec-ch-ua", "\"Not A; Brand\";v=\"99\", \"Chromium\";v=\"99\", \"Microsoft Edge\";v=\"99\""),
                new Tuple<string, string>("sec-ch-ua-mobile", "?0"),
                new Tuple<string, string>("Connection", "keep-alive"),
                new Tuple<string, string>("Upgrade-Insecure-Requests", "1")
            };

        public Hitomi(string url, string args) : base(url, args)
        {

        }

        private static readonly string API_URL = "https://ltn.hitomi.la/galleries/{0}.js";

        private static string SubdomainFromUrl(string url, string baseStr)
        {
            string retVal = "b";
            if (!string.IsNullOrEmpty(baseStr))
            {
                retVal = baseStr;
            }

            var matches = Regex.Match(url, "/[0-9a-f]{61}([0-9a-f]{2})([0-9a-f])");
            if (matches == null) return "a";

            
            var g = Convert.ToUInt16(matches.Groups[2].ToString() + matches.Groups[1].ToString(), 16);
            int ggFuncResult = (int)_engine.Evaluate($"gg.m({g})");

            retVal = Convert.ToChar(97 + ggFuncResult) + retVal;

            return retVal;
        }

        private static string UrlFromUrl(string url, string baseStr) =>
            Regex.Replace(url, @"\/\/..?\.hitomi\.la\/", "//" + SubdomainFromUrl(url, baseStr) + ".hitomi.la/");

        private static string FullPathFromHash(string hash)
        {
            //if (hash.Length < 3) return hash;

            //var groups = Regex.Match(hash, @"^.*(..)(.)$").Groups;
            //return $"{groups[2]}/{groups[1]}/{hash}";
            string b = (string)_engine.Evaluate("gg.b");
            string s = (string)_engine.Evaluate($"gg.s('{hash}')");

            return b + s + '/' + hash;
        }

        private static string UrlFromHash(string hash, string dir, string ext)
        {
            return $"https://a.hitomi.la/{dir}/{FullPathFromHash(hash)}.{ext}";
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            _entry = entry;

            entry.StatusMsg = "Retrieving";
            entry.Name = "[Hitomi] " + Url;

            var mediaUrls = await GetMediaUrls();
            var newPath = $"{DEFAULT_PATH}/hitomi/{_title}";

            await TagWriter.WriteTags(_tags, newPath);
            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry,
                _headers, fileNameNumber: true, delayInBetween: 2000);
        }

        public async Task<IEnumerable<string>> GetMediaUrls()
        {
            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://hitomi.la/");
                _cookieContainer = new CookieContainer();

                var cookies = ChromeCookies.GetCookies("hitomi.la");
                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("hitomi.la"));
                Requests.AddCookies(_cookieContainer, baseAddress);
            }

            var id = Url.Split("-").Last().Split(".")[0];
            _galleryId = id;

            //_headers.Add(new Tuple<string, string>("Referer", $"https://hitomi.la/reader/{_galleryId}.html"));
            _headers.Add(new Tuple<string, string>("Referer", "https://hitomi.la/"));

            var jsData = await Requests.GetStr(string.Format(API_URL, id));
            var jsonStr = Regex.Match(jsData, @"var galleryinfo = (.*?)$").Groups[1].ToString();

            var data = JsonParser.Parse(jsonStr);
            _title = RemoveIllegalChars(data["title"].ToString());

            _tags = new JDict();
            _tags["tags"] = new JArray(data["tags"].Select(tag => tag["tag"].ToString()));

            _entry.Name = $"[Hitomi] {_title}";

            var hashes = data["files"].Select(i => i["hash"].ToString());

            if (string.IsNullOrEmpty(GG_SCRIPT_FUNC))
            {
                GG_SCRIPT_FUNC = await Requests.GetStr(GG_JS_URL);
                _engine.Execute("var gg = {};");
                _engine.Execute(GG_SCRIPT_FUNC);
            }

            var imgUrls = new List<string>();
            foreach (var file in data["files"])
            {
                if (file["haswebp"].ToString() == "1")
                {
                    string imgUrl = UrlFromUrl(UrlFromHash(file["hash"].ToString(), "webp", "webp"), "a");
                    imgUrls.Add(imgUrl);
                }
                else if (file["hasavif"].ToString() == "1")
                {
                    imgUrls.Add(UrlFromUrl(UrlFromHash(file["hash"].ToString(), "avif", "avif"), "a"));
                }
                else
                {
                    string ext = file["name"].ToString().Split('.').Last();
                    imgUrls.Add(UrlFromUrl(UrlFromHash(file["hash"].ToString(), "images", ext), null));
                }
            }

            return imgUrls;
        }
    }
}
