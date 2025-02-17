using Downloader.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Downloader.Sites
{
    public class ManaToki : Site
    {
        private static List<Tuple<string, string>> _downloadFileHeaders =
            new List<Tuple<string, string>>();


        private static readonly List<string> _userAgentList = new List<string>() 
        {
            "Mozilla/5.0 (Windows NT 6.1 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.87 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.94 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1, WOW64 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.87 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1, WOW64 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.94 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.3, WOW64 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.87 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0, Win64, x64 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.87 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0, Win64, x64 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.94 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0, WOW64 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.75 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0, WOW64 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.87 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0, WOW64 AppleWebKit/537.36 (KHTML, like Gecko Chrome/50.0.2661.94 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.0, rv:46.0 Gecko/20100101 Firefox/46.0",
            "Mozilla/5.0 (Windows NT 6.1, WOW64, rv:46.0 Gecko/20100101 Firefox/46.0",
            "Mozilla/5.0 (Windows NT 10.0, Win64, x64, rv:46.0 Gecko/20100101 Firefox/46.0",
            "Mozilla/5.0 (Windows NT 10.0, WOW64, rv:46.0 Gecko/20100101 Firefox/46.0",
            "Mozilla/5.0 (Macintosh, Intel Mac OS X 10_11_1 AppleWebKit/601.2.7 (KHTML, like Gecko Version/9.0.1 Safari/601.2.7",
            "Mozilla/5.0 (Macintosh, Intel Mac OS X 10_11_2 AppleWebKit/601.3.9 (KHTML, like Gecko Version/9.0.2 Safari/601.3.9",
            "Mozilla/5.0 (Macintosh, Intel Mac OS X 10_11_3 AppleWebKit/601.4.4 (KHTML, like Gecko Version/9.0.3 Safari/601.4.4",
            "Mozilla/5.0 (Android, Tablet, rv:30.0 Gecko/30.0 Firefox/30.0",
            "Mozilla/5.0 (Android, Tablet, rv:33.0 Gecko/33.0 Firefox/33.0",
            "Mozilla/5.0 (Android, Tablet, rv:34.0 Gecko/34.0 Firefox/34.0",
            "Mozilla/5.0 (Android, Tablet, rv:35.0 Gecko/35.0 Firefox/35.0",
            "Mozilla/5.0 (Linux, Android 4.4.4, D5503 Build/14.4.A.0.133 AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.93 Mobile Safari/537.36",
            "Mozilla/5.0 (Linux, Android 4.4.4, Nexus 10 Build/KTU84P AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.59 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 4.4.4, Nexus 7 Build/KTU84P AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.59 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 4.4.4, Nexus 7 Build/KTU84P AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.93 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 5.0.1, Nexus 10 Build/LRX22C AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.59 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 5.0.1, Nexus 10 Build/LRX22C AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.93 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 5.0.2, Nexus 10 Build/LRX22G AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.93 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 5.0, Nexus 10 Build/LRX21P AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.59 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 5.0, Nexus 10 Build/LRX21P AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.93 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 5.0, Nexus 7 Build/LRX21P AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.59 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 5.0, Nexus 7 Build/LRX21P AppleWebKit/537.36 (KHTML, like Gecko Chrome/39.0.2171.93 Safari/537.36",
            "Mozilla/5.0 (iPad, CPU OS 8_1_2 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko CriOS/39.0.2171.50 Mobile/12B440 Safari/600.1.4",
            "Mozilla/5.0 (iPhone, CPU iPhone OS 7_1_2 like Mac OS X AppleWebKit/537.51.2 (KHTML, like Gecko CriOS/39.0.2171.50 Mobile/11D257 Safari/9537.53",

            "Mozilla/5.0 (Linux, Android 5.0.2, Nexus 10 Build/LRX22G AppleWebKit/537.36 (KHTML, like Gecko Chrome/40.0.2214.109 Safari/537.36",
            "Mozilla/5.0 (Linux, Android 5.0.2, Nexus 10 Build/LRX22G AppleWebKit/537.36 (KHTML, like Gecko Chrome/40.0.2214.89 Safari/537.36",


            "Mozilla/5.0 (iPad, CPU OS 8_0_2 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12A405 Safari/600.1.4",
            "Mozilla/5.0 (iPad, CPU OS 8_0 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12A365 Safari/600.1.4",
            "Mozilla/5.0 (iPad, CPU OS 8_1_1 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12B435 Safari/600.1.4",
            "Mozilla/5.0 (iPad, CPU OS 8_1_2 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12B440 Safari/600.1.4",
            "Mozilla/5.0 (iPad, CPU OS 8_1_3 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12B466 Safari/600.1.4",
            "Mozilla/5.0 (iPad, CPU OS 8_1 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12B410 Safari/600.1.4",
            "Mozilla/5.0 (iPhone, CPU iPhone OS 8_1_1 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12B435 Safari/600.1.4",
            "Mozilla/5.0 (iPhone, CPU iPhone OS 8_1_1 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12B436 Safari/600.1.4",
            "Mozilla/5.0 (iPhone, CPU iPhone OS 8_1 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12B410 Safari/600.1.4",
            "Mozilla/5.0 (iPhone, CPU iPhone OS 8_1 like Mac OS X AppleWebKit/600.1.4 (KHTML, like Gecko Version/8.0 Mobile/12B411 Safari/600.1.4",


            "Opera/9.80 (Android 2.3.3, Linux, Opera Mobi/ADR-1212030829 Presto/2.11.355 Version/12.10",
            "Opera/9.80 (Android 2.3.3, Linux, Opera Mobi/ADR-1301080958 Presto/2.11.355 Version/12.10",
            "Mozilla/4.0 (compatible, MSIE 5.23, Macintosh, PPC Escape 5.1.8",
            "Mozilla/5.0 (SMART-TV, X11, Linux i686 AppleWebKit/534.7 (KHTML, like Gecko Version/5.0 Safari/534.7",
            "Mozilla/5.0 (X11, U, Linux i686, en-US AppleWebKit/533.4 (KHTML, like Gecko Chrome/5.0.375.127 Large Screen Safari/533.4 GoogleTV/162671",
            "Mozilla/5.0 (Windows, U, Windows NT 5.1, en-US, rv:1.9.2 Gecko/20100222 Firefox/3.6 Kylo/0.6.1.70394",
            "Opera/9.80 (Linux armv7l, Opera TV Store/5581 Presto/2.12.362 Version/12.11",
            "iTunes-AppleTV/4.1",
            "Mozilla/5.0 (CrKey armv7l 1.4.15250 AppleWebKit/537.36 (KHTML, like Gecko Chrome/31.0.1650.0 Safari/537.36",
            "Mozilla/5.0 (X11, U, Linux i686, en-US AppleWebKit/533.4 (KHTML, like Gecko Chrome/5.0.375.127 Large Screen Safari/533.4 GoogleTV/ 162671",
            "Mozilla/5.0 (X11, U: Linux i686, en-US AppleWebKit/533.4 (KHTML, like Gecko Chrome/5.0.375.127 Large Screen Safari/533.4 GoogleTV/b39389",
            "Mozilla/5.0 (X11, U, Linux i686, en-US AppleWebKit/534.1 (KHTML, like Gecko HbbTV/1.1.1 (+PVR,Mstar,OWB,,,",
            "Opera/9.80 (Linux sh4, U, , en, CreNova Build AppleWebKit/533.1 (KHTML like Gecko Version/4.0 Mobile Safari/533.1 HbbTV/1.1 (,CreNova,CNV001,1.0,1.0, FXM-U2FsdGVkX19AfSGBrU5pNwqodai+lZp2xktKFNHDE46SbYGa7Wp+eG5Z56WMDCQu-END, en Presto/2.9 Version",
            "Mozilla/5.0 (X11, Linux i686 AppleWebKit/537.4 (KHTML, like Gecko MWB/1.0 Safari/537.4 HbbTV/1.2.1 (, Mstar, MWB,,,",
            "Mozilla/5.0 (Linux mips, U,HbbTV/1.1.1 (+RTSP,DMM,Dreambox,0.1a,1.0, CE-HTML/1.0, en AppleWebKit/535.19 no/Volksbox QtWebkit/2.2",
            "Mozilla/5.0 (Linux, U, Android 4.1.1, en-gb, POV_TV-HDMI-KB-01 Build/JRO03H AppleWebKit/534.30 (KHTML, like Gecko Version/4.0 Safari/534.30",
            "Mozilla/5.0 (DirectFB, U, Linux 35230, en AppleWebKit/531.2+ (KHTML, like Gecko Safari/531.2+ LG Browser/4.1.4(+3D+SCREEN+TUNER, LGE, 42LW5700-SA, 04.02.28, 0x00000001,, LG NetCast.TV-2011",
            "Mozilla/5.0 (DirectFB, U, Linux mips, en AppleWebKit/531.2+ (KHTML, like Gecko Safari/531.2+ LG Browser/4.0.10(+SCREEN+TUNER, LGE, 42LE5500-SA, 04.02.02, 0x00000001,, LG NetCast.TV-2010",
            "Mozilla/5.0 (Unknown, Linux armv7l AppleWebKit/537.1+ (KHTML, like Gecko Safari/537.1+ LG Browser/6.00.00(+mouse+3D+SCREEN+TUNER, LGE, GLOBAL-PLAT5, 03.07.01, 0x00000001,, LG NetCast.TV-2013/03.17.01 (LG, GLOBAL-PLAT4, wired",
            "Mozilla/5.0 (DirectFB, Linux armv7l AppleWebKit/534.26+ (KHTML, like Gecko Version/5.0 Safari/534.26+ HbbTV/1.1.1 ( ,LGE ,NetCast 3.0 ,1.0 ,1.0M ,",
            "Mozilla/5.0 (Linux, U, Android 3.2, en-us, GTV100 Build/MASTER AppleWebKit/534.13 (KHTML, like Gecko Version/4.0 Safari/534.13",
            "Opera/9.80 (Linux i686, U, fr Presto/2.10.287 Version/12.00 , SC/IHD92 STB",
            "Mozilla/5.0 (FreeBSD, U, Viera, fr-FR AppleWebKit/535.1 (KHTML, like Gecko Viera/1.5.2 Chrome/14.0.835.202 Safari/535.1",
            "Mozilla/5.0 (X11, FreeBSD, U, Viera, de-DE AppleWebKit/537.11 (KHTML, like Gecko Viera/3.10.0 Chrome/23.0.1271.97 Safari/537.11",
            "Opera/9.70 (Linux armv6l , U, CE-HTML/1.0 NETTV/2.0.2, en Presto/2.2.1",
            "Opera/9.80 (Linux armv6l , U, CE-HTML/1.0 NETTV/3.0.1,, en Presto/2.6.33 Version/10.60",
            "Opera/9.80 (Linux mips, HbbTV/1.2.1 (, Philips, , , ,  CE-HTML/1.0 NETTV/4.2.0 PHILIPSTV/1.1.1 Firmware/171.56.0 (PhilipsTV, 1.1.1, en Presto/2.12.362 Version/12.11",
            "Mozilla/5.0 (Linux, U, Android 4.1.1, nl-nl, POV_TV-HDMI-200BT Build/JRO03H AppleWebKit/534.30 (KHTML, like Gecko Version/4.0 Safari/534.30",
            "Roku/DVP-5.2 (025.02E03197A",
            "Roku/DVP-5.0 (025.00E08043A",
            "HbbTV/1.1.1 (,Samsung,SmartTV2013,T-FXPDEUC-1102.2,, WebKit",
            "Mozilla/5.0 (SmartHub, SMART-TV, U, Linux/SmartTV AppleWebKit/531.2+ (KHTML, like Gecko WebBrowser/1.0 SmartTV Safari/531.2+",
            "Mozilla/5.0 (SMART-TV, X11, Linux i686 AppleWebKit/535.20+ (KHTML, like Gecko Version/5.0 Safari/535.20+",
            "Mozilla/5.0 (SmartHub, SMART-TV, U, Linux/SmartTV, Maple2012",
            "Mozilla/5.0 (SmartHub, SMART-TV, U, Linux/SmartTV, Maple2012 AppleWebKit/534.7 (KHTML, like Gecko SmartTV Safari/534.7",
            "Mozilla/4.0 (compatible, Gecko/20041115 Maple 5.0.0 Navi",
            "Mozilla/5.0 (DTV AppleWebKit/531.2+ (KHTML, like Gecko Espial/6.1.5 AQUOSBrowser/2.0 (US01DTV,V,0001,0001",
            "Mozilla/5.0 (DTV AppleWebKit/531.2+ (KHTML, like Gecko Espial/6.0.4",
            "Opera/9.80 (Linux armv7l, InettvBrowser/2.2 (00014A,SonyDTV115,0002,0100 KDL42W650A, CC/GRC Presto/2.12.362 Version/12.11",
            "Opera/9.80 (Linux armv6l, Opera TV Store/5599, (SonyBDP/BDV13 Presto/2.12.362 Version/12.11",
            "Opera/9.80 (Linux sh4, U, HbbTV/1.1.1 (,,,,,, CE-HTML, TechniSat Digit ISIO S, de Presto/2.9.167 Version/11.50",
            "Mozilla/5.0 (DTV, TSBNetTV/T32013713.0203.7DD, TVwithVideoPlayer, like Gecko NetFront/4.1 DTVNetBrowser/2.2 (000039,T32013713,0203,7DD InettvBrowser/2.2 (000039,T32013713,0203,7DD",
            "Mozilla/5.0 (Linux, GoogleTV 3.2, VAP430 Build/MASTER AppleWebKit/534.24 (KHTML, like Gecko Chrome/11.0.696.77 Safari/534.24",
        };

        private static readonly Random _random = new Random();

        private string _title;
        private string _baseUrl;

        public ManaToki(string url, string args) : base(url, args)
        {
            _baseUrl = url.Split('/')[2];

            if (!_downloadFileHeaders.Any())
            {
                _downloadFileHeaders.Add(new Tuple<string, string>("Referer", _baseUrl));
            }
            
        }

        private static string GetRandUserAgent() => _userAgentList[_random.Next(_userAgentList.Count)];


        public override async Task DownloadAll()
        {
            var info = await GetMediaUrls();
            string basePath = $"{DEFAULT_PATH}/manatoki/{_title}";

            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            foreach (Tuple<string, string> chapter in info)
            {
                string chapUrl = chapter.Item1;
                string chapName = RemoveIllegalChars(chapter.Item2);
                string chapPath = $"{basePath}/{chapName}";

                using var messageBar = new MessageBar();
                messageBar.PrintMsg("[ManaToki] downloading", chapName);

                var chapUrls = await GetChapterUrls(chapUrl);
                await DownloadUtil.DownloadAllUrls(chapUrls, chapPath, $"{chapName}", 
                    messageBar, fileNameNumber: true);
            }
        }

        public async Task<IEnumerable<string>> GetChapterUrls(string chapterUrl)
        {
            await Task.Delay(1000);
            var html = await Requests.GetStr(chapterUrl);
            var htmlDataStrs = new Regex("html_data\\+='(.*?)'").Matches(html)
                .Select(data => data.Groups[1].Value);

            string htmlData = string.Empty;
            foreach (string data in htmlDataStrs)
            {
                htmlData += data;
            }


            int i = 0;
            string outStr = string.Empty;
            int l = htmlData.Length;

            for (; i < l; i += 3)
            {
                outStr += (char) Convert.ToInt32(htmlData.Substring(i, 2), 16);
            }

            return new Regex("loading-image\\.gif.*?data.*?\"(.*?)\"", RegexOptions.Singleline)
                .Matches(outStr)
                .Select(match => match.Groups[1].Value);
        }


        // First string is the url, second is the title
        public async Task<IEnumerable<Tuple<string, string>>> GetMediaUrls()
        {
            await Task.Delay(1000);
            var html = await Requests.GetStr(Url);

            _title = new Regex("<meta name=\"subject\" content=\"(.*?)\"")
                .Match(html).Groups[1].Value;
            _title = RemoveIllegalChars(_title);

            return new Regex("list-item.*?href=\"(.*?)\".*?\\/span>(.*?)<",
                 RegexOptions.Singleline)
                 .Matches(html)
                 .Select(match =>
                 new Tuple<string, string>(match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim()));
        }
    }
}
