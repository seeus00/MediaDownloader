using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Config
{
    public static class ConfigManager
    {
        private static readonly string CONFIGURATION_PATH = MainWindow.CONFIG_PATH + "/config.txt";

        public static readonly Dictionary<string, string> PERSONAL_CONFIG =
            new Dictionary<string, string>();

        static ConfigManager()
        {
            if (!File.Exists(CONFIGURATION_PATH)) using (File.Create(CONFIGURATION_PATH)) { }
        }

        public static async Task ReadConfigurationFile()
        {
            var lines = await File.ReadAllLinesAsync(CONFIGURATION_PATH);
            foreach (string line in lines)
            {
                var split = line.Split("=");
                PERSONAL_CONFIG.Add(split[0], split[1]);
            }
        }
    }
}
