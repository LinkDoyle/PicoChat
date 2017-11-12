#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PicoChat
{
    class Logging
    {
        public void Debug(string message)
        {
#if DEBUG
            Trace.WriteLine($"{message}");
            Trace.TraceInformation($"{message}");
#endif
        }
    }
    class Client
    {
        private Logging logging = new Logging();
        private const int ReadBufferSize = 1024;

        public IPAddress ServerAddress { get; }
        public int ServerPort { get; }

        public Client(IPAddress serverAddress, int port)
        {
            ServerAddress = serverAddress;
            ServerPort = port;
        }


        public async Task SendAndReceive()
        {
            try
            {
                using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    client.Connect(ServerAddress, ServerPort);
                    logging.Debug("Client successfully connected");
                    var stream = new NetworkStream(client);
                    var cts = new CancellationTokenSource();

                    Task tSender = Sender(stream, cts);
                    Task tReceiver = Receiver(stream, cts.Token);
                    await Task.WhenAll(tSender, tReceiver);
                }
            }
            catch (SocketException ex)
            {
                logging.Debug($"{ex.Message}");
            }
            catch (Exception ex)
            {
                logging.Debug($"{ex.Message}");
            }
        }

        internal void Start()
        {
            SendAndReceive().Wait();
        }

        private async Task SendMessage(NetworkStream stream, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes($"{message}");
            await stream.WriteAsync(buffer, 0, buffer.Length);
            await stream.FlushAsync();
        }

        public async Task Sender(NetworkStream stream, CancellationTokenSource cts)
        {
            logging.Debug("Sender task");
            await SendMessage(stream, "/hello");
            Console.WriteLine("Enter a string to send, /shutdown to exit");

            while (true)
            {
                Console.Write("client>");
                string message = Console.ReadLine();
                await SendMessage(stream, message);
                if (message == "/shutdown")
                {
                    cts.Cancel();
                    logging.Debug("Sender task closes");
                    break;
                }
            }
        }



        public async Task Receiver(NetworkStream stream, CancellationToken token)
        {
            try
            {
                stream.ReadTimeout = 5000;
                logging.Debug("Receiver task");
                byte[] readBuffer = new byte[ReadBufferSize];
                while (true)
                {
                    Array.Clear(readBuffer, 0, ReadBufferSize);

                    int read = await stream.ReadAsync(readBuffer, 0, ReadBufferSize, token);
                    string receivedLine = Encoding.UTF8.GetString(readBuffer, 0, read);
                    logging.Debug($"[RECEIVED] {receivedLine}");
                }
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
