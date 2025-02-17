using Downloader.Util;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.Util;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class MissAv : Site
    {
        private readonly List<Tuple<string, string>> HEADERS = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("Referer", "https://missav.com"),
            new Tuple<string, string>("Origin", "https://missav.com"),

        };
        private string _code;

        public MissAv(string url, string args) : base(url, args)
        {
            _code = Url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = "[MissAv] " + _code;

            var path = $"{DEFAULT_PATH}/missav/{_code}";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            HEADERS.Add(new Tuple<string, string>("User-Agent", RandomUA.RandomUserAgent));

            string html = await Requests.GetStr(Url);
            string jsScript =
                new Regex("eval\\((.*?)const", RegexOptions.Singleline).Match(html)
                .Groups[1].Value.Trim();
            string viewApi = new Regex("axios\\.get\\('(.*?)'", RegexOptions.Singleline).Match(html)
                .Groups[1].Value.Trim();

            var strBuilder = new StringBuilder(jsScript);

            strBuilder.Insert(0, "eval(");
            jsScript = strBuilder.ToString();

            ScriptEngine engine = new V8ScriptEngine();
            string m3u8PlaylistUrl = engine.Evaluate(jsScript) as string;

            //await Requests.GetStr(viewApi);
            string playListData = await Requests.GetStr(m3u8PlaylistUrl, HEADERS);
            playListData = playListData.Trim('\n');

            string videoRes = playListData.Split('\n').Last().Split('/')[0]; 
            string videoResAndFile = playListData.Split('\n').Last();

            var uri = new Uri(m3u8PlaylistUrl);
            var noLastSegment = string.Format("{0}://{1}", uri.Scheme, uri.Authority);
            for (int i = 0; i < uri.Segments.Length - 1; i++)
            {
                noLastSegment += uri.Segments[i];
            }

            noLastSegment = noLastSegment.Trim("/".ToCharArray()); // remove trailing `/`
            string m3u8VideoUrl = $"{noLastSegment}/{videoResAndFile}";
            string finalVideoMp4Path = $"{path}/{_code}.mp4";

            entry.StatusMsg = "Converting";
            await Task.Run(() => VideoConverter.M3u8UrlToMp4(m3u8VideoUrl, finalVideoMp4Path, entry, headers: HEADERS));
            entry.StatusMsg = "Finished";
        }
    }
}
