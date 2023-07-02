using ChromeCookie;
using Downloader.Util;
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
    public class Test : Site
    {
        private static readonly List<Tuple<string, string>> HEADERS =
            new List<Tuple<string, string>>()
            {

                new Tuple<string, string>("User-Agent", MainWindow.PERSONAL_CONFIG["user_agent"]),
                new Tuple<string, string>("Referer", "https://avjoa47.com/"),
                new Tuple<string, string>("Accept-Encoding", "identity"),
                new Tuple<string, string>("Host", "cdn.sdh239sd356sdg.com"),
            };

        public static readonly Random RAND = new Random();

        public Test(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            string url = "https://cdn.sdh239sd356sdg.com/2305/16/MIDV-186.mp4";
            await Requests.DownloadFileFromUrl(url, "C:/Users/casey/Desktop", entry: entry, headers: HEADERS);


            //var baseAddress = new Uri("https://dood.to/");
            //var _cookieContainer = new CookieContainer();

            ////_cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("dood.to"));
            //_cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".dood.to"));
            //Requests.AddCookies(_cookieContainer, baseAddress);

            //string videoId = "yo5k8np882do";
            //string testUrl = $"https://dood.to/e/{videoId}";

            //string html = await Requests.GetStr(testUrl);

            //string passMD5 = new Regex("(//pass_md5.*?)\'").Match(html).Groups[1].Value;
            //string token = new Regex("token=(.*?)&").Matches(html).First().Groups[1].Value;

            //string md5url = $"https://dood.to{passMD5}";
            //string result = await Requests.GetStr(md5url, HEADERS);

            //string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            //string digits = "0123456789";

            //string combinedNumChars = chars + digits;
            //string a = string.Empty;
            //for (int i = 0; i < 10; i++)
            //{
            //    int ind = RAND.Next(combinedNumChars.Length);
            //    a += combinedNumChars[ind];
            //}

            //TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            //int timestamp = (int)t.TotalSeconds * 1000;

            //string finalUrl = $"{result}{a}?token={token}&expiry={timestamp}";
            //Debug.WriteLine(finalUrl);

            //var newHeaders = new List<Tuple<string, string>>()
            //{

            //    new Tuple<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36 Edg/100.0.1185.50"),
            //    new Tuple<string, string>("Referer", "https://dood.to"),
            //};
            //await Requests.DownloadParticalContent(finalUrl, "C:/Users/casey/Desktop", newHeaders, fileName: "negro.mp4");

            //string pngPath = "G:/Users/casey/Pictures/danbooru/pomp_(qhtjd0120)/0f61339da882d5ba9e30a2da1ff7ef33.png";
            //await ImageUtil.PngToJpg(pngPath);


        }
    }
}
