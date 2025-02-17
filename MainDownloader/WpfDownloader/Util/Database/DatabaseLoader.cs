using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WpfDownloader.Util.Database
{
    public static class DatabaseLoader
    {
        public static string RemoveIllegalChars(string value) =>
            Regex.Replace(value, @"[^\w\.@-|~+-]", "", RegexOptions.None);
    }
}
