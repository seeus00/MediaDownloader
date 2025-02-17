using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Util.LinkSaver
{
    public class LinkData
    {
        public string Url { get; set; }

        private string _fullPath;
        public string FullPath
        {
            get
            {
                return _fullPath;
            }

            set
            {
                _fullPath = string.IsNullOrEmpty(value) ? string.Empty : value.Replace('\\', '/');
            }
        }

        public string DirName
        {
            get => FullPath.Split('/').Last().Split('.').First();
            private set { }
        }
        ////Urls that have already been saved
        //public List<string> SavedUrls { get; set; }
        ////File paths that have already been downloaded
        //public List<string> DownloadedFilesPaths { get; set; }
    }
}
