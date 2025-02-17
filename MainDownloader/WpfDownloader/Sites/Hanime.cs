using Downloader.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Hanime : Site
    {
        private static readonly string GET_API = "https://hanime.tv/rapi-cache/rapi/v7/video?id={0}";

        private string _seriesTitle;

        private string _slug;
        private JToken _data;

        //ARGS: s=series,none=one
        public Hanime(string url, string args) : base(url, args)
        {
            _slug = url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            await UpdateData(_slug);
            _seriesTitle = RemoveIllegalChars(_data["hentai_franchise"]["slug"].ToString());

            var newPath = $"{DEFAULT_PATH}/hanime/{_seriesTitle}";

            entry.DownloadPath = newPath;

            var info = new JDict();
            info["title"] = new JType(_seriesTitle);
            info["tags"] = new JArray(
                _data["hentai_tags"].Select(tag => tag["text"].ToString()));
            await TagWriter.WriteTags(info, newPath);

            var slugs = new List<string>();
            if (string.IsNullOrEmpty(Args))
            {
                slugs.Add(_slug);
            }else if (Args == "s")
            {
                slugs.AddRange(_data["hentai_franchise_hentai_videos"]
                    .Select(vid => vid["slug"].ToString()));
            }

            entry.FilesMsg = $"0/{slugs.Count()}";

            entry.StatusMsg = "Converting";
            entry.Name = "[Hanime] " + _seriesTitle;

            int ind = 1;
            var semaphoreSlim = new SemaphoreSlim(2);
            var tasks = slugs.Select(async (slug, i) =>
            {
                if (entry.CancelToken.IsCancellationRequested) return;

                await semaphoreSlim.WaitAsync();
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var currEntry = new UrlEntry()
                        {
                            StatusMsg = "Converting",
                            Name = "[Hanime] " + slug,
                        };

                        entry.SubItems.Add(currEntry);
                    });


                    var m3u8Url = await GetM3u8Url(slug);
                    //var m3u8Path = $"{newPath}/{slug}.m3u8";
                    //var mp4Path = $"{newPath}/{slug}.mp4";
                    var coverUrl = _data["hentai_video"]["cover_url"].ToString();

                    string thumbnailUrl = _data["hentai_video"]["poster_url"].ToString();
                    string storyboardUrl = _data["hentai_video_storyboards"].First()["url"].ToString();

                    await Requests.DownloadFileFromUrl(thumbnailUrl, newPath, fileName: $"{slug}-thumbnail");
                    await Requests.DownloadFileFromUrl(storyboardUrl, newPath, fileName: $"{slug}-storyboard");

                    //await Requests.DownloadFileFromUrl(coverUrl, newPath, fileName: slug);
                    //await Requests.DownloadFileFromUrl(m3u8Url, newPath, fileName: slug);


                    await VideoConverter.DownloadYoutubeVideo(m3u8Url, newPath, entry.SubItems[i], showProgress: true, fileName: slug);

                    //await VideoConverter.M3u8ToMp4(m3u8Path, mp4Path, entry.SubItems[i], showProgress: true);

                    int percent = (ind * 100) / slugs.Count;
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        entry.Bar.Value = percent;
                        entry.SubItems[i].StatusMsg = UrlEntry.FINISHED;

                        entry.FilesMsg = $"{ind}/{slugs.Count()}";
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

        private async Task UpdateData(string slug)
        {
            var jsonStr = await Requests.GetStr(string.Format(GET_API, slug));
            _data = JsonParser.Parse(jsonStr);
        }

        private async Task<string> GetM3u8Url(string slug)
        {
            //Get highest quality (1080p -> 720p -> 420p, etc)
            await UpdateData(slug);
            string m3u8Url = _data["videos_manifest"]["servers"].First()["streams"]
                .Where(stream => stream["height"].ToString() != "1080")
                .First()["url"].ToString();

            return m3u8Url;
            //return $"https://weeb.hanime.tv/weeb-api-cache/api/v8/m3u8s/{id}.m3u8";
        }
    }
}
