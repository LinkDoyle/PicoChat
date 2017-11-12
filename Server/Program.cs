using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PicoChat
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length != 2)
            {
                ShowUsage();
                return;
            }
            if (!IPAddress.TryParse(args[0], out IPAddress address))
            {
                ShowUsage();
                return;
            }
            if (!int.TryParse(args[1], out int port))
            {
                ShowUsage();
                return;
            }

            Server server = new Server(address, port);
            server.Start();
            Console.WriteLine("Press return to exit");
            Console.ReadLine();
            server.Stop();
        }

        private static void ShowUsage()
        {
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} address port");
            Console.ReadLine();
        }


    }
}
