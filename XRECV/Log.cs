using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace XRECV
{
    public sealed class Log
    {
        private static Log _inst;
        private static object instLock = new object();
        public static Log inst
        {
            get
            {
                lock (instLock)
                {
                    if (_inst == null)
                        _inst = new Log();
                    return _inst;
                }
            }
        }

        private FileStream logStream;
        private StreamWriter logWriter;

        public Log()
        {
            if (File.Exists("xrecv.log"))
            {
                logStream = File.Open("xrecv.log", FileMode.Append, FileAccess.Write);
            }
            else
                logStream = File.Open("xrecv.log", FileMode.Create, FileAccess.Write);

            logWriter = new StreamWriter(logStream);
        }

        public void Write(string Message)
        {
            Console.WriteLine(Message);
            logWriter.WriteLine(DateTime.Now.ToString() + "--" + Message);
        }

        public void close()
        {
            logWriter.Flush();
            logWriter.Close();
            logStream.Close();
        }
    }
}