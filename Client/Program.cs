using System;
using System.Linq;
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
            string hostName = args[0];
            if (!int.TryParse(args[1], out int port))
            {
                ShowUsage();
                return;
            }
            Console.WriteLine("Press return when the server is started.");
            Console.ReadLine();

            IPHostEntry ipHost = Dns.GetHostEntry(hostName);
            IPAddress ipAddress = ipHost.AddressList.Where(address => address.AddressFamily == AddressFamily.InterNetwork).First();
            if (ipAddress == null)
            {
                Console.WriteLine("No IPv4 address");
                return;
            }
            Client client = new Client(ipAddress, port);
            client.Start();
            Console.ReadLine();
        }

        private static void ShowUsage()
        {
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} server port");
        }

    }
}
