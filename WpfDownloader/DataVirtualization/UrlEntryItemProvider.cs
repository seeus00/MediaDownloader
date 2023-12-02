using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using WpfDownloader.DataVirtualization;
using WpfDownloader.WpfData;

namespace TreeListView.DataVirtualization
{
    internal class UrlEntryItemProvider : IItemsProvider<UrlEntry>
    {
        private List<UrlEntry> items;

        public UrlEntryItemProvider(List<UrlEntry> items)
        {
            this.items = items;
        }

        public int FetchCount() => items.Count;

        public IList<UrlEntry> FetchRange(int startIndex, int count)
        {
            var list = new List<UrlEntry>();
            for (int i = startIndex; i < startIndex + count; i++)
            {
                if (i >= items.Count) break;
                list.Add(items[i]);
            }

            return list;
        }
    }
}
