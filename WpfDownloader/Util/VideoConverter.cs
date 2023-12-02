using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfDownloader.WpfData;
using static Google.Protobuf.WellKnownTypes.Field.Types;

public static class VideoConverter
{
    private const int MAX_WORKER_THREADS = 2;

    //Wait some time before making a new request (helps avoid "too many requests" related errors)
    private const int DELAY_TIME_REQUESTS = 1000;

    public static async Task DownloadSingleUrl(string videoUrl, string videoPath, UrlEntry entry, string args = null,
        bool showProgress = true, string fileName = null, List<Tuple<string, string>> headers = null)
    {
        string arguments = string.Empty;
        switch (args)
        {
            case "mp3":
                arguments = $"--extract-audio --audio-format mp3";
                break;
            case "webm":
                arguments = videoUrl;
                break;
            //Default is mp4
            default:
                arguments = $"-f bestvideo[ext=mp4]+bestaudio[ext=m4a]/mp4";
                break;
        }

        if (headers != null)
        {
            foreach (var header in headers) arguments += $" --add-header {header.Item1}:\"{header.Item2}\"";
        }

        arguments += $" \"{videoUrl}\"";
        if (!string.IsNullOrEmpty(fileName))
        {
            arguments += $" --no-continue -o \"{videoPath}/{fileName}.%(ext)s\"";
        }
        else
        {
            arguments += $" --no-continue -o \"{videoPath}/%(title)s.%(ext)s\"";
        }


        var proc = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "yt-dlp",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        var registerResult = entry.CancelToken.Register(() => proc.Kill());
        proc.Start();

        string standard_output = null;
        while ((standard_output = await proc.StandardOutput.ReadLineAsync()) != null)
        {
            //Debug.WriteLine(standard_output);
            if (entry.CancelToken.IsCancellationRequested)
            {
                entry.StatusMsg = "Cancelled";
                proc.Kill();
                break;
            }

            if (showProgress)
            {
                if (standard_output.Contains("Destination"))
                {
                    string title = new Regex("Destination:(.*?)$", RegexOptions.Singleline)
                        .Match(standard_output).Groups[1].Value.Trim();

                    var dirName = new DirectoryInfo(title).Name;
                    entry.Name = dirName;
                }

                if (standard_output.Contains("[download]"))
                {
                    var match = new Regex("(\\d+\\.?\\d*)%").Match(standard_output);

                    if (!match.Success) continue;


                    string percentStr = match.Groups[1].Value.Trim();

                    if (!double.TryParse(percentStr, out double percent)) continue;
                    if (percent <= 100)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            entry.Bar.Value = percent;
                        });
                    }
                }
            }
        }

        await proc.WaitForExitAsync(entry.CancelToken);
        proc.Kill();

        registerResult.Dispose();
        entry.StatusMsg = UrlEntry.FINISHED;
    }

    public static async Task DownloadYoutubeVideo(string videoUrl, string videoPath, UrlEntry entry, string args = null,
        bool showProgress = true, string fileName = null, List<Tuple<string, string>> headers = null)
    {
        //Is youtube playlist
        if (new Regex("youtube\\.com\\/playlist\\?list=[a-zA-Z0-9_.-]*$").Match(videoUrl).Success)
        {   
            //Write playlist urls to a file
            var playlistVideoPath = $"{videoPath}/urls.txt";
            if (!Directory.Exists(videoPath)) Directory.CreateDirectory(videoPath);

            if (File.Exists(playlistVideoPath)) File.Delete(playlistVideoPath);

            var proc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "yt-dlp",
                    Arguments = $"--flat-playlist -i --print-to-file url \"{playlistVideoPath}\" --cookies-from-browser firefox {videoUrl}",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            var registerResult = entry.CancelToken.Register(() => proc.Kill());
            proc.Start();

            string standard_output = null;
            string playlistTitle = string.Empty;
            while ((standard_output = await proc.StandardOutput.ReadLineAsync()) != null)
            {
                //Debug.WriteLine(standard_output);
                if (entry.CancelToken.IsCancellationRequested)
                {
                    entry.StatusMsg = "Cancelled";
                    proc.Kill();
                    break;
                }

                if (standard_output.Contains("Downloading playlist"))
                {
                    playlistTitle = new Regex("Downloading playlist: (.*?)$").Match(standard_output).Groups[1].Value.Trim();
                }
            }

            await proc.WaitForExitAsync(entry.CancelToken);
            proc.Kill();
            registerResult.Dispose();

            var videoUrls = await File.ReadAllLinesAsync(playlistVideoPath);

            //Create separate inner progress bars
            entry.SubItems = new ObservableCollection<UrlEntry>(videoUrls.Select(url => new UrlEntry()
            {
                Name = url,
                StatusMsg = UrlEntry.DOWNLOADING
            }));

            videoPath = $"{videoPath}/{WpfDownloader.Sites.Site.RemoveIllegalChars(playlistTitle)}";
            if (!Directory.Exists(videoPath)) Directory.CreateDirectory(videoPath);

            entry.Name = playlistTitle;

            var ss = new SemaphoreSlim(MAX_WORKER_THREADS);
            int ind = 1;
            var tasks = videoUrls.Select(async (url, i) =>
            {
                await ss.WaitAsync();
                try
                {
                    await DownloadSingleUrl(url, videoPath, entry.SubItems[i], args, showProgress, fileName, headers);

                    double percent = (double) ind / videoUrls.Length * 100.0;
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        entry.Bar.Value = percent;
                        entry.FilesMsg = $"{ind}/{videoUrls.Length}";
                    }));
                    ind++;
                }
                catch (Exception e)
                {
                    entry.SubItems[i].StatusMsg = "ERROR";
                }
                finally
                {
                    ss.Release();
                }
            });

            await Task.WhenAll(tasks);
            entry.StatusMsg = UrlEntry.FINISHED;
        }
        else
        {
            await DownloadSingleUrl(videoUrl, videoPath, entry, args, showProgress, fileName, headers);
        }

    }
   

    public static void M3u8UrlToMp4(string m3u8Url, string mp4Path, UrlEntry entry, List<Tuple<string, string>> headers = null)
    {
        var headersStrBuilder = new StringBuilder();
        if (headers != null)
        {
            foreach (var header in headers)
            {
                headersStrBuilder.Append($"--add-header {header.Item1}:\"{header.Item2}\" ");
            }
        }

        var proc = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = $"/C yt-dlp --hls-prefer-native \"{m3u8Url}\" -o \"{mp4Path}\" {headersStrBuilder}",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        proc.Start();
        string standard_output = string.Empty;
        while ((standard_output = proc.StandardOutput.ReadLine()) != null)
        {
            if (standard_output.Contains("ETA"))
            {
                string frags = new Regex("frag (.*?)\\)").Match(standard_output)
                    .Groups[1].Value;
                var split = frags.Split('/');

                if (split.Length != 2) continue;

                double fragsCompleted = int.Parse(split[0]);
                double totalFrags = int.Parse(split[1]);

                double percent = fragsCompleted / totalFrags * 100.0;
                //Debug.WriteLine(percent);
                //Application.Current.Dispatcher.Invoke(new Action(() =>
                //{
                //    entry.Bar.Value = percent;
                //}), DispatcherPriority.ContextIdle);
            }
        }

        proc.WaitForExit();
        proc.Close();
    }
}

