using Downloader.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class ImgObj
    {
        public string ImgUrl { get; set; }
        public string ImgName { get; set; }
        public string Tags { get; set; }
    }

    public class SafeBooru : Site
    {
        private string tags;


        public SafeBooru(string url, string args) : base(url, args)
        {
            tags = HttpUtility.UrlDecode(Url.Split("tags=").Last().Trim());
        }

        public async Task<IEnumerable<ImgObj>> GetMediaUrls()
        {
            var imgs = new List<ImgObj>();
            int currPg = 1;
            while (true)
            {
                string apiUrl = $"https://safebooru.org/index.php?page=dapi&s=post&q=index&pid={currPg}&limit=20&json=1&tags={tags}";
                string jsonStr = await Requests.GetStr(apiUrl);

                var data = JsonParser.Parse(jsonStr);
                if (!data.Any()) break;

                var res = data.Select(obj => new ImgObj()
                {
                    ImgUrl = $"https://safebooru.org//images/{obj["directory"].Value}/{obj["image"].Value}",
                    ImgName = obj["image"].Value.Split('.').First(),
                    Tags = string.Join(',', obj["tags"].Value.Split())
                });

                if (!res.Any()) break;
                imgs.AddRange(res);

                currPg++;
            }

            return imgs;
        }


        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = $"[SafeBooru] {tags}";

            string path = $"{DEFAULT_PATH}/safebooru/{tags}";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var mediaUrls = await GetMediaUrls();
            entry.StatusMsg = "Downloading imgs";

            var imgUrls = mediaUrls.Select(media => media.ImgUrl);
            await DownloadUtil.DownloadAllUrls(imgUrls, path, entry);


            if (Args == "tags")
            {
                entry.StatusMsg = "Creating tags";


                int ind = 1;
                //Write tags 
                foreach (var media in mediaUrls)
                {
                    string textFilePath = $"{path}/{media.ImgName}.txt";
                    await File.WriteAllTextAsync(textFilePath, media.Tags);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        entry.FilesMsg = $"{ind}/{mediaUrls.Count()}";
                        entry.Bar.Value = (double)ind / mediaUrls.Count() * 100f;
                    });

                    ind++;
                }
            }

            entry.StatusMsg = "Finished";
        }
    }
}
