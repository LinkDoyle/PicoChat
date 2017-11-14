#define DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PicoChat.Common;

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
        public string CurrentRoomName { get; set; }

        public event EventHandler<LoginInfo> LoginOK;
        public event EventHandler<LoginInfo> LoginFailed;
        public event EventHandler LogoutOK;
        public event EventHandler<RoomInfo> LeavedFromRoom;
        public event EventHandler<RoomInfo> JoinedInRoom;
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
                try
                {
                    while (true)
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            break;
                        }
                        DataPackage dataPackage = DataPackage.FromStream(stream);
                        Debug.WriteLine($"Received message type: {dataPackage.Type.ToString()}");
                        switch (dataPackage.Type)
                        {
                            case MessageType.SYSTEM_LOGIN_OK:
                                {
                                    LoginInfo info = Serializer.Deserialize<LoginInfo>(Encoding.UTF8.GetString(dataPackage.Data));
                                    name = info.Name;
                                    LoginOK?.Invoke(this, info);
                                }
                                break;
                            case MessageType.SYSTEM_LOGIN_FAILED:
                                {
                                    LoginInfo info = Serializer.Deserialize<LoginInfo>(Encoding.UTF8.GetString(dataPackage.Data));
                                    LoginFailed?.Invoke(this, info);
                                }
                                break;
                            case MessageType.SYSTEM_JOIN_ROOM_OK:
                                {
                                    RoomInfo roomInfo = Serializer.Deserialize<RoomInfo>(Encoding.UTF8.GetString(dataPackage.Data));
                                    JoinedInRoom?.Invoke(this, roomInfo);
                                }
                                break;
                            case MessageType.SYSTEM_LEAVE_ROOM_OK:
                                {
                                    RoomInfo roomInfo = Serializer.Deserialize<RoomInfo>(Encoding.UTF8.GetString(dataPackage.Data));
                                    LeavedFromRoom?.Invoke(this, roomInfo);
                                }
                                break;
                            case MessageType.CLIENT_MESSAGE:
                                {
                                    Message message = Serializer.DeserializeMessage(Encoding.UTF8.GetString(dataPackage.Data));
                                    MessageReceived?.Invoke(this, message);
                                }
                                break;
                            case MessageType.CLIENT_LOGOUT:
                                {
                                    name = null;
                                    LogoutOK?.Invoke(this, null);
                                    goto default;
                                }
                            default:
                                SystemMessageReceived?.Invoke(this, new SystemMessageEventArgs(dataPackage.Type, dataPackage.Data));
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
            stream_.Write(new DataPackage(type, content));
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
