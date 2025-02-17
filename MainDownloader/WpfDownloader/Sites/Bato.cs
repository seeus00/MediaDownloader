using ControlzEx.Standard;
using Downloader.Util;
using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.Util.CryptoUtil;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public struct BatoChapter
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }

    public class Bato : Site
    {
        public Bato(string url, string args) : base(url, args)
        {

        }

        public async Task DownloadChapterUrls(string path, UrlEntry entry, BatoChapter chapter)
        {
            string chapterPath = $"{path}/{RemoveIllegalChars(chapter.Title)}";
            if (!Directory.Exists(chapterPath)) Directory.CreateDirectory(chapterPath);

            string html = await Requests.GetStr(chapter.Url);
            var imgUrls = new Regex("imgHttpLis = \\[(.*?)\\]", RegexOptions.Singleline)
                .Match(html).Groups[1].Value
                .Split(',')
                .Select(url => url.Replace("\"", "").Trim())
                .ToList();

            string batoPass = new Regex("const batoPass = (.*?);", RegexOptions.Singleline)
                .Match(html).Groups[1].Value;
            string batoWord = new Regex("const batoWord = \"(.*?)\"", RegexOptions.Singleline)
                .Match(html).Groups[1].Value;

            using var engine = new V8ScriptEngine();
            engine.Execute($"var result = eval('{batoPass}')");

            string passphrase = (string)engine.Script.result;
            string jsonData = AesUtil.DecryptAes(batoWord, passphrase);
            var data = JsonParser.Parse(jsonData);

            var newImgUrls = data.Select((query, ind) => $"{imgUrls[ind]}?{query}");
            await DownloadUtil.DownloadAllUrls(newImgUrls, chapterPath, entry, showProgress: false, fileNameNumber: true, setDownloadPath: false);
        }


        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving info";

            var htmlBytes = await Requests.GetBytes(Url);
            string html = Encoding.UTF8.GetString(htmlBytes);

            var chapters = new Regex("chap.*?href=\"(.*?)\" >.*?<b.*?>(.*?)<", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => new BatoChapter()
                { 
                    Title = System.Net.WebUtility.HtmlDecode($@"{match.Groups[2].Value.Trim()}"),
                    Url = $"https://bato.to{match.Groups[1].Value}"
                })
                .SkipLast(1);



            string title = RemoveIllegalChars(new Regex("og:title.*?content=\"(.*?)\"", RegexOptions.Singleline)
                .Match(html)
                .Groups[1].Value.Trim());
            string newPath = $"{DEFAULT_PATH}/bato/{title}";

            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

            entry.DownloadPath = newPath;
            entry.Name = $"[Bato] {title}";

            entry.StatusMsg = "Downloading";
            entry.FilesMsg = $"0/{chapters.Count()}";

            int ind = 1;
            var semaphoreSlim = new SemaphoreSlim(2);
            var tasks = chapters.Select(async chapter =>
            {
                if (entry.CancelToken.IsCancellationRequested) return;

                await semaphoreSlim.WaitAsync();
                try
                {
                    await DownloadChapterUrls(newPath, entry, chapter);

                    int percent = (ind * 100) / chapters.Count();
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        entry.Bar.Value = percent;

                        entry.FilesMsg = $"{ind}/{chapters.Count()}";
                    }), DispatcherPriority.Background);
                    ind++;
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            });

            await Task.WhenAll(tasks);
            entry.StatusMsg = (entry.CancelToken.IsCancellationRequested) ? "Cancelled" : "Finished";
        }
    }
}
