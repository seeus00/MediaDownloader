using ChromeCookie;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WpfDownloader.Util.UserAgent;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Anchira : Site
    {
        // {id}/{key}
        public static string API_DATA_URL = "https://api.anchira.to/library/{0}/{1}/data";
        public static string API_INFO_URL = "https://api.anchira.to/library/{0}/{1}";

        public static string FILE_HOST_DOMAIN_EVEN = "https://kisakisexo.xyz";
        public static string FILE_HOST_DOMAIN_ODD = "https://aronasexo.xyz";

        private static CookieContainer _cookieContainer = null;

        private static List<Tuple<string, string>> HEADERS = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("Referer", "https://anchira.to/"),
            new Tuple<string, string>("Origin", "https://anchira.to"),
            new Tuple<string, string>("User-Agent", UserAgentUtil.CURR_USER_AGENT),
            new Tuple<string, string>("Host", "api.anchira.to"),
            new Tuple<string, string>("Connection", "keep-alive"),
            new Tuple<string, string>("Sec-Fetch-Dest", "empty"),
            new Tuple<string, string>("Sec-Fetch-Mode", "cors"),
            new Tuple<string, string>("Sec-Fetch-Site", "same-site"),
            new Tuple<string, string>("Accept-Language", "en-US,en;q=0.5"),
        };


        private string id;
        private string key;

        private string title;
        private List<string> tags;

        public Anchira(string url, string args) : base(url, args)
        {

        }

        private async Task<IEnumerable<string>> GetMediaUrls()
        {
            string infoUrl = string.Format(API_INFO_URL, id, key);
            string dataUrl = string.Format(API_DATA_URL, id, key);

            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://anchira.to");
                _cookieContainer = new CookieContainer();
                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".anchira.to"));


                Requests.AddCookies(_cookieContainer, baseAddress);
            }

           

            //var postResp = await Requests.PostAsync("https://anchira.to/api/v1/auth/refresh", headers);

            var resp = await Requests.Get(infoUrl, headers: HEADERS);
            resp.EnsureSuccessStatusCode();
            var data = JsonParser.Parse(await resp.Content.ReadAsStringAsync());
            title = data["title"].ToString();
            tags = data["tags"].Select(tag => tag["name"].ToString().ToLower()).ToList();

            var fileNames = data["data"].Select(file => file["n"].ToString());

            resp = await Requests.Get(dataUrl, headers: HEADERS);
            resp.EnsureSuccessStatusCode();
            data = JsonParser.Parse(await resp.Content.ReadAsStringAsync());

            string dataKey = data["key"].ToString();
            string hash = data["hash"].ToString();

            var finalUrls = new List<string>();
            var partialUrls = fileNames.Select(name => $"{id}/{dataKey}/{hash}/a/{name}").ToList();

            //For some reason, the site uses a different file hosting server for even and odd pages
            for (int i = 0; i < partialUrls.Count; i++)
            {
                if (i % 2 == 0) finalUrls.Add($"{FILE_HOST_DOMAIN_EVEN}/{partialUrls[i]}");
                else finalUrls.Add($"{FILE_HOST_DOMAIN_ODD}/{partialUrls[i]}");
            }

            return finalUrls;
        }


        public override async Task DownloadAll(UrlEntry entry)
        {
            var split = Url.Split("/");
            id = split[4];
            key = split[5];

            entry.StatusMsg = "Retrieving";
            entry.Name = $"[Anchira] {id} - {key}";

          
            var urls = await GetMediaUrls();

            string path = $"{DEFAULT_PATH}/anchira/{RemoveIllegalChars(title)}";
            entry.StatusMsg = "Downloading";
            entry.Name = $"[Anchira] {title}";

            //Remove original host
            HEADERS.RemoveAt(3);
            await DownloadUtil.DownloadAllUrls(urls, path, entry, headers: HEADERS, redirectUri: true);
        }
    }
}
