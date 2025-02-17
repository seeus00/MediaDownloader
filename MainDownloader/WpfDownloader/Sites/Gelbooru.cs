using Downloader.Data;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Gelbooru : Site
    {
        public Gelbooru(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            var tags = Url.Split("tags=").Last();
            tags = Uri.UnescapeDataString(tags).Trim();

            var path = $"{DEFAULT_PATH}/gelbooru/{tags}";

            entry.StatusMsg = "Retrieving";
            entry.Name = "[Gelbooru] " + tags;

            var entries = await GetMediaUrls();
            await DownloadUtil.DownloadAllUrls(entries, path, entry);
        }

        public async Task<IEnumerable<string>> GetMediaUrls()
        {
            var userTags = Url.Split("&tags=").Last();

            var mediaUrls = new List<string>();
            int currPg = 1;
            while (true)
            {
                var pageUrl = $"https://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=42&pid={currPg}&tags={userTags}";
                var html = await Requests.GetStr(pageUrl);
                if (string.IsNullOrEmpty(pageUrl))
                {
                    break;
                }

                var matches = 
                    new Regex("\\<file_url\\>(.*?)<\\/file_url>", 
                    RegexOptions.Singleline)
                    .Matches(html);

                if (!matches.Any()) break;

                //var results = 
                //    matches.Select(match =>
                //    new ImgEntry()
                //    {
                //        Url = match.Groups[1].Value,
                //        Tags = match.Groups[2].Value.Replace('"', ' ').Split()
                //    });
                var results =
                    matches.Select(match => match.Groups[1].Value);

                mediaUrls.AddRange(results);
                currPg += 1;
            }

            return mediaUrls;
        }
    }
}
