using Downloader.Util;
using MonoTorrent.Client;
using MonoTorrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WpfDownloader.WpfData;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;

namespace WpfDownloader.Sites
{
    public class BitTorrent : Site
    {
        private static readonly List<Tuple<string, string>> HEADERS =
            new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9"),
                new Tuple<string, string>("Accept-Language", "en-US,en;q=0.9"),
            };

        private ClientEngine _engine;

        public BitTorrent(string url, string args) : base(url, args)
        {
            _engine = new ClientEngine(new EngineSettings());
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            var newPath = $"{DEFAULT_PATH}/torrents";
            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

            string desc;
            TorrentManager manager;

            if (Url.Contains("magnet"))
            {
                var magnetLink = MagnetLink.Parse(Url);
                manager = await _engine.AddAsync(magnetLink, newPath);

                desc = magnetLink.Name;
            }
            else if (Url.Contains("C:"))
            {
                var torrent = await Torrent.LoadAsync(Url);
                manager = await _engine.AddAsync(torrent, newPath, new TorrentSettings());

                desc = torrent.Name;
            }
            else
            {
                var data = await Requests.GetBytes(Url, HEADERS, entry.CancelToken);
                var torrent = await Torrent.LoadAsync(data);

                manager = await _engine.AddAsync(torrent, newPath, new TorrentSettings());

                desc = torrent.Name;
            }

            if (manager.Files != null) entry.FilesMsg = $"0/{manager.Files.Count}";

            await manager.StartAsync();
            entry.StatusMsg = "Downloading";
            entry.Name = "[BitTorrent] " + desc;

            entry.DownloadPath = (manager.SavePath != null) ? manager.SavePath : newPath;

            while (manager.State != TorrentState.Stopped && manager.State != TorrentState.Paused)
            {
                if (entry.CancelToken.IsCancellationRequested)
                {
                    entry.StatusMsg = "Cancelled";
                    await manager.StopAsync();
                    entry.CancelToken.ThrowIfCancellationRequested();
                    break;
                }

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    entry.Bar.Value = manager.Progress;

                    if (manager.Files != null)
                    {
                        int filesDone = (int)(manager.Files.Count * manager.Progress / 100.0);
                        entry.FilesMsg = $"{filesDone}/{manager.Files.Count}";
                    }
                }));

                if (manager.Progress >= 100.0)
                {
                    await manager.StopAsync();
                    entry.StatusMsg = "Finished";

                    break;
                }

                await Task.Delay(1000);
            }

            //await Task.Run(async () =>
            //{
            //    if (Url.Contains("magnet"))
            //    {
            //        var magnetLink = MagnetLink.Parse(Url);
            //        manager = await _engine.AddAsync(magnetLink, newPath);

            //        desc = magnetLink.Name;
            //    }
            //    else if (Url.Contains("C:"))
            //    {
            //        var torrent = await Torrent.LoadAsync(Url);
            //        manager = await _engine.AddAsync(torrent, newPath, new TorrentSettings());

            //        desc = torrent.Name;
            //    }
            //    else
            //    {
            //        var data = await Requests.GetBytes(Url);
            //        var torrent = await Torrent.LoadAsync(data);

            //        manager = await _engine.AddAsync(torrent, newPath, new TorrentSettings());

            //        desc = torrent.Name;
            //    }

            //    if (manager.Files != null) entry.FilesMsg = $"0/{manager.Files.Count}";

            //    await manager.StartAsync();
            //    entry.StatusMsg = "Downloading";
            //    entry.Name = "[BitTorrent] " + desc;

            //    entry.DownloadPath = (manager.SavePath != null) ? manager.SavePath : newPath;


            //    while (!manager.Complete)
            //    {
            //        await Task.Delay(500);
            //        await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //        {
            //            entry.Bar.Value = manager.Progress;

            //            if (manager.Files != null)
            //            {
            //                int filesDone = (int)(manager.Files.Count * manager.Progress / 100.0);
            //                entry.FilesMsg = $"{filesDone}/{manager.Files.Count}";
            //            }
            //        }));
            //    }


            //    await manager.StopAsync();
            //    entry.StatusMsg = "Finished";
            //});
        }
    }
}
