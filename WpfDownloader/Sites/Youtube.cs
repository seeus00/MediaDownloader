using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Youtube : Site
    {
        public Youtube(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            string newPath = $"{DEFAULT_PATH}/youtube";
            entry.StatusMsg = "Downloading";
            entry.DownloadPath = newPath;

            await VideoConverter.DownloadYoutubeVideo(Url, newPath, entry, args: Args);
            entry.StatusMsg = (entry.CancelToken.IsCancellationRequested) ? "Cancelled" : "Finished";

        }
    }
}
