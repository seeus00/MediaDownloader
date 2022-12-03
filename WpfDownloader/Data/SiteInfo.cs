using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Downloader.Data
{
    public class SiteInfo
    {
        private static readonly char SPLIT_CHAR = ',';

        public string Domains { get; set; }
        public string ClassName { get; set; }

        public bool isValidSite(string testUrl)
        {
            return Domains.Split(SPLIT_CHAR)
                .Where(domain => new Regex(domain).Match(testUrl).Success)
                .Any();
        }
    }
}
