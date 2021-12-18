using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace ConnectivityMonitor
{
    class Program
    {
        static readonly object finishLocker = new object();

        static private bool finish;
        static public bool Finish
        {
            get
            {
                lock (finishLocker) { return finish; }
            }
            set
            {
                lock (finishLocker) { finish = value; }
            }
        }


        static void Main(string[] args)
        {
            string address = "72.31.202.145";
            if (args.Length > 0)
                address = args[0];
            int buflen = 32;

            ConnectionMonitorArgs cmonargs = new ConnectionMonitorArgs();
            cmonargs.Address = address;
            cmonargs.Timeout = 2000;
            cmonargs.Buffer = new byte[32]; for(int i = 0; i < buflen; ++i) cmonargs.Buffer[i] = 0xFF;
            cmonargs.Options = new PingOptions();

            Thread cmonitor = new Thread(ConnectionMonitor);


            ConsoleKeyInfo cki;
            Console.TreatControlCAsInput = true;

            cmonitor.Start(cmonargs);
            do
            {
                cki = Console.ReadKey(true);

                switch (cki.Key)
                {
                    //case ConsoleKey.A:
                    //    Console.WriteLine("{0}ms average ping time determined.", Times.Average);
                    //    break;
                    default:
                        //Console.WriteLine("a key was pressed.");
                        break;
                }

            } while (cki.Key != ConsoleKey.Escape && ((cki.Modifiers & ConsoleModifiers.Control) != 0 && cki.Key == ConsoleKey.C) );
            Console.WriteLine("Signalling ConnectionMonitor() to finish and waiting to exit...");
            Finish = true;
            cmonitor.Join();
            

        }

        static void ConnectionMonitor(object arg)
        {
            ConnectionMonitorArgs args = (ConnectionMonitorArgs)arg;

            Ping pinger = new Ping();
            PingOptions options = new PingOptions();
            PingReply reply;
            Times times = new Times();
            int counter = 0;

            while (Finish == false)
            {
                ++counter;
                reply = pinger.Send(args.Address, args.Timeout, args.Buffer, args.Options);
                if (reply.Status == IPStatus.Success)
                {
                    times.Append(reply.RoundtripTime);
                    Console.WriteLine("Reply {0} from {1} in {2}ms.", counter, reply.Address.ToString(), reply.RoundtripTime);
                }
                else
                {
                    Console.WriteLine("Reply {0} from {1} failed.", counter, reply.Address.ToString());
                }
                Thread.Sleep(2000);
            }
        }
    }

    struct ConnectionMonitorArgs
    {
        public string Address { get; set; }
        public int Timeout { get; set; }
        public byte[] Buffer { get; set; }
        public PingOptions Options { get; set; }
    }

    class Times
    {
        long[] times = new long[2048];
        int I = 0;

        long Average
        {
            get
            {
                long sum = 0;
                for (int i = 0; i < 2048; ++i)
                {
                    sum += times[i];
                }
                return sum / 2048;
            }
        }

        public void Append(long time)
        {
            times[I++] = time;
            if (I > 2047) I = 0;
        }
    }
}
