using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader.Loggers
{
    public static class Logger
    {
        public static async Task WriteToLog(string logPath, string content)
        {
            using (var writer = File.AppendText(logPath))
            {
                await writer.WriteLineAsync(content);
            }
        }
    }
}
