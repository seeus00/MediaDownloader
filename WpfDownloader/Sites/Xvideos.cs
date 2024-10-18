using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    internal class Xvideos : Site
    {
        public Xvideos(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            string newPath = $"{DEFAULT_PATH}/xvideos";
            entry.StatusMsg = "Downloading";
            entry.DownloadPath = newPath;

            await VideoConverter.DownloadYoutubeVideo(Url, newPath, entry, args: Args);
            entry.StatusMsg = (entry.CancelToken.IsCancellationRequested) ? "Cancelled" : "Finished";

        }
    }
}
