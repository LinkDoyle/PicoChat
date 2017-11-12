#define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Windows.Threading;
using PicoChat.Common;
using System.IO;

namespace PicoChat
{
    class Logging
    {
        public void Debug(object message)
        {
#if DEBUG
            Trace.TraceInformation($"{message}");
#endif
        }
    }
    class Client
    {
        Logging logging_ = new Logging();
        ConectionState state_ = ConectionState.DISCONNECTED;
        Socket socket_;
        NetworkStream stream_;
        //BlockingCollection<string> messageQueue = new BlockingCollection<string>();
        string name;
        CancellationTokenSource cts_ = new CancellationTokenSource();


        public string Name => name;

        public bool Connected { get => state_ == ConectionState.CONNECTED; }
        public enum ConectionState
        {
            CONNECTED,
            DISCONNECTED
        }

        public IPAddress ServerAddress { get; }
        public int ServerPort { get; }

        public class SystemMessageEventArgs : EventArgs
        {
            public MessageType Type { get; }
            public byte[] Data { get; }
            public SystemMessageEventArgs(MessageType type, byte[] data)
            {
                Type = type;
                Data = data;
            }
        }

        public event EventHandler<Message> MessageReceived;
        public event EventHandler<ConectionState> StateChaged;
        public event EventHandler<SocketException> SocketExceptionRaising;
        public event EventHandler<SystemMessageEventArgs> SystemMessageReceived;

        public Client(IPAddress serverAddress, int port)
        {
            ServerAddress = serverAddress;
            ServerPort = port;
        }

        public void Connect()
        {
            Debug.Assert(state_ == ConectionState.DISCONNECTED);
            SendAndReceive();
        }

        public void Disconnect()
        {
            Debug.Assert(state_ == ConectionState.CONNECTED);
            cts_.Cancel();
        }

        public Task SendAndReceive()
        {
            return Task.Run(() =>
            {
                logging_.Debug("SendAndReceive Started.");

                try
                {
                    using (socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        socket_.Connect(ServerAddress, ServerPort);
                        state_ = ConectionState.CONNECTED;
                        logging_.Debug("Client successfully connected");
                        StateChaged?.Invoke(this, ConectionState.CONNECTED);

                        stream_ = new NetworkStream(socket_);
                        //Task tSender = Sender(stream_, cts_);
                        Task tReceiver = Receiver(stream_, cts_);
                        tReceiver.Wait();
                        //Task.WaitAll(tSender, tReceiver);
                    }
                }
                catch (SocketException ex)
                {
                    SocketExceptionRaising?.Invoke(this, ex);
                    logging_.Debug($"{ex}");
                }
                catch (Exception ex)
                {
                    logging_.Debug($"{ex}");
                    throw;
                }
                state_ = ConectionState.DISCONNECTED;
                StateChaged?.Invoke(this, (ConectionState)Client.ConectionState.DISCONNECTED);

                logging_.Debug("SendAndReceive Finished.");
            });
        }

        //public async Task Sender(NetworkStream stream, CancellationTokenSource cts)
        //{
        //    logging_.Debug("Sender task");
        //    await SendMessage("/hello");
        //    Console.WriteLine("Enter a string to send, /shutdown to exit");

        //    while (true)
        //    {
        //        string message = messageQueue.Take();
        //        await SendMessage(message);
        //        if (message == "/shutdown")
        //        {
        //            cts.Cancel();
        //            logging_.Debug("Sender task closes");
        //            break;
        //        }
        //    }
        //}

        public Task Receiver(NetworkStream stream, CancellationTokenSource cts)
        {
            return Task.Run(() =>
            {
                logging_.Debug("Receiver task");
                //stream.ReadTimeout = 5000;
                using (BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    try
                    {
                        while (true)
                        {
                            if (cts.Token.IsCancellationRequested)
                            {
                                cts.Token.ThrowIfCancellationRequested();
                                break;
                            }
                            int typeValue = binaryReader.ReadInt32();
                            MessageType type = Enum.IsDefined(typeof(MessageType), typeValue)
                                ? (MessageType)typeValue
                                : MessageType.SYSTEM_UNKNOWN;
                            int length = binaryReader.ReadInt32();
                            byte[] data = binaryReader.ReadBytes(length);
                            Debug.WriteLine($"Received message type: {type.ToString()}");
                            switch (type)
                            {
                                case MessageType.CLIENT_MESSAGE:
                                    {
                                        Message message = Serializer.DeserializeMessage(Encoding.UTF8.GetString(data));
                                        MessageReceived?.Invoke(this, message);
                                    }
                                    break;
                                default:
                                    SystemMessageReceived?.Invoke(this, new SystemMessageEventArgs(type, data));
                                    break;
                            }
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        logging_.Debug(ex);
                    }
                }
                logging_.Debug("Receiver task exited");
            });
        }

        //public void Send(string message)
        //{
        //    messageQueue.Add(message);
        //}


        //async Task SendMessage(string message)
        //{
        //    byte[] buffer = Encoding.UTF8.GetBytes($"{message}");
        //    await stream_.WriteAsync(buffer, 0, buffer.Length);
        //    await stream_.FlushAsync();
        //}


        void Send(MessageType messageType)
        {
            Send(messageType, null);
        }

        void Send(MessageType type, byte[] content)
        {
            using (BinaryWriter writer = new BinaryWriter(stream_, Encoding.UTF8, true))
            {
                writer.Write((uint)type);
                if (content != null)
                {
                    writer.Write(content.Length);
                    writer.Write(content);
                }
                else
                {
                    writer.Write(0);
                }
            }
        }

        public void Login(string name)
        {
            this.name = name;
            Send(MessageType.CLIENT_LOGIN, Encoding.UTF8.GetBytes(name));
        }

        public void Logout()
        {
            Send(MessageType.CLIENT_LOGOUT);
        }

        public void Join(string roomName)
        {
            Send(MessageType.CLIENT_JOIN_ROOM, Encoding.UTF8.GetBytes(roomName));
        }

        public void Leave(string roomName)
        {
            Send(MessageType.CLIENT_LEAVE_ROOM, Encoding.UTF8.GetBytes(roomName));
        }

        public void SendMessage(string roomName, string content)
        {
            Send(MessageType.CLIENT_MESSAGE, Serializer.SerializeToBytes(new Message(name, roomName, content)));
        }

        public void Ping()
        {
            throw new NotImplementedException();
        }
    }
}
