using WpfDownloader.Sites;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections;
using System.Threading;
using System.Security.Policy;
using System.Windows.Threading;
using Google.Protobuf.WellKnownTypes;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace WpfDownloader.WpfData
{
    public class UrlEntry : INotifyPropertyChanged
    {
        public static Dictionary<string, int> STATUS_IDS =
            new Dictionary<string, int>()
            {
                { "Queued", 3 },
                { "Retrieving", 2 },
                { "Downloading", 1 },
                { "Finished", 0 },
            };

        public static readonly string RETRIEIVING = "Retrieving";
        public static readonly string QUEUED = "Queued";
        public static readonly string DOWNLOADING = "Downloading";
        public static readonly string FINISHED = "Finished";

        public CancellationTokenSource CancelTokenSource { get; set; } = new CancellationTokenSource();
        public CancellationToken CancelToken
        {
            get
            {
                return CancelTokenSource.Token;
            }
        } 

        public string ImgIconPath { get; set; }
        public string Number { get; set; }


        private ObservableCollection<UrlEntry> subItems = new ObservableCollection<UrlEntry>();
        public ObservableCollection<UrlEntry> SubItems
        {
            get
            {
                return subItems;
            }

            set 
            {
                subItems = value;
                NotifyPropertyChanged("SubItems");
            }

        }


        private string _url;
        public string Url { 
            get 
            {
                return _url;
            } 
            set
            {
                _url = value;
                NotifyPropertyChanged("Url");
            } 
        }

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private string _statusMsg;
        public string StatusMsg
        {
            get
            {
                return _statusMsg;
            }
            set
            {
                _statusMsg = value;
                NotifyPropertyChanged("StatusMsg");
            }
        }

        private string _filesMsg;
        public string FilesMsg
        {
            get
            {
                return _filesMsg;
            }
            set
            {
                _filesMsg = value;
                NotifyPropertyChanged("FilesMsg");
            }
        }


        public ProgressBar Bar { get; set; }

        public string DownloadPath { get; set; }

        //public Site SiteObj { get; set; }

        public Button CancelButton { get; set; }

        public UrlEntry()
        {
            Bar = new ProgressBar();
            //CancelButton = new Button();
        }


        public UrlEntry(string imgIconPath, string name, string statusMsg, 
            ProgressBar bar)
        {
            ImgIconPath = imgIconPath;
            Name = name;
            StatusMsg = statusMsg;
            Bar = bar;
        }


        //Opens file explorer to the path
        public void OpenPath()
        {
            if (string.IsNullOrEmpty(DownloadPath)) return;

            string path = DownloadPath.Replace('/', '\\');
            using Process fileopener = new Process();

            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }



        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }));

        }

        public sealed class NameComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                UrlEntry a = (UrlEntry)x;
                UrlEntry b = (UrlEntry)y;

                return a.Name.CompareTo(b.Name);
            }
        }

        public sealed class StatusComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                UrlEntry a = (UrlEntry)x;
                UrlEntry b = (UrlEntry)y;

                if (STATUS_IDS.ContainsKey(a.StatusMsg) && STATUS_IDS.ContainsKey(b.StatusMsg))
                {
                    if (STATUS_IDS[a.StatusMsg] > STATUS_IDS[b.StatusMsg])
                    {
                        return 1;
                    }
                    else if (STATUS_IDS[a.StatusMsg] < STATUS_IDS[b.StatusMsg])
                    {
                        return -1;
                    }
                }

                return 0;
            }
        }

        //public int CompareTo(UrlEntry other)
        //{
        //    if (other == null) return 1;

        //    return Name.CompareTo(other.Name);
        //}

        //public int CompareTo(object obj)
        //{
        //    if (obj == null) return 1;

        //    UrlEntry otherEntry = obj as UrlEntry;
        //    if (otherEntry != null)
        //    {
        //        return Name.CompareTo(otherEntry.Name);
        //    }else
        //    {
        //        throw new ArgumentException("obj is not of type UrlEntry");
        //    }
        //}
    }
}
