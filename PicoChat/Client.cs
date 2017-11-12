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
        string name;
        CancellationTokenSource cts_ = new CancellationTokenSource();

        public string Name => name;
        public bool Connected { get => state_ == ConectionState.CONNECTED; }
        public IPAddress ServerAddress { get; set; }
        public int ServerPort { get; set; }

        public event EventHandler<Message> MessageReceived;
        public event EventHandler<ConectionState> StateChaged;
        public event EventHandler<SocketException> SocketExceptionRaising;
        public event EventHandler<SystemMessageEventArgs> SystemMessageReceived;

        public enum ConectionState
        {
            CONNECTED,
            DISCONNECTED
        }

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
                        Task tReceiver = Receiver(stream_, cts_);
                        tReceiver.Wait();
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
                    catch (IOException ex)
                    {
                        if (ex.InnerException is SocketException)
                        {
                            SocketExceptionRaising?.Invoke(this, (SocketException)ex.InnerException);
                        }
                        else
                        {
                            logging_.Debug(ex);
                        }
                    }
                }
                logging_.Debug("Receiver task exited");
            });
        }

        void Send(MessageType messageType)
        {
            Send(messageType, null);
        }

        void Send(MessageType type, byte[] content)
        {
            if (!Connected)
            {
                FireError("FAILED TO SEND MESSAGE, PLEASE CHECK YOUR CONNECTION");
                FireInfo("PLEASE USE /connect TO CONNECT");
                return;
            }
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

        void FireInfo(string message)
        {
            MessageReceived?.Invoke(this, new Message("[Info]", "[System]", message));
        }

        void FireError(string message)
        {
            MessageReceived?.Invoke(this, new Message("[Error]", "[System]", message));
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

        public void ListJoinedRooms()
        {
            Send(MessageType.CLIENT_LIST_JOINED_ROOMS);
        }

        public void SendMessage(string roomName, string content)
        {
            Send(MessageType.CLIENT_MESSAGE, Serializer.SerializeToBytes(new Message(name, roomName, content)));
        }
    }
}
