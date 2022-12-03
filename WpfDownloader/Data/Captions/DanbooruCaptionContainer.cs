using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Data.Captions
{
    public class DanbooruCaptionContainer
    {
        public string OrigImagePath { get; set; }
        public string OutputImagePath { get; set; }
        public IEnumerable<DanbooruCaptionData> Captions { get; set; }
    }
}
