using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Linq;

namespace XRECV
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.inst.Write("==============================================================================================");
            Log.inst.Write("CSSOFTWARE XRECV FLIGHT DATA INGEST V 1.0.7");
            Log.inst.Write("==============================================================================================");

            //ProgramConfig.inst.CheckForConfigFiles();

            DataReciever reciever = null;

            try
            {
                if (args.Length == 0)
                {
                    reciever = new DataReciever(5000);
                }
                else if(args.Length == 1)
                {
                    reciever = new DataReciever(int.Parse(args[0]));
                }
            }
            catch(Exception ex)
            {
                Log.inst.Write("FATAL ERROR CREATING DATA RECIEVER!");
                Log.inst.Write(ex.ToString());
            }

            reciever.Run();
        }
    }
}