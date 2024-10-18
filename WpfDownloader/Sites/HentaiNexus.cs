using System;
using System.Linq;
using System.Threading.Tasks;
using WpfDownloader.WpfData;
using Downloader.Util;
using System.Net;
using ChromeCookie;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Web;
using MonoTorrent;
using System.IO;

namespace WpfDownloader.Sites
{
    internal class HentaiNexus : Site
    {
        private static CookieContainer _cookieContainer = null;


        public HentaiNexus(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Getting JS script..";

            if (_cookieContainer == null)
            {
                var baseAddress = new Uri("https://hentainexus.com");
                _cookieContainer = new CookieContainer();
                _cookieContainer.Add(baseAddress, ChromeCookies.GetCookies("hentainexus.com"));

                Requests.AddCookies(_cookieContainer, baseAddress);
            }

            string code = Url.Split("/").Last();
            string html = await Requests.GetStr($"https://hentainexus.com/read/{code}#001");

            var match = new Regex("initReader\\(\\\"(.*?)\\\", \\\"(.*?)\\\", \\{(.*?)\\}", RegexOptions.Singleline).Match(html);

            string b64Str = match.Groups[1].Value;
            string title = match.Groups[2].Value;
            string dictStr = match.Groups[3].Value;
            string args = $"'{b64Str}','{title}',{{{dictStr}}}";

            string newPath = $"{DEFAULT_PATH}/hentai_nexus/{RemoveIllegalChars(title)} - {code}";
            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);


            string initReaderFuncCall = $"global.callInitReader = function callInitReader() {{ initReader({args}); }}";

            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

            string jsPath = $"{projectDirectory}/JSFiles/HentaiNexus/NodeInitReader.js";
            string newJsPath = $"{newPath}/temp.js";

            if (File.Exists(newJsPath)) File.Delete(newJsPath);
            File.Copy(jsPath, newJsPath);
            await File.AppendAllTextAsync(newJsPath, '\n' + initReaderFuncCall);

            var proc = new Process();
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = $"{Environment.ExpandEnvironmentVariables("%ProgramW6432%")}/nodejs/node.exe";
            startInfo.Arguments = $"-r \"{newJsPath}\" -e \"callInitReader()\"";
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            proc.StartInfo = startInfo;
            proc.Start();

          
            string res = HttpUtility.HtmlDecode(await proc.StandardOutput.ReadToEndAsync());
            await proc.WaitForExitAsync();

            var data = JsonParser.Parse(res);
            var urls = data.Select(imgInfo => imgInfo["image"].ToString().Replace("\\", ""));

            entry.StatusMsg = "Downloading";

           
            await DownloadUtil.DownloadAllUrls(urls, newPath, entry);
            File.Delete(newJsPath);
        }
    }
}
