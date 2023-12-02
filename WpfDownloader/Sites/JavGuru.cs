using Downloader.Util;
using MonoTorrent;
using MonoTorrent.BEncoding;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Shapes;
using WpfDownloader.Util;
using WpfDownloader.Util.UserAgent;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class JavGuru : Site
    {
        public JavGuru(string url, string args) : base(url, args)
        {

        }

        private string DecodeOlid(string olid)
        {
            int x = olid.Length - 1;
            var encoded = new StringBuilder();

            while (x >= 0)
            {
                encoded.Append(olid[x]);
                x--;
            }

            return encoded.ToString();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";

            string path = $"{DEFAULT_PATH}/jav_guru";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            entry.DownloadPath = path;

            var headers =
                new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("User-Agent", UserAgentUtil.CURR_USER_AGENT),
                    new Tuple<string, string>("Referer", Url),

                };


            string html = await Requests.GetStr(Url, headers);
            var urls = new Regex("iframe_url\":\"(.*?)\"")
                .Matches(html)
                .Select(match => Base64Util.DecodeB64Str(match.Groups[1].ToString()))
                .ToList();

            int streamTapeInd  = new Regex("STREAM ([A-Z]+)<")
                .Matches(html)
                .Select((match, i) => new { Str = match.Groups[1].ToString(), Index = i } )
                .First(val => val.Str == "ST").Index;


            string streamTape = urls[streamTapeInd];
            html = await Requests.GetStr(streamTape, headers);

            

            string queryId = new Regex("jav\\.guru\\/searcho\\/\\?(.*?)=").Matches(html).First().Groups[1].ToString();
            string olid = new Regex("OLID = '(.*?)'").Match(html).Groups[1].ToString();

            string decodedOlid = DecodeOlid(olid);
            string video_stream_url = $"https://jav.guru/searcho/?{queryId}={decodedOlid}";

            html = await Requests.GetStr(video_stream_url, headers);
            var videoConfig = new Regex("vidconfig = (.*?);").Match(html).Groups[1].ToString();

            var config = JsonParser.Parse(videoConfig);
            string token = new Regex("token=(.*?)'").Matches(html).First().Groups[1].ToString();

            string urlParams = new Regex("remotecaption\\\\\\/(.*?)&token").Match(config["cors"].ToString()).Groups[1].ToString();
            string videoUrl = $"https://streamtape.xyz/get_video?id={config["id"].ToString()}&{urlParams}&token={token}&stream=1";

            headers =
                new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("User-Agent", UserAgentUtil.CURR_USER_AGENT),
                };

            entry.StatusMsg = "Downloading";
            await Requests.DownloadFileFromUrl(videoUrl, path, headers: headers, cancelToken: entry.CancelToken, entry: entry);

            //string encodedString = new Regex("iframe_url\":\"(.*?)\"").Matches(html).First().Groups[1].ToString();
            //byte[] b64Data = Convert.FromBase64String(encodedString);
            //string posterUrl = Encoding.UTF8.GetString(b64Data);

            //var headers = new List<Tuple<string, string>>()
            //{
            //    new Tuple<string, string>("Referer", posterUrl)
            //};

            //string hash = new Regex("ed=(.*?)&").Match(posterUrl).Groups[1].ToString();
            //string newUrl = $"https://jav.guru/searcho/?er={string.Join("", hash.Reverse())}";

            //var resp = await Requests.Get(newUrl, headers: headers, entry.CancelToken);
            //string location = resp.RequestMessage.RequestUri.ToString();
            //string id = location.Split('/').Last();

            //var values = new List<KeyValuePair<string, string>>()
            //{
            //    new KeyValuePair<string, string>("r", "https://jav.guru/"),
            //    new KeyValuePair<string, string>("d", "vanfem.com"),
            //};

            //string jsonStr = await Requests.GetStrPost($"https://vanfem.com/api/source/{id}", values);
            //var data = JsonParser.Parse(jsonStr);

            //if (data["success"].ToString() != "true")
            //{
            //    entry.StatusMsg = "Api Error";
            //    return;
            //}

            //string title = RemoveIllegalChars(Url.Split('/').Last());
            //string newPath = $"{DEFAULT_PATH}/jav_guru";
            //if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

            //string highestQualityVideoUrlRedirect = data["data"]
            //    .Where(video => video["label"].ToString() == "720p")
            //    .Select(video => video["file"].ToString()).First();

            //headers = new List<Tuple<string, string>>()
            //{
            //    new Tuple<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9"),
            //    new Tuple<string, string>("accept-encoding", "gzip, deflate, br"),
            //    new Tuple<string, string>("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36 Edg/107.0.1418.52"),
            //    new Tuple<string, string>("connection", "Keep-Alive")
            //};

            //entry.DownloadPath = newPath;
            //entry.Name = title;
            //entry.StatusMsg = "Downloading";
            //await Requests.DownloadFileFromUrl(highestQualityVideoUrlRedirect, newPath, headers:headers, fileName: title + ".mp4", 
            //    entry: entry, cancelToken: entry.CancelToken);

            entry.StatusMsg = (entry.CancelToken.IsCancellationRequested) ? "Cancelled" : "Finished";
        }
    }
}
