using ChromeCookie;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using WpfDownloader.Util.UserAgent;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    internal class EightChanMoe : Site
    {
        private const string DOMAIN = "https://8chan.moe";

        private string title;
        private string bodyContent;

        private CookieContainer cookieContainer;

        public EightChanMoe(string url, string args) : base(url, args)
        {
        }

        private async Task<IEnumerable<string>> GetMediaUrls()
        {
            if (cookieContainer == null)
            {
                var baseAddress = new Uri(DOMAIN);
                cookieContainer = new CookieContainer();

                cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("8chan.moe"));


                Requests.AddCookies(cookieContainer, baseAddress);
            }

            string html = await Requests.GetStr(Url);
            var mediaUrls = new Regex("imgLink.*>?href=\"(.*?)\"").Matches(html).Select(match => $"{DOMAIN}{match.Groups[1].Value}");

            var match = new Regex("noEmailName\" >(.*?)<.*?divMessage\">(.*?)<", RegexOptions.Singleline).Matches(html).First();
            title = HttpUtility.HtmlDecode(match.Groups[1].Value).Trim();
            bodyContent = HttpUtility.HtmlDecode(match.Groups[2].Value).Trim();

            return mediaUrls;
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            string threadId = new Regex("res\\/([0-9]+)").Match(Url).Groups[1].Value;

            entry.StatusMsg = "Retrieving";
            entry.Name = "[8Chan] " + threadId;

            var mediaUrls = await GetMediaUrls();
            entry.Name = "[8Chan] " + title;
            string newPath = $"{DEFAULT_PATH}/8chan/{threadId} - {RemoveIllegalChars(title)}";

            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry);
        }
    }
}
