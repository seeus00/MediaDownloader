using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TreeListView;

namespace WpfDownloader.WpfData.Controls.Converters
{
    public class ParentNodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var menuItem = (MenuItem)value;
            ContextMenu menu = menuItem.Parent as ContextMenu;
            var entry = menu.DataContext;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
