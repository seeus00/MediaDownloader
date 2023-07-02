using ChromeCookie;
using Downloader.Util;
using MonoTorrent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Shell;
using WpfDownloader.Util.HttpExtensions;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class KskMoe : Site
    {
        private static CookieContainer cookieContainer = null;

        private static readonly List<Tuple<string, string>> HEADERS =
           new List<Tuple<string, string>>()
           {
                new Tuple<string, string>("User-Agent", MainWindow.PERSONAL_CONFIG["user_agent"]),
                new Tuple<string, string>("Origin", "https://ksk.moe"),
                new Tuple<string, string>("Host", "ksk.moe"),
           };

        public KskMoe(string url, string args) : base(url, args)
        {
            HEADERS.Add(new Tuple<string, string>("Referer", Url));
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            if (cookieContainer == null)
            {
                var baseAddress = new Uri("https://ksk.moe");
                cookieContainer = new CookieContainer();

                cookieContainer.Add(baseAddress, ChromeCookies.GetCookies(".ksk.moe"));
                Requests.AddCookies(cookieContainer, baseAddress);
            }

            string html = await Requests.GetStr(Url);

            string downloadId = new Regex("action=\"\\/download\\/(.*?)\"", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => match.Groups[1].Value.Trim())
                .First();


            var hashes = new Regex("name=\"hash\" value=\"(.*?)\"", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => match.Groups[1].Value.Trim());

            //First hash if for the original, second is for resampled 
            string hash = hashes.First();
            string postUrl = $"https://ksk.moe/download/{downloadId}";

            var titles = new Regex("id=\"metadata\".*?<h[0-9]+>(.*?)<.*?<h[0-9]+>(.*?)<", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => match.Groups[1].Value.Trim());

            string title = HttpUtility.HtmlDecode(titles.Last());
            entry.Name = $"[KskMoe] " + title;

            string artist = HttpUtility.HtmlDecode(
                new Regex("id=\"metadata\".*?artists\\/(.*?)\"", RegexOptions.Singleline)
                    .Match(html)
                    .Groups[1]
                    .Value.Trim()); 

            //Convert array of tags into string
            string tags = string.Join(',', new Regex("\\/tags\\/(.*?)\"", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => HttpUtility.HtmlDecode(match.Groups[1].Value.Trim())));

            var thumbnailUrls = new Regex("\\/read.*?img.*?src=\"(.*?)\"", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => HttpUtility.HtmlDecode(match.Groups[1].Value.Trim())).Skip(1);

            var fullImgUrls = new List<string>();
            foreach (string thumbnailUrl in thumbnailUrls)
            {
                var split = thumbnailUrl.Split('/');
                string fileName = split.Last();
                string baseDomain = string.Join('/', split.Take(3));

                string newUrl = $"{baseDomain}/original/{downloadId}/{fileName}";
                fullImgUrls.Add(newUrl);
            }

            string currPath = $"{DEFAULT_PATH}/KskMoe/{RemoveIllegalChars(title)}";
            if (!Directory.Exists(currPath)) Directory.CreateDirectory(currPath);


            entry.DownloadPath = currPath;
            await DownloadUtil.DownloadAllUrls(fullImgUrls, currPath, entry, fileNameNumber: true);


            /*
            entry.StatusMsg = "Retrieving ZIP";
            
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("hash", hash)
            };

            var resp = await Requests.PostAsync(postUrl, data, headers: HEADERS);
            var zipPath = $"{currPath}/temp.zip";

            entry.StatusMsg = "Downloading ZIP";
            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write,
                        FileShare.None, useAsync: true, bufferSize: 4096))
            {
                await HttpClientExtensions.CopyToAsyncProgress(resp, fs, entry: entry,
                    cancellationToken: entry.CancelToken);
            }

            entry.StatusMsg = "Extracting ZIP";
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, currPath));

            //Delete cbz
            entry.StatusMsg = "Deleting ZIP";
            await Task.Run(() => File.Delete(zipPath));

            

            entry.StatusMsg = "Renaming files";
            //Rename files 

            int pg = 1;
            foreach (string filePath in Directory.GetFiles(currPath))
            {
                string ext = filePath.Split('.').Last();
                File.Move(filePath, $"{currPath}/{pg}.{ext}");

                pg++;
            }
            */
            entry.StatusMsg = "Finished";
        }
    }
}
