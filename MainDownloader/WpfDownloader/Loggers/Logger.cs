using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Downloader.Loggers
{
    public static class Logger
    {
        private readonly static SemaphoreSlim slim = new SemaphoreSlim(1);

        public static async Task WriteToLog(string logPath, string content)
        {
            await slim.WaitAsync();
            using (var writer = File.AppendText(logPath))
            {
                await writer.WriteLineAsync(content);
            }

            slim.Release();
        }
    }
}
