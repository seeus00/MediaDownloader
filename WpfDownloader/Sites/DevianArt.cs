using ChromeCookie;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class DevianArt : Site
    {
        private string _userId;
        private static readonly string GET_GALLERY =
            "https://www.deviantart.com/_napi/da-user-profile/api/gallery/contents?username={0}&offset={1}&limit={2}&all_folder=true";
        private static CookieContainer _cookieContainer = null;

        private static readonly List<Tuple<string, string>> HEADERS = 
            new List<Tuple<string, string>>
        {
            new Tuple<string, string>("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36"),
            new Tuple<string, string>("accept", "application/json"),
            new Tuple<string, string>("accept-encoding", "br"),
            //new Tuple<string, string>("accept-language", "en-US,en;q=0.9"),
            new Tuple<string, string>("cache-control", "max-age=0"),
            new Tuple<string, string>("upgrade-insecure-requests", "1"),

            //new Tuple<string, string>("Referer", "https://www.deviantart.com/")
        };
            
        public DevianArt(string url, string args) : base(url, args)
        {
            HEADERS.Add(new Tuple<string, string>("referer", Url + "/gallery"));
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = "[DevianArt] " + Url;

            var mediaUrls = await GetMediaUrls();
            var newPath = $"{DEFAULT_PATH}/devianArt/{_userId}";

            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry, headers: HEADERS);
        }
        //public static void CopyTo(Stream src, Stream dest)
        //{
        //    byte[] bytes = new byte[4096];

        //    int cnt;

        //    while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
        //    {
        //        dest.Write(bytes, 0, cnt);
        //    }
        //}

        //public static string Unzip(byte[] bytes)
        //{
        //    using (var msi = new MemoryStream(bytes))
        //    using (var mso = new MemoryStream())
        //    {
        //        using (var gs = new GZipStream(msi, CompressionMode.Decompress))
        //        {
        //            //gs.CopyTo(mso);
        //            CopyTo(gs, mso);
        //        }

        //        return Encoding.UTF8.GetString(mso.ToArray());
        //    }
        //}

        public async Task<IEnumerable<string>> GetMediaUrls()
        {
            if (_cookieContainer == null)
            {
                _cookieContainer = new CookieContainer();

                var baseAddress = new Uri("https://www.deviantart.com/");
                var cookies = ChromeCookies.GetCookies(".deviantart.com");
                _cookieContainer.Add(baseAddress, cookies);
                Requests.AddCookies(_cookieContainer, baseAddress);
            }

            _userId = Url.Split('/').Last();

            var getUrl = string.Format(GET_GALLERY, _userId, 0, 24);
            //var bytes = await Requests.GetBytes(getUrl, HEADERS);
            //string jsonStr = Unzip(bytes);
            string jsonStr = await Requests.GetStr(getUrl, HEADERS);
            var data = JsonParser.Parse(jsonStr);
            
            var urls = new List<string>();

            while (true)
            {

                if (!data["results"].Any()) break;

                foreach (var result in data["results"])
                { 
                    string finalUrl = string.Empty;

                    var media = result["deviation"]["media"];
                    string baseUri = media["baseUri"].ToString();

                    if (media["types"].Last()["t"].ToString() == "gif")
                    {
                        string type = media["types"].Last()["b"].ToString();

                        finalUrl = !string.IsNullOrEmpty(media["token"].First().ToString()) ?
                            $"{type}?token={media["token"].First().ToString()}" : type;
                    }
                    else 
                    {
                        string type = media["types"]
                            .Where(type => type["t"].ToString() == "preview")
                            .First()["c"].ToString().Replace("<prettyName>", media["prettyName"].ToString());
                        if (media["token"] != null)
                        {
                            finalUrl = $"{baseUri}{type}?token={media["token"].First().ToString()}";
                        }
                        else
                        {
                            finalUrl = $"{baseUri}{type}";
                        }
                    }
                    if (!string.IsNullOrEmpty(finalUrl))
                    {
                        urls.Add(finalUrl);
                    }
                }

                if (!bool.Parse(data["hasMore"].ToString()))
                {
                    break;
                }

                getUrl = string.Format(GET_GALLERY, _userId, data["nextOffset"].ToString(), 24);
                jsonStr = await Requests.GetStr(getUrl, HEADERS);

                Debug.WriteLine(jsonStr);

                if (string.IsNullOrEmpty(jsonStr)) break;
                data = JsonParser.Parse(jsonStr);
            }
            
            return urls;
        }
    }
}
