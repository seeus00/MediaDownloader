using Downloader.Util;
using MonoTorrent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    internal class SpotifyData
    {
        public string SongName { get; set; }
        public string ReleaseDate { get; set; }
        public List<string> Artists { get; set; }
        public string YoutubeSearchString => $"{string.Join(", ", Artists)} - {SongName}";
    }

    internal class Spotify : Site
    {
        private const int MAX_WORKER_THREADS = 2;

        private const string CLIENT_ID = "dfd4938497af49b98ddded937096a113";
        private const string CLIENT_SECRET = "6beb7d052cae44748a1622340e5d0467";

        private const string ACCESS_TOKEN_API = "https://accounts.spotify.com/api/token";

        private string accessToken;
        private string playlistName;

        public Spotify(string url, string args) : base(url, args)
        {
        }

        private async Task<string> GetAccessToken()
        {
            var payload = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            };

            string authStr = $"{CLIENT_ID}:{CLIENT_SECRET}";
            string b64AuthStr = Convert.ToBase64String(Encoding.UTF8.GetBytes(authStr));

            var headers = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Authorization", $"Basic {b64AuthStr}"),
                //new Tuple<string, string>("Content-Type", $"application/x-www-form-urlencoded"),
            };

            var resp = await Requests.Post(ACCESS_TOKEN_API, payload: payload, headers: headers);
            resp.EnsureSuccessStatusCode();

            string jsonStr = await resp.Content.ReadAsStringAsync();
            var data = JsonParser.Parse(jsonStr);

            return data["access_token"].ToString();
        }

        private async Task<IEnumerable<SpotifyData>> GetPlaylists(string id)
        {
            var headers = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Authorization", $"Bearer {accessToken}")
            };

            var resp = await Requests.Get($"https://api.spotify.com/v1/playlists/{id}", headers: headers);
            resp.EnsureSuccessStatusCode();

            string jsonStr = await resp.Content.ReadAsStringAsync();
            var data = JsonParser.Parse(jsonStr);

            playlistName = data["name"].ToString();

            return data["tracks"]["items"].Select(item => new SpotifyData()
            {
                SongName = item["track"]["name"].ToString(),
                ReleaseDate = item["track"]["album"]["release_date"].ToString(),
                Artists = item["track"]["album"]["artists"].Select(artist => artist["name"].ToString()).ToList()
            });
        }

        private async Task<string> SearchForYoutubeVideo(string searchTerms)
        {
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = "yt-dlp";
            process.StartInfo.Arguments = $" ytsearch:\"{searchTerms}\" --get-id";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string youtubeId = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return $"https://www.youtube.com/watch?v={youtubeId.Trim()}";
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";

            accessToken = await GetAccessToken();

            string playlistId = Url.Split('/').Last();
            var playlists = await GetPlaylists(playlistId);

            entry.Name = $"[Spotify] {playlistName}";

            string newPath = $"{DEFAULT_PATH}/spotify/{RemoveIllegalChars(playlistName)}";
            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
            
            entry.StatusMsg = "Downloading";
            entry.SubItems = new ObservableCollection<UrlEntry>(playlists.Select(data => new UrlEntry()
            {
                Name = $"[Spotify] {data.YoutubeSearchString}",
                StatusMsg = UrlEntry.DOWNLOADING
            }));

            var ss = new SemaphoreSlim(MAX_WORKER_THREADS);
            int ind = 1;

            var tasks = playlists.Select(async (playlistData, i) =>
            {
                await ss.WaitAsync();
                try
                {
                    string ytVidUrl = await SearchForYoutubeVideo(playlistData.YoutubeSearchString);
                    await VideoConverter.DownloadSingleUrl(ytVidUrl, newPath, entry.SubItems[i], "mp3", true);

                    double percent = (double)ind / playlists.Count() * 100.0;
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        entry.Bar.Value = percent;
                        entry.FilesMsg = $"{ind}/{playlists.Count()}";
                    }));
                    ind++;
                }
                catch(Exception e)
                {
                    entry.SubItems[i].StatusMsg = "ERROR";
                }finally
                {
                    ss.Release();
                }
            });

            await Task.WhenAll(tasks);
            entry.StatusMsg = "Finished";
        }
    }
}
