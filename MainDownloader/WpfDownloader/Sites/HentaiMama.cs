using Downloader.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class HentaiMamaEntry
    {
        public string EpisodeSlug { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    internal class HentaiMama : Site
    {
        private string _slug;

        public HentaiMama(string url, string args) : base(url, args)
        {
            _slug = Url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = "[HentaiMama] " + _slug;

            string newPath = $"{DEFAULT_PATH}/hentaimama/{_slug}";

            string html = await Requests.GetStr(Url);
            var entries =  new Regex("item se episodes.*?data-src=\"(.*?)\".*?<a href=\"(.*?)\"", 
                RegexOptions.Singleline)
                .Matches(html)
                .Select(match => new HentaiMamaEntry()
                {
                    EpisodeSlug = match.Groups[2].Value.TrimEnd('/').Split('/').Last(),
                    ThumbnailUrl = match.Groups[1].Value,
                    VideoUrl = match.Groups[2].Value
                });

            entry.StatusMsg = "Downloading";
            entry.DownloadPath = newPath;

            Directory.CreateDirectory(newPath);

            string coverUrl = new Regex("data-src=\"(.*?)\"", RegexOptions.Singleline).Match(html).Groups[1].Value;
            await Requests.DownloadFileFromUrl(coverUrl, newPath, fileName: $"{_slug}-cover");

            int ind = 1;
            foreach (var henEntry in entries)
            {
                await Requests.DownloadFileFromUrl(henEntry.ThumbnailUrl, newPath, fileName: $"{henEntry.EpisodeSlug}-thumbnail");

                string videoUrl = await GetVideoUrl(henEntry.VideoUrl);
                await Requests.DownloadFileFromUrl(videoUrl, newPath);

                int percent = (ind * 100) / entries.Count();
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    entry.Bar.Value = percent;

                    entry.FilesMsg = $"{ind}/{entries.Count()}";
                }), DispatcherPriority.Background);
                ind++;
            }

            entry.StatusMsg = "Finished";
        }

        private async Task<string> GetVideoUrl(string episodeUrl)
        {
            string episodeHtml = await Requests.GetStr(episodeUrl);

            string aParam = new Regex("a:'(.*?)'", RegexOptions.Singleline).Match(episodeHtml).Groups[1].Value;

            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("action", "get_player_contents"),
                new KeyValuePair<string, string>("a", aParam),
            };
           
            string result = await Requests.GetStrPost("https://hentaimama.io/wp-admin/admin-ajax.php", data);
            string b64Part = new Regex("new.*?\\?p=(.*?)\"").Match(result).Groups[1].Value;

            byte[] b64Data = Convert.FromBase64String(b64Part.Trim('/').Trim('\\'));
            string decodedString = Encoding.UTF8.GetString(b64Data);

            return $"https://javprovider.com/{decodedString}";
        }
    }
}
