using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static async Task<string> GetUA()
        {
            string jsonStr = await Requests.GetStr(LATEST_UA_API);
            var data = JsonParser.Parse(jsonStr);

            return data[5].Value.Trim(); 
        }
    }
}
