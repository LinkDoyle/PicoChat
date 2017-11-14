using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using PicoChat.Common;
using System.Diagnostics;

namespace PicoChat.Tests
{
    [TestClass()]
    public class ServerTests
    {
        TestContext testContextInstance;
        Server server;
        readonly IPAddress address = IPAddress.Loopback;
        const int port = 23333;

        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        void StartServer()
        {
            Trace.WriteLine("Starting the server...");
            server = new Server(address, port);
            server.Start();
        }

        void StopServer()
        {
            Trace.WriteLine("Stopping the server...");
            server.Stop();
        }

        void StartAndWaitClients(int count, int messageCount)
        {
            Client[] clients = new Client[count];
            Task[] clientTasks = new Task[count];
            Trace.WriteLine("Creating clients...");
            for (int i = 0; i < clients.Length; ++i)
            {
                Client client = clients[i] = new Client(address, port);
                CountdownEvent countdownEvent = new CountdownEvent(1);
                string clientName = $"Clients[{i}]";
                client.StateChaged += (sender, e) =>
                {
                    if (client.Connected)
                    {
                        Trace.WriteLine($"{clientName} connected.");
                        client.Login(clientName);
                    }
                    else
                    {
                        Trace.WriteLine($"{clientName} disconnected.");
                    }
                };
                client.LoginOK += (sender, e) =>
                {
                    Trace.WriteLine($"{clientName}  logged in.");
                    client.Join("Room");
                };
                client.LoginFailed += (sender, e) => Assert.Fail();
                client.JoinedInRoom += (sender, e) =>
                {
                    Trace.WriteLine($"{clientName} joinned in {e.Name}.");
                    for (int j = 0; j < messageCount; ++j)
                    {
                        client.SendMessage("Room", "Hello");
                    }
                    countdownEvent.Signal();
                };
                clientTasks[i] = new Task(() =>
                {
                    int receivedCount = 0;
                    client.SystemMessageReceived += (sender, e) =>
                    {
                        if (e.Type == MessageType.SYSTEM_MESSAGE_OK)
                        {
                            receivedCount++;
                        }
                    };
                    client.Connect();
                    Assert.IsTrue(client.Connected);

                    var task = client.HandleAsync();
                    countdownEvent.Wait();
                    Thread.Sleep(5000);
                    Trace.WriteLine($"{clientName} received {receivedCount} messages (excepted {messageCount}).");
                    client.Disconnect();

                    Trace.WriteLine($"{clientName} finished the task.");
                }, TaskCreationOptions.LongRunning);
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (Task t in clientTasks)
            {
                t.Start();
            }
            Trace.WriteLine("Waiting for all clients to exit...");
            Task.WaitAll(clientTasks);
            stopwatch.Stop();
            Trace.WriteLine($"Time usage: {stopwatch.ElapsedMilliseconds} ms.");
        }

        [TestMethod()]
        public void ServerAndClientTest()
        {
            StartServer();
            StartAndWaitClients(50, 100);
            StopServer();
        }

        [TestMethod()]
        public void ClientOnlyTest()
        {
            StartAndWaitClients(50, 100);
        }
    }
}