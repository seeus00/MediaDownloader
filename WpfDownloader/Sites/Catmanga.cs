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
    public class Catmanga : Site
    {
        private string _buildId;
        private string _seriesId;
        private string _title;

        public Catmanga(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll()
        {
            using var messageBar = new MessageBar();
            messageBar.PrintMsg("[Catmanga] downloading", Url);

            var info = await GetChapterInfo();
            
            string basePath = $"{DEFAULT_PATH}/catmanga/{_title}";
            foreach (var chapter in info)
            {
                string chapName = RemoveIllegalChars(chapter.Item1);
                string chapNum = chapter.Item2;
                string chapPath = $"{basePath}/{chapNum} - {chapName}";

                string chapApiUrl = $"https://catmanga.org/_next/data/{_buildId}/series/{_seriesId}/{chapNum}.json";
                var chapterUrls = await GetChapterUrls(chapApiUrl);

                await DownloadUtil.DownloadAllUrls(chapterUrls, chapPath, $"{chapNum} - {chapName}", messageBar, fileNameNumber: true); ;
            }
        }

        public async Task<IEnumerable<Tuple<string, string>>> GetChapterInfo()
        {
            var html = await Requests.GetStr(Url);
            var jsonStr = new Regex("type=\"application\\/json\">(.*?)<").Match(html).Groups[1].Value;
            var data = JsonParser.Parse(jsonStr);

            _buildId = data["buildId"].Value;
            _title = RemoveIllegalChars(data["props"]["pageProps"]["series"]["title"].Value);
            _seriesId = data["props"]["pageProps"]["series"]["series_id"].Value;
            
            return data["props"]["pageProps"]["series"]["chapters"]
                .Select(chapter => 
                new Tuple<string, string>(chapter["title"].Value, chapter["number"].Value));

        }

        public async Task<IEnumerable<string>> GetChapterUrls(string chapterUrl)
        {
            var jsonStr = await Requests.GetStr(chapterUrl);
            var data = JsonParser.Parse(jsonStr);

            return data["pageProps"]["pages"].Select(page => page.Value);
        }
    }
}
