using Downloader.Data;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class FakkuCC : Site
    {
        private static string METADATA_API = "https://fakku.cc/api/archives/{0}/metadata";
        private static string FILES_API = "https://fakku.cc/api/archives/{0}/files?force=false";

        private string _id;
        private string _title;

        private string _newPath;

        public FakkuCC(string url, string args) : base(url, args)
        {
            _id = Url.Split("id=").Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";

            var urls = await GetMediaUrls(entry);
            await DownloadUtil.DownloadAllUrls(urls, _newPath, entry);
        }

        public async Task<IEnumerable<ImgData>> GetMediaUrls(UrlEntry entry)
        {
            string metaDataJson = await Requests.GetStr(string.Format(METADATA_API, _id));
            var metadata = JsonParser.Parse(metaDataJson);

            _title = metadata["title"].ToString();
            entry.Name = _title;

            _newPath = $"{DEFAULT_PATH}/FakkuCC/{_title}";

            //Write tags
            var info = new JDict();
            info["title"] = new JType(_title);
            info["tags"] = new JArray(metadata["tags"].ToString().Split(',').Select(tag => tag.Trim()));

            await TagWriter.WriteTags(info, _newPath);

            var filesJson = await Requests.GetStr(string.Format(FILES_API, _id));
            var files = JsonParser.Parse(filesJson);

            return files["pages"].Select(url => new ImgData()
            {
                Url = $"https://fakku.cc/{url.ToString().Substring(1)}",
                Filename = url.ToString().Split("path=").Last()
            });

        }
    }
}
