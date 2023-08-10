using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.Config;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public abstract class Site
    {
        public static string DEFAULT_PATH = ConfigManager.PERSONAL_CONFIG.ContainsKey("default_path") ?
             ConfigManager.PERSONAL_CONFIG["default_path"] : 
             Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        private string _url;

        public string Url { get => _url; set => 
                _url = value.EndsWith("/") ? value.Substring(0, value.Length - 1) : value;
        }

        public string Args { get; set; }

        public Site(string url, string args)
        {
            Url = url;
            Args = args;
        }


        public static string RemoveIllegalChars(string str) =>
            string.Join("_", str.Split(Path.GetInvalidFileNameChars())).TrimEnd('.');

        public static string ResizeString(string str) =>
            (str.Length >= 255) ? str.Substring(0, 254) : str;

        public abstract Task DownloadAll(UrlEntry entry);
    }
}
