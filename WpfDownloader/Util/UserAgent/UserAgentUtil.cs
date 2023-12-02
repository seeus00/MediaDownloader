using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WpfDownloader.Config;

namespace WpfDownloader.Util.UserAgent
{
    public static class UserAgentUtil
    {
        public static string CURR_USER_AGENT = string.Empty;
        private static string LATEST_UA_API = "https://jnrbsn.github.io/user-agents/user-agents.json";

        public static async Task InitUserAgent()
        {
            CURR_USER_AGENT = ConfigManager.PERSONAL_CONFIG.ContainsKey("user_agent") ? ConfigManager.PERSONAL_CONFIG["user_agent"] : await GetUA();
        }

        //Works only for firefox, make it so you find other browser locations
        public static async Task<string> GetUA()
        {
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = "C:\\Program Files\\Mozilla Firefox\\firefox.exe";
            process.StartInfo.Arguments = " -v | more";
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            string version = output.Trim().Split().Last();
            return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:{version}) Gecko/20100101 Firefox/{version}";

                

            //string jsonStr = await Requests.GetStr(LATEST_UA_API);
            //var data = JsonParser.Parse(jsonStr);

            //return data[3].ToString().Trim();
        }
    }
}
