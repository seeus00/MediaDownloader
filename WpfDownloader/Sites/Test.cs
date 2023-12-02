using ChromeCookie;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.Util;
using WpfDownloader.Util.UserAgent;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Test : Site
    {
        public Test(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.FilesMsg = "12313/23123127381293";
            entry.DownloadPath = "C:/Users/casey/Downloads";
            //var test = new List<string>();
            //for (int i = 0; i < 2000000; i++)
            //{
            //    test.Add("NIGGER");
            //}

            var subitems = new ObservableCollection<UrlEntry>();
            for (int i = 0; i < 200; i++) subitems.Add(new UrlEntry()
            {
                Name = "SUB ITEM: " + i,
                StatusMsg = "123h12891h89318321312319382h31893h182391238912h8931283129h38912h3912h93812h93",
                FilesMsg = "192831931/1273891239812793MB",
                DownloadPath = "C:/Users/casey/Videos/1697921020492573.webm"
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                entry.SubItems = subitems;
            });

            //for (int i = 0; i < 2000; i++)
            //{
            //    await Application.Current.Dispatcher.BeginInvoke(() =>
            //    {
            //        entry.SubItems.Add(new UrlEntry()
            //        {
            //            Name = "SUB ITEM",
            //            StatusMsg = "123h12891h89318321312319382h31893h182391238912h8931283129h38912h3912h93812h93",
            //            FilesMsg = "192831931/1273891239812793MB"
            //        });
            //    }, DispatcherPriority.Background);

            //    //}


            //
        }
    }
}
