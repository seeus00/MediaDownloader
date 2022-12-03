using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Data.Imaging
{
    public class ZipToGifData
    {
        public string TempPathName { get; set; }
        public IEnumerable<GifFrameData> Frames { get; set; }
    }
}
