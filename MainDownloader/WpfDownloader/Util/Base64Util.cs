using MonoTorrent.BEncoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Util
{
    public class Base64Util
    {
        public static string DecodeB64Str(string str) => Encoding.UTF8.GetString(Convert.FromBase64String(str));

    }
}
