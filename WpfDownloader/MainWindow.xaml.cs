using Downloader.Data;
using Downloader.Loggers;
using WpfDownloader.Sites;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WpfDownloader.WpfData;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Collections.Specialized;
using static WpfDownloader.WpfData.UrlEntry;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Drawing;
using WpfDownloader.Util;
using Org.BouncyCastle.Asn1.Cms;
using System.Reflection.Metadata;
using System.Security.Authentication;
using System.Net.Security;
using WpfDownloader.Config;
using WpfDownloader.Util.UserAgent;

namespace WpfDownloader
{
    public partial class MainWindow : MetroWindow
    {

        // The enum flag for DwmSetWindowAttribute's second parameter, which tells the function what attribute to set.
        // Copied from dwmapi.h
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        // The DWM_WINDOW_CORNER_PREFERENCE enum for DwmSetWindowAttribute's third parameter, which tells the function
        // what value of the enum to set.
        // Copied from dwmapi.h
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        // Import dwmapi.dll and define DwmSetWindowAttribute in C# corresponding to the native function.
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void DwmSetWindowAttribute(IntPtr hwnd,
                                                         DWMWINDOWATTRIBUTE attribute,
                                                         ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                         uint cbAttribute);


        public static readonly string CONFIG_PATH =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Downloader";


        private static readonly string URLS_FILE_PATH = CONFIG_PATH + "/list.txt";
        private static readonly string ERROR_FILE_PATH = CONFIG_PATH + "/errors.log";
        private static readonly string SUMMARY_FILE_PATH = CONFIG_PATH + "/summary.log";
        

        private static readonly SiteInfo[] SITES =
        {
            new SiteInfo() { ClassName="FourChan", Domains="boards\\.4channel\\.org\\/.*?\\/thread\\/[0-9]*,boards\\.4chan\\.org\\/.*?\\/thread\\/[0-9]*" },
            new SiteInfo() { ClassName="Danbooru", Domains="danbooru\\.donmai\\.us" },
            new SiteInfo() { ClassName="SafeBooru", Domains="safebooru\\.org" },
            new SiteInfo() { ClassName="Gelbooru", Domains="gelbooru\\.com" },
            new SiteInfo() { ClassName="Nhentai",  Domains="nhentai\\.net\\/g\\/[0-9]+,nhentai\\.net\\/artist,nhentai\\.net\\/group"},
            new SiteInfo() { ClassName="Pixiv",  Domains="pixiv\\.net\\/en\\/users"},
            new SiteInfo() { ClassName="Hanime",  Domains="hanime\\.tv"},
            new SiteInfo() { ClassName="Hitomi",  Domains="hitomi\\.la"},
            new SiteInfo() { ClassName="Imgur",  Domains="imgur\\.com,m\\.imgur\\.com"},
            new SiteInfo() { ClassName="Vsco",  Domains="vsco\\.co"},
            new SiteInfo() { ClassName="Catmanga",  Domains="catmanga\\.org\\/series"},
            new SiteInfo() { ClassName="DevianArt",  Domains="www\\.deviantart\\.com"},
            new SiteInfo() { ClassName="ManaToki",  Domains="manatoki,newtoki"},
            new SiteInfo() { ClassName="BitTorrent",  Domains="\\.torrent,magnet:\\?xt"},
            new SiteInfo() { ClassName="Hentai2Read",  Domains="hentai2read\\.com"},
            new SiteInfo() { ClassName="PandaArchive",  Domains="panda\\.chaika\\.moe"},
            new SiteInfo() { ClassName="Yandex",  Domains="disk\\.yandex\\.com\\/d\\/"},
            new SiteInfo() { ClassName="ArtStation",  Domains="artstation\\.com"},
            new SiteInfo() { ClassName="Twitter",  Domains="twitter\\.com"},
            new SiteInfo() { ClassName="EightMuses",  Domains="comics\\.8muses\\.com"},
            new SiteInfo() { ClassName="KemonoParty",  Domains="kemono\\.party,coomer\\.party"},
            new SiteInfo() { ClassName="MissAv",  Domains="missav\\.com"},
            new SiteInfo() { ClassName="WebToonXYZ",  Domains="webtoon\\.xyz"},
            new SiteInfo() { ClassName="TwoChen",  Domains="2chen\\.moe"},
            new SiteInfo() { ClassName="FakkuCC",  Domains="fakku\\.cc"},
            new SiteInfo() { ClassName="HentaiMama",  Domains="hentaimama\\.io\\/tvshows"},
            new SiteInfo() { ClassName="Hiperdex",  Domains="hiperdex\\.com\\/manga"},
            new SiteInfo() { ClassName="Rule34",  Domains="rule34\\.xxx\\/index\\.php\\?page=post&s=list&tags="},
            new SiteInfo() { ClassName="Youtube",  Domains="youtube\\.com,reddit.com\\/r\\/.*?\\/comments\\/.*?\\/.*?"},
            new SiteInfo() { ClassName="JavGuru",  Domains="jav\\.guru"},
            new SiteInfo() { ClassName="KskMoe",  Domains="ksk\\.moe"},
            new SiteInfo() { ClassName="Avjoa",  Domains="avjoa[0-9]+"},
            new SiteInfo() { ClassName="Bato",  Domains="bato\\.to\\/series\\/[0-9]+"},
            new SiteInfo() { ClassName="Test",  Domains="test"},
        };

        private static readonly SemaphoreSlim _slim = new SemaphoreSlim(2);

        private static MainWindow openWindow = null;


        private ObservableCollection<UrlEntry> _urlEntries;
        private ConcurrentDictionary<Site, UrlEntry> _startingEntries;
        private ConcurrentDictionary<Site, UrlEntry> _inProgressEntries;

        private TextBox _urlTextBox;
        private ListView _urlListView;

        private TextBlock _downloadingTextBlock;
        private TextBlock _finishedTextBlock;
        private TextBlock _errorTextBlock;

        private static int _downloading = 0;
        private static int _finished = 0;
        private static int _errors = 0;

        private static int entryInd = 0;

        private GridViewColumnHeader _lastHeaderClickedName = null;
        private ListSortDirection _lastDirectionName = ListSortDirection.Ascending;

        private System.Windows.Forms.NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls13;


            _urlEntries = new ObservableCollection<UrlEntry>();
            _startingEntries = new ConcurrentDictionary<Site, UrlEntry>();
            _inProgressEntries = new ConcurrentDictionary<Site, UrlEntry>();

            Loaded += MainWindowLoaded;
            _urlTextBox = FindName("UrlTextBox") as TextBox;

            _urlListView = FindName("UrlsListView") as ListView;
            _urlListView.MaxHeight = Height - 100;
            _urlListView.ItemsSource = _urlEntries;

            _downloadingTextBlock = FindName("DownloadingTextBlock") as TextBlock;
            _finishedTextBlock = FindName("FinishedTextBlock") as TextBlock;
            _errorTextBlock = FindName("ErrorTextBlock") as TextBlock;

            KeyDown += OnKeyDownHandler;
            ((INotifyCollectionChanged)_urlListView.ItemsSource).CollectionChanged +=
             (s, e) =>
             {
                 if (e.Action == NotifyCollectionChangedAction.Add)
                 {
                     _urlListView.ScrollIntoView(_urlListView.Items[_urlListView.Items.Count - 1]);
                 }
             };

            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(hWnd, attribute, ref preference, sizeof(uint));


            //_notifyIcon = new System.Windows.Forms.NotifyIcon();
            //_notifyIcon.Icon = new System.Drawing.Icon("../../../res/icon.ico");
            //_notifyIcon.Visible = true;
            //_notifyIcon.MouseDown += delegate (object sender, System.Windows.Forms.MouseEventArgs 
            //    args)
            //{
            //    this.Show();
            //    this.WindowState = WindowState.Normal;
            //};


            //Set rounded corner preference
            //IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            //var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            //var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            //DwmSetWindowAttribute(hWnd, attribute, ref preference, sizeof(uint));

            //if (DwmSetWindowAttribute(hWnd, 19, new[] { 1 }, 4) != 0)
            //    DwmSetWindowAttribute(hWnd, 20, new[] { 1 }, 4);


            //Make context menu corners rounded
            //var item = (FindName("ContextMenuItem") as MenuItem);
            //hWnd = new WindowInteropHelper(GetWindow(item)).EnsureHandle();

            //preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            //DwmSetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
            //    ref preference, sizeof(uint));

            //if (DwmSetWindowAttribute(hWnd, 19, new[] { 1 }, 4) != 0)
            //    DwmSetWindowAttribute(hWnd, 20, new[] { 1 }, 4);

        }

        private async void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (openWindow == null)
            {
                if (!Directory.Exists(CONFIG_PATH)) Directory.CreateDirectory(CONFIG_PATH);

                if (!File.Exists(URLS_FILE_PATH)) using (File.Create(URLS_FILE_PATH)) { }
                if (!File.Exists(ERROR_FILE_PATH)) using (File.Create(ERROR_FILE_PATH)) { }
                if (!File.Exists(SUMMARY_FILE_PATH)) using (File.Create(SUMMARY_FILE_PATH)) { }

                await ConfigManager.ReadConfigurationFile();
                await UserAgentUtil.InitUserAgent();     

                _urlListView.Background = SystemParameters.WindowGlassBrush;
                //_urlListView.Background = Background;


                openWindow = this;
            }
        }

        private void UpdateDownloading()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _downloadingTextBlock.Text = $"Downloading: {_downloading}";
            }));
        }

        private void UpdateFinished()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _finishedTextBlock.Text = $"Finished: {_finished}";
            }));
        }

        private void UpdateErrors()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _errorTextBlock.Text = $"Errors: {_errors}";
            }));
        }


        public bool UrlValid(string testUrl) => 
            SITES.Where(site => site.isValidSite(testUrl)).Any();

        public async Task StartDownload()
        {
            Debug.WriteLine(entryInd);
            int currLen = _startingEntries.Count;
            var tasks = new List<Task>();

            foreach (var entryPair in _startingEntries)
            {
                if (_inProgressEntries.Contains(entryPair)) continue;

                Site siteObj = entryPair.Key;
                UrlEntry entry = entryPair.Value;

                tasks.Add(Task.Run(async () =>
                {
                    _inProgressEntries.TryAdd(entryPair.Key, entryPair.Value);

                    await _slim.WaitAsync();

                    try
                    {
                        _downloading++;
                        UpdateDownloading();

                        await siteObj.DownloadAll(entry);

                        _finished++;
                        _downloading--;
                        UpdateFinished();
                        UpdateDownloading();
                    }
                    catch (Exception e)
                    {
                        if (entry.CancelToken.IsCancellationRequested)
                        {
                            _downloading--;
                            entry.StatusMsg = "Cancelled";

                            UpdateDownloading();
                        }
                        else
                        {
                            await Logger.WriteToLog(ERROR_FILE_PATH, $"[{siteObj.GetType().Name}] - {DateTime.Now}");
                            await Logger.WriteToLog(ERROR_FILE_PATH, e.ToString());
                            await Logger.WriteToLog(ERROR_FILE_PATH, "\n\n");

                            await Logger.WriteToLog(SUMMARY_FILE_PATH, $"{siteObj.Url} [FAILED]");

                            entry.StatusMsg = "ERROR";
                            _downloading--;
                            _errors++;

                            UpdateErrors();
                            UpdateDownloading();

                            Process.Start("notepad.exe", ERROR_FILE_PATH);
                        }
                    }finally
                    {
                        Dispatcher.Invoke(() =>
                        {
                            entry.CancelButton.Visibility = Visibility.Hidden;
                        }, DispatcherPriority.Normal);
                        

                        _startingEntries.TryRemove(siteObj, out _);
                        _inProgressEntries.TryRemove(siteObj, out _);

                        _slim.Release();
                    }

                    await Logger.WriteToLog(SUMMARY_FILE_PATH, $"{siteObj.Url} [SUCCESS]");
                    return;
                }));
            }

            await Task.WhenAll(tasks);

            tasks.Clear();
            if (_startingEntries.Count > currLen)
            {
                await StartDownload();
            }

            tasks.Clear();
        }

        public async Task AddEntry(string addUrl)
        {
            //Empty text box
            if (string.IsNullOrEmpty(addUrl)) return;

            var split = addUrl.Split('|');
            string url = split[0];

            var site = SITES.Where(site => site.isValidSite(url)).FirstOrDefault();
            if (site == null) return;

            if (_startingEntries.Where(val => val.Key.Url == url).Any()) return;

            var name = site.ClassName;
            UrlEntry entry = new UrlEntry()
            {
                Url = url,
                Number = (++entryInd).ToString(),
                //ImgIcon = ImageUtil.ICONS[site.ClassName],
                ImgIconPath = $"/res/{site.ClassName}.ico",
                Name = $"[{site.ClassName}] {url}",
                StatusMsg = "Queued"
            };

            var type = Type.GetType($"WpfDownloader.Sites.{site.ClassName}");
            Site siteObj = null;
            if (split.Length >= 2)
            {
                siteObj = Activator.CreateInstance(type, url, split[1]) as Site;
            }
            else
            {
                siteObj = Activator.CreateInstance(type, url, null) as Site;
            }

            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _urlTextBox.Text = string.Empty;
                _urlEntries.Add(entry);
            }), DispatcherPriority.Background);


            _startingEntries.TryAdd(siteObj, entry);

            await StartDownload();
        }

        private async void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                await AddEntry(_urlTextBox.Text);
            }
        }

        private async void StartButtonClick(object sender, RoutedEventArgs e)
        {
            await StartDownload();
        }

        private void UrlsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UrlEntry item = ((FrameworkElement)e.OriginalSource).DataContext as UrlEntry;
            if (item == null)
                return;

            if (e.ChangedButton == MouseButton.Right)
            {
                Clipboard.SetText(item.Url);
            }else if (e.ChangedButton == MouseButton.Left)
            {
                if (!string.IsNullOrEmpty(item.DownloadPath))
                {
                    string path = item.DownloadPath.Replace('/', '\\');
                    Process.Start("explorer.exe", path);
                }
            }

            e.Handled = true;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {   
            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;

            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
        }


        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            if (_inProgressEntries.Any() || _startingEntries.Any())
            {
                var result = await this.ShowMessageAsync("Downloads in progress",
                    $"You have {_inProgressEntries.Count} entries downloading. Are you sure you want to exit?",
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                    {
                        AnimateHide = false
                    });
                if (result == MessageDialogResult.Affirmative)
                {
                    e.Cancel = false;
                }
                else
                {
                    return;
                }
            }
            else
            {
                e.Cancel = false;
            }

            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
                Properties.Settings.Default.Maximized = false;
            }

            await Task.Run(async () =>
            {
                foreach (var entry in _inProgressEntries)
                {
                    entry.Value.CancelTokenSource.Cancel();
                }

                //_notifyIcon.Visible = false;
                Properties.Settings.Default.Save();
                Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
            });

            //_notifyIcon.Visible = false;
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        private async void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var tasks = new List<Task>();
                if (openFileDialog.FileName.EndsWith(".txt"))
                {
                    var urls = await File.ReadAllLinesAsync(openFileDialog.FileName);
                    tasks.AddRange(urls
                        .Where(url => url.StartsWith("http"))
                        .Select(async url =>
                    {
                        if (!string.IsNullOrEmpty(url)) await AddEntry(url);
                    }));
                }
                else
                {
                    tasks.Add(AddEntry(openFileDialog.FileName));
                }

                await Task.WhenAll(tasks);
            }
        }
        

        private void SortStatus()
        {
            var dataView =
                (ListCollectionView) CollectionViewSource.GetDefaultView(_urlListView.ItemsSource);
            dataView.CustomSort = new StatusComparer();
            dataView.Refresh();
        }

        private void SortName()
        {
            var dataView =
                (ListCollectionView)CollectionViewSource.GetDefaultView(_urlListView.ItemsSource);
            dataView.CustomSort = new NameComparer();
            dataView.Refresh();
        }

        void GridViewColumnHeaderName_Click(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is GridViewColumnHeader ch)) return;
            var dir = ListSortDirection.Ascending;
            if (ch == _lastHeaderClickedName && _lastDirectionName == ListSortDirection.Ascending)
                dir = ListSortDirection.Descending;
            SortName();
            _lastHeaderClickedName = ch; _lastDirectionName = dir;
        }


        private void GridViewColumnHeaderStatus_Click(object sender, RoutedEventArgs e)
        {
            SortStatus();
        }

        private string GetClipboardData()
        {
            try
            {
                string clipboardData = null;
                Exception threadEx = null;
                Thread staThread = new Thread(
                    delegate ()
                    {
                        try
                        {
                            clipboardData = Clipboard.GetText(TextDataFormat.Text);
                        }

                        catch (Exception ex)
                        {
                            threadEx = ex;
                        }
                    });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
                return clipboardData;
            }
            catch (Exception exception)
            {
                return string.Empty;
            }
        }

        private async void Window_Activated(object sender, EventArgs e)
        {
            await Task.Run(async () =>
            {
                string copiedText = GetClipboardData();
                if (UrlValid(copiedText))
                {
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _urlTextBox.Text = copiedText;
                        _urlTextBox.SelectAll();
                    }));
                }
            });
        }

        //Cancel button
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UrlEntry item = ((FrameworkElement)e.OriginalSource).DataContext as UrlEntry;
            if (item == null) return;

            item.CancelTokenSource.Cancel();
        }
    }
}
