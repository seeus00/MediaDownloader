using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TreeListView;
using Wpf.Ui.Controls;
using WpfDownloader.Util.LinkSaver;
using MenuItem = System.Windows.Controls.MenuItem;

namespace WpfDownloader.WpfData.Controls
{
    public class LinkEventArgs : EventArgs
    {
        public LinkData Data { get; set; }
    }


    public partial class TreeViewControl : UserControl
    {
        public event EventHandler<LinkEventArgs> DownloadMenuClicked;

        private TreeListView.TreeListView treeView;

        public TreeViewControl()
        {
            InitializeComponent();

            treeView = FindName("MainTreeView") as TreeListView.TreeListView; ;
        }

        private void TreeListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as TreeListViewItem;
            if (e.ClickCount > 1 && item != null && item.DataContext != null)
            {
                e.Handled = true;

                var entry = item.DataContext as UrlEntry;
                Debug.WriteLine(entry.Name);
                //entry.OpenPath();

            }
        }

        private void ToggleButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) return;
        }

        private void ToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) return;
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEntry = treeView.SelectedItem as UrlEntry;
            if (selectedEntry != null) selectedEntry.OpenPath();
        }

        //Recursivley look for item and remove it from subitems
        private void FindAndRemove(UrlEntry target, ObservableCollection<UrlEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (target == entry)
                {
                    entries.Remove(entry);
                    return;
                }
                FindAndRemove(target, entry.SubItems);
            }
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEntry = treeView.SelectedItem as UrlEntry;
            if (selectedEntry == null) return;

            var source = treeView.ItemsSource as ObservableCollection<UrlEntry>;
            FindAndRemove(selectedEntry, source);

            (treeView.ContextMenu.Items[2] as MenuItem).Visibility = Visibility.Visible;
            (treeView.ContextMenu.Items[treeView.ContextMenu.Items.Count - 1] as MenuItem).Visibility = Visibility.Collapsed;
        }

        private async void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEntry = treeView.SelectedItem as UrlEntry;
            if (selectedEntry == null) return;

            LinkSaveManager.AddLinkData(new LinkData()
            {
                Url = selectedEntry.Url,
                FullPath = selectedEntry.DownloadPath
            });

            (treeView.ContextMenu.Items[3] as MenuItem).Visibility = Visibility.Visible;
            (treeView.ContextMenu.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
        }

        private void RemoveLinkMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEntry = treeView.SelectedItem as UrlEntry;
            if (selectedEntry == null) return;

            LinkSaveManager.RemoveLinkData(new LinkData()
            {
                Url = selectedEntry.Url,
                FullPath = selectedEntry.DownloadPath
            });


            var source = treeView.ItemsSource as ObservableCollection<UrlEntry>;
            FindAndRemove(selectedEntry, source);

            
        }

        private void MainTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedEntry = treeView.SelectedItem as UrlEntry;
            var source = treeView.ItemsSource as ObservableCollection<UrlEntry>;

            //If selected item is a parent node
            if (source.Where(entry => selectedEntry == entry).Any())
            {
                treeView.ContextMenu = treeView.Resources["ParentNodeContext"] as ContextMenu;
                if (LinkSaveManager.ContainsLinkData(new LinkData()
                {
                    Url = selectedEntry.Url,
                    FullPath = selectedEntry.DownloadPath
                }))
                {
                    (treeView.ContextMenu.Items[4] as MenuItem).Visibility = Visibility.Visible;
                    (treeView.ContextMenu.Items[3] as MenuItem).Visibility = Visibility.Visible;
                    (treeView.ContextMenu.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
                }
                else
                {
                    (treeView.ContextMenu.Items[4] as MenuItem).Visibility = Visibility.Collapsed;
                    (treeView.ContextMenu.Items[2] as MenuItem).Visibility = Visibility.Visible;
                    (treeView.ContextMenu.Items[3] as MenuItem).Visibility = Visibility.Collapsed;
                }

            }
            else
            {
                treeView.ContextMenu = treeView.Resources["ChildNodeContext"] as ContextMenu;
                if (selectedEntry != null && File.Exists(selectedEntry.DownloadPath))
                {
                    (treeView.ContextMenu.Items[2] as MenuItem).Visibility = Visibility.Visible;
                }else
                {
                    (treeView.ContextMenu.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
                }
            }

            e.Handled = true;
        }

        private void DeleteFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEntry = treeView.SelectedItem as UrlEntry;
            var source = treeView.ItemsSource as ObservableCollection<UrlEntry>;

            if (File.Exists(selectedEntry.DownloadPath))
            {
                File.Delete(selectedEntry.DownloadPath);
                FindAndRemove(selectedEntry, source);

                (treeView.ContextMenu.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
            }
            
            e.Handled = true;
        }


        protected virtual void OnClickedMenuReached(LinkEventArgs e)
        {
            EventHandler<LinkEventArgs> handler = DownloadMenuClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void DownloadLinkMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEntry = treeView.SelectedItem as UrlEntry;
            if (selectedEntry == null) return;

            var data = new LinkData()
            {
                Url = selectedEntry.Url,
                FullPath = selectedEntry.DownloadPath
            };
            if (!LinkSaveManager.ContainsLinkData(data)) return;

            OnClickedMenuReached(new LinkEventArgs()
            {
                Data = data
            });

            (treeView.ContextMenu.Items[4] as MenuItem).Visibility = Visibility.Collapsed;

            var source = treeView.ItemsSource as ObservableCollection<UrlEntry>;
            FindAndRemove(selectedEntry, source);
        }
    }
}
