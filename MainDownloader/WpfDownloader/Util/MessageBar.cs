using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader.Util
{
    //Shows messages
    public class MessageBar : IDisposable
    {
        private static readonly object ConsoleWriterLock = new object();
        private static readonly int MAX_MSG_LENGTH = 10;

        private static readonly int ELLIPSE_LENGTH = 3;

        public static int GLOBAL_LINE_NUM = -1;

        public int CurrLineNum { get; set; }

        private bool firstPrint = true;

        public MessageBar()
        {
            lock(ConsoleWriterLock)
            {
                if (GLOBAL_LINE_NUM == -1)
                    GLOBAL_LINE_NUM = (Console.CursorTop - 1 < 0) ? 0 : Console.CursorTop - 1;
                CurrLineNum = GLOBAL_LINE_NUM++;
            }
        }

        public void PrintMsg(string baseMsg, string msg)
        {
            lock (ConsoleWriterLock)
            {
                msg = (msg.Length >= MAX_MSG_LENGTH) ? msg.Substring(0, MAX_MSG_LENGTH) +
                    new string('.', ELLIPSE_LENGTH) :
                    msg + new string(' ', MAX_MSG_LENGTH + ELLIPSE_LENGTH - msg.Length);


                Console.CursorTop = CurrLineNum;
                Console.CursorLeft = 0;
                if (firstPrint)
                {
                    Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
                    firstPrint = false;
                }

                Console.Write($"{baseMsg} {msg}");
            }
        }

        public void Dispose()
        {
            lock (ConsoleWriterLock)
            {
                Console.CursorTop = MessageBar.GLOBAL_LINE_NUM;
                Console.CursorLeft = 0;
            }
        }
    }
}
