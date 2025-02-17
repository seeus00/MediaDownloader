using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Data.Captions
{
    public class DanbooruCaptionData
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public int PosX { get; set; }
        public int PosY { get; set; }

        public string CaptionText { get; set; }

        public MagickColor BgColor { get; set; }

    }
}
