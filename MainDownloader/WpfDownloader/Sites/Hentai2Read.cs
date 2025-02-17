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
    public class Hentai2Read : Site
    {
        private static readonly string IMAGE_SERVER = "https://static.hentaicdn.com/hentai";
        private static readonly List<Tuple<string, string>> USER_AGENTS =
            new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Referer", "https://hentai2read.com")
            };


        private string _title;

        public Hentai2Read(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll()
        {
            using var messageBar = new MessageBar();
            messageBar.PrintMsg("[Hentai2Read] downloading", Url);

            var info = await GetMediaUrls();
            string basePath = $"{DEFAULT_PATH}/hentai2read/{_title}";

            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            foreach (Tuple<string, string> chapter in info)
            {
                string chapUrl = chapter.Item1;
                string chapName = RemoveIllegalChars(chapter.Item2);
                string chapPath = $"{basePath}/{chapName}";

                var chapImgs = await GetChapterImages(chapUrl);
                await DownloadUtil.DownloadAllUrls(chapImgs, chapPath, $"{chapName}", 
                    messageBar, headers: USER_AGENTS, fileNameNumber: true);
            }
        }

        public async Task<IEnumerable<string>> GetChapterImages(string chapterUrl)
        {
            string html = await Requests.GetStr(chapterUrl);
            var images = new Regex("gData.*?images' : \\[\"(.*?)\"\\]",
                RegexOptions.Singleline).Match(html)
                .Groups[1].Value
                .Replace("\\", string.Empty)
                .Replace("\"", string.Empty)
                .Split(',');

            return images.Select(img => $"{IMAGE_SERVER}{img}");
        }


        // Returns chapter url + title 
        public async Task<IEnumerable<Tuple<string, string>>> GetMediaUrls()
        {
            string html = await Requests.GetStr(Url);

            _title = new Regex("fa fa-book.*?href=\".*?\">(.*?)<", RegexOptions.Singleline)
                .Match(html).Groups[1].Value
                .Trim();
            _title = RemoveIllegalChars(_title);

            return new Regex("class=\"media\".*?</div>.*?href=\"(.*?)\">(.*?)<"
                , RegexOptions.Singleline)
                .Matches(html)
                .Select(m =>
                    new Tuple<string, string>(m.Groups[1].Value, m.Groups[2].Value.Trim()));
        }
    }
}
