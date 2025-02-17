using Downloader.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader.Sites
{
    public class PandaArchive : Site
    {
        private static readonly string API_URL = "https://panda.chaika.moe/api?archive={0}";

        public PandaArchive(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll()
        {
            string id = Url.Split('/').Last();
            using var messageBar = new MessageBar();
            messageBar.PrintMsg("[PandaArchive] downloading", id);

            string jsonStr = await Requests.GetStr(string.Format(API_URL, id));

            var data = JsonParser.Parse(jsonStr);

            string title = RemoveIllegalChars(data["title"].Value);
            string path = $"{DEFAULT_PATH}/pandaArchive/{id}-{title}";

            string zipUrl = $"https://panda.chaika.moe{data["download"].Value}";
            var tags = string.Join(' ', data["tags"].Select(tag => tag.Value));

            await TagWriter.WriteTags(tags, path);

            using (var zipStream = new MemoryStream(await Requests.GetBytes(zipUrl)))
            using (var zip = new ZipArchive(zipStream))
            using (var progressBar = new ProgressBar(zip.Entries.Count, title, messageBar.CurrLineNum))
            {
                var tasks = zip.Entries.Select(async (entry, ind) =>
                {
                    //string ext = entry.Name.Split('.').Last();
                    await Task.Run(() =>
                        entry.ExtractToFile(Path.Combine(path, entry.Name)));

                    progressBar.Update(ind);
                }); 

                await Task.WhenAll(tasks);
            }
        }
    }
}
