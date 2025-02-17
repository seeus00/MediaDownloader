using Downloader.Util;
using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.Util.CryptoUtil;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    internal class BatoV2 : Site
    {
        public BatoV2(string url, string args) : base(url, args)
        {
        }

        public async Task DownloadChapterUrls(string path, UrlEntry entry, BatoChapter chapter)
        {
            await Task.Delay(500);

            string chapterPath = $"{path}/{RemoveIllegalChars(chapter.Title)}";
            if (!Directory.Exists(chapterPath)) Directory.CreateDirectory(chapterPath);

            var resp = await Requests.Get(chapter.Url);
            resp.EnsureSuccessStatusCode();

            string html = await resp.Content.ReadAsStringAsync();

            var imgUrls = new Regex("\\\\&quot;(.*?)\\\\&quot", RegexOptions.Singleline)
                .Matches(html)
                .Select(match => HttpUtility.HtmlDecode(match.Groups[1].Value).Trim());
            await DownloadUtil.DownloadAllUrls(imgUrls, chapterPath, entry, showProgress: true, fileNameNumber: true, setDownloadPath: false);
        }


        public override async Task DownloadAll(UrlEntry entry)
        {
            var uri = new Uri(Url);
            string host = uri.Host;

            var resp = await Requests.Get(Url);
            resp.EnsureSuccessStatusCode();

            string html = await resp.Content.ReadAsStringAsync();
            var chapters = new Regex("href=\\\"(\\/title\\/[0-9]+.*?\\/[0-9]+.*?)\\\".*?>(.*?)<", RegexOptions.Singleline)
                .Matches(html);

            string title = chapters[0].Groups[2].Value.Trim();

            var newChapters = chapters
                .Skip(1)
                .Select(match => new BatoChapter()
                {
                    Title = match.Groups[2].Value.Trim(),
                    Url = $"https://{host}{match.Groups[1].Value.Trim()}"
                });

            string newPath = $"{DEFAULT_PATH}/bato/{RemoveIllegalChars(title)}";
            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

            entry.DownloadPath = newPath;
            entry.Name = $"[Bato] {title}";

            entry.StatusMsg = "Downloading";
            entry.FilesMsg = $"0/{newChapters.Count()}";

            entry.SubItems = new ObservableCollection<UrlEntry>(newChapters.Select(chapter => new UrlEntry()
            {
                Name = chapter.Title,
                StatusMsg = "Downloading"
            }));

            int ind = 1;
            var semaphoreSlim = new SemaphoreSlim(2);
            var tasks = newChapters.Select(async (chapter, i) =>
            {
                if (entry.CancelToken.IsCancellationRequested) return;

                await semaphoreSlim.WaitAsync();
                try
                {
                    await DownloadChapterUrls(newPath, entry.SubItems[i], chapter);

                    int percent = (ind * 100) / newChapters.Count();
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        entry.Bar.Value = percent;

                        entry.FilesMsg = $"{ind}/{newChapters.Count()}";
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
