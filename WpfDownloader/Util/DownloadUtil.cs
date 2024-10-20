﻿using Downloader.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.Data.Imaging;
using WpfDownloader.WpfData;

namespace Downloader.Util
{
    public static class DownloadUtil
    {
        private static readonly int MAX_THREADS = 5;

        public static async Task DownloadAllUrls(IEnumerable<ImgData> urls, string path,
            UrlEntry entry, List<Tuple<string, string>> headers = null, bool dulpicateFileName = false,
            bool showProgress = true, bool setDownloadPath = true,
            int delayInBetween = -1, List<ZipToGifData> gifEntries = null, int maxThreads = 5)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            entry.StatusMsg = "Downloading";

            if (setDownloadPath) entry.DownloadPath = path;
            if (showProgress) entry.FilesMsg = $"0/{urls.Count()}";

            int ind = 0;
            var semaphoreSlim = new SemaphoreSlim(maxThreads);


            var orderedUrls = new Dictionary<int, ImgData>();
            int i = 0;
            foreach (var url in urls) orderedUrls.Add(i++, url);

            var tasks = new List<Task>();
            foreach (var pair in orderedUrls)
            {
                if (entry.CancelToken.IsCancellationRequested) break;
                var downTask = Task.Run(async () =>
                {
                    await semaphoreSlim.WaitAsync();
                    try
                    {
                        if (entry.CancelToken.IsCancellationRequested) return;
                        if (delayInBetween != -1)
                        {
                            await Task.Delay(delayInBetween);
                        }

                        await Requests.DownloadFileFromUrl(pair.Value.Url, path, headers,
                                fileName: pair.Value.Filename, duplicateFileName: false, cancelToken: entry.CancelToken);

                        if (showProgress)
                        {
                            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ind++;
                                entry.Bar.Value = (ind * 100.0) / urls.Count();
                                entry.FilesMsg = $"{ind}/{urls.Count()}";
                            }));
                        }
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                }, entry.CancelToken);
                tasks.Add(downTask);
            }

            await Task.WhenAll(tasks);


            if (gifEntries != null)
            {
                entry.StatusMsg = "zip -> gif";
                await GifWriter.ZipToGifBatch(gifEntries);
            }

            if (showProgress) entry.StatusMsg = "Finished";
            if (entry.CancelToken.IsCancellationRequested) entry.StatusMsg = "Cancelled";


            tasks.Clear();
        }


        public static async Task DownloadAllUrls(IEnumerable<string> urls, string path,
            UrlEntry entry, List<Tuple<string, string>> headers = null,
            bool fileNameNumber = false, bool dulpicateFileName = false,
            bool showProgress = true, bool setDownloadPath = true,
            int delayInBetween = -1, List<ZipToGifData> gifEntries = null, int maxThreads = 5,
            bool overrideDownloadedFiles = true, bool redirectUri = false)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            entry.StatusMsg = "Downloading";

            if (setDownloadPath) entry.DownloadPath = path;
            if (showProgress) entry.FilesMsg = $"0/{urls.Count()}";
            
            int ind = 0;
            var semaphoreSlim = new SemaphoreSlim(maxThreads);

            var orderedUrls = new Dictionary<int, string>();


            entry.SubItems = new ObservableCollection<UrlEntry>(urls.Select(url => new UrlEntry()
            {
                Name = url.Split('/').Last(),
                StatusMsg = UrlEntry.DOWNLOADING
            }));

            int i = 0;
            foreach (string url in urls) orderedUrls.Add(i++, url);


            var tasks = new List<Task>();
            foreach (var pair in orderedUrls)
            {
                if (entry.CancelToken.IsCancellationRequested) break;
                var downTask = Task.Run(async () =>
                {
                    await semaphoreSlim.WaitAsync();
                    try
                    {
                        if (entry.CancelToken.IsCancellationRequested) return;
                        if (delayInBetween != -1)
                        {
                            await Task.Delay(delayInBetween);
                        }

                        if (fileNameNumber)
                        {
                            await Requests.DownloadFileFromUrl(pair.Value, path, headers: headers,(pair.Key + 1).ToString(), 
                                duplicateFileName: dulpicateFileName, cancelToken: entry.SubItems[pair.Key].CancelToken, 
                                entry: entry.SubItems[pair.Key], overrideFile: overrideDownloadedFiles, redirectUrl: redirectUri);
                        }
                        else
                        {
                            await Requests.DownloadFileFromUrl(pair.Value, path, headers: headers,
                                duplicateFileName: dulpicateFileName, cancelToken: entry.SubItems[pair.Key].CancelToken, 
                                entry: entry.SubItems[pair.Key], overrideFile: overrideDownloadedFiles, redirectUrl: redirectUri);
                        }
                        if (showProgress)
                        {
                            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ind++;
                                entry.Bar.Value = (ind * 100.0) / urls.Count();
                                entry.FilesMsg = $"{ind}/{urls.Count()}";
                            }));
                        }
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                }, entry.CancelToken);
                tasks.Add(downTask);
            }

            await Task.WhenAll(tasks);


            if (gifEntries != null)
            {
                entry.StatusMsg = "zip -> gif";
                await GifWriter.ZipToGifBatch(gifEntries);
            }

            if (showProgress) entry.StatusMsg = "Finished";
            if (entry.CancelToken.IsCancellationRequested) entry.StatusMsg = "Cancelled";


            tasks.Clear();
        }

        public static async Task DownloadAllUrlsAndtags (
            IEnumerable<ImgEntry> urlsAndTags, 
            string path, UrlEntry entry, 
            List<Tuple<string, string>> headers = null,
            bool fileNameNumber = false)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            entry.StatusMsg = "Downloading";
            entry.DownloadPath = path;

            entry.FilesMsg = $"0/{urlsAndTags.Count()}";

            int ind = 0;
            var semaphoreSlim = new SemaphoreSlim(MAX_THREADS);
            var tasks = urlsAndTags.Select(async (item, pg) =>
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    if (entry.CancelToken.IsCancellationRequested) return;

                    string fileName = (fileNameNumber) ? (pg + 1).ToString() : string.Empty;
                    await Requests.DownloadFileFromUrl(item.Url, path, headers,
                            fileName);

                    var tags = new JDict();
                    tags["tags"] = new JArray(item.Tags);

                    await TagWriter.WriteTags(tags, path, 
                        Requests.GetFileNameWithoutExtension(item.Url));

                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ++ind;
                        entry.Bar.Value = (ind * 100.0) / urlsAndTags.Count();

                        entry.FilesMsg = $"{ind}/{urlsAndTags.Count()}";
                    }), DispatcherPriority.Background);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            });

            await Task.WhenAll(tasks);
            entry.StatusMsg = "Finished";

            tasks = Enumerable.Empty<Task>();
        }
    }
}
