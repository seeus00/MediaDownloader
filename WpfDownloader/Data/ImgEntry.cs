using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader.Data
{
    public struct ImgEntry
    {
        public string Url { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }
}
