using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfDownloader.WpfData;

public static class VideoConverter
{
    private static readonly string USER_AGENT =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.63 Safari/537.36 Edg/102.0.1245.39";


    public static async Task DownloadYoutubeVideo(string videoUrl, string videoPath, string args, UrlEntry entry)
    {
        string arguments = string.Empty;
        switch (args)
        {
            case "mp3":
                arguments = $"--extract-audio --audio-format mp3 {videoUrl}";
                break;
            case "webm":
                arguments = videoUrl;
                break;
            //Default is mp4
            default:
                arguments = $"-f bestvideo[ext=mp4]+bestaudio[ext=m4a]/mp4 {videoUrl}";
                break;
        }

        arguments += $" --no-continue -o {videoPath}/%(title)s.%(ext)s";
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

        string standard_output;
        while ((standard_output = await proc.StandardOutput.ReadLineAsync()) != null)
        {
            if (entry.CancelToken.IsCancellationRequested)
            {
                entry.StatusMsg = "Cancelled";
                break;
            }

            if (standard_output.Contains("Destination"))
            {
                string title = new Regex("Destination:(.*?)\\.", RegexOptions.Singleline)
                    .Match(standard_output).Groups[1].Value.Trim();

                var dirName = new DirectoryInfo(title).Name;
                entry.Name = dirName;
                break;
            }
        }

        while ((standard_output = await proc.StandardOutput.ReadLineAsync()) != null)
        {
            if (entry.CancelToken.IsCancellationRequested)
            {
                entry.StatusMsg = "Cancelled";
                break;
            }

            if (standard_output.Contains("MiB"))
            {
                string percentStr = new Regex("\\[download\\] (.*?)%", RegexOptions.Singleline)
                    .Match(standard_output).Groups[1].Value;
                percentStr = percentStr.Trim();

                double percent = double.Parse(percentStr);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    entry.Bar.Value = percent;
                }), DispatcherPriority.ContextIdle);

                if (percent >= 100.0) break;
            }
        }

        await proc.WaitForExitAsync(entry.CancelToken);
        proc.Close();

        registerResult.Dispose();
    }

    // Requires ffmpeg installed in path
    public static async Task M3u8ToMp4(string m3u8Path, string mp4Path, UrlEntry entry, bool showProgress = false)
    {
        string protocols = "file,http,https,tcp,tls,crypto";

        var proc = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                Arguments = $"-stats -y -protocol_whitelist {protocols} -i \"{m3u8Path}\" -c copy \"{mp4Path}\" -headers \"{USER_AGENT}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        var registerResult = entry.CancelToken.Register(() => proc.Kill());

        proc.Start();
        if (showProgress)
        {
            string standard_output;
            int max = 0;
            while ((standard_output = await proc.StandardError.ReadLineAsync()) != null)
            {
                if (standard_output.Contains("Duration"))
                {
                    string durString = new Regex(@"Duration: (.*?),").Match(standard_output).Groups[1].Value;

                    max = FfmpegParser.GetTotalSeconds(durString);
                    break;
                }
            }

            while ((standard_output = await proc.StandardError.ReadLineAsync()) != null)
            {
                if (entry.CancelToken.IsCancellationRequested)
                {
                    entry.StatusMsg = "Finished";
                    proc.Kill();
                    proc.Dispose();
                    break;
                }

                if (standard_output.Contains("time"))
                {
                    string currTime = new Regex(@"time=(.*?) bitrate").Match(standard_output).Groups[1].Value;
                    int secTime = FfmpegParser.GetTotalSeconds(currTime);

                    int percent = (secTime * 100) / max;
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        entry.Bar.Value = secTime;
                    }), DispatcherPriority.ContextIdle);
                }
            }
        }else
        {
            string tmpErrorOut = await proc.StandardError.ReadToEndAsync();
            string output = await proc.StandardOutput.ReadToEndAsync();
        }

        await proc.WaitForExitAsync(entry.CancelToken);
        File.Delete(m3u8Path);
        proc.Close();

        registerResult.Dispose();
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
                Arguments = $"/C yt-dlp --hls-prefer-native \"{m3u8Url}\" -o \"{mp4Path}\" {headersStrBuilder.ToString()}",
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

    public static void M3u8UrlToMp4Ffmpeg(string m3u8Url, string mp4Path)
    {
        var proc = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = $"/C ffmpeg -i \"{m3u8Url}\" -acodec copy -vcodec copy \"{mp4Path}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        proc.Start();

        proc.WaitForExit();
        proc.Close();
    }
}

