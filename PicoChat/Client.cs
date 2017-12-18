using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PicoChat.Common;
using System.Drawing;

namespace PicoChat
{
    public class Client : IClient
    {
        private Socket _socket;
        private NetworkStream _stream;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private ConectionState _state = ConectionState.DISCONNECTED;

        public string Name { get; private set; }
        public bool Connected => _state == ConectionState.CONNECTED;
        public IPAddress ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string CurrentRoomName { get; set; }

        public event EventHandler<LoginInfo> LoginOK;
        public event EventHandler<LoginInfo> LoginFailed;
        public event EventHandler LogoutOK;
        public event EventHandler<RoomInfo> LeavedFromRoom;
        public event EventHandler<RoomInfo> JoinedInRoom;
        public event EventHandler<Message> MessageReceived;
        public event EventHandler<ImageMessage> ImageMessageReceived;
        public event EventHandler<Receipt> MessageArrivied;
        public event EventHandler<ConectionState> StateChaged;
        public event EventHandler ReceiverTaskExited;
        public event EventHandler<SocketException> SocketExceptionRaising;
        public event EventHandler<SystemMessageEventArgs> UnknownMessageReceived;
        public event EventHandler<string> SystemMessageReceived;

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
            Debug.Assert(_state == ConectionState.DISCONNECTED);
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(ServerAddress, ServerPort);
                _stream = new NetworkStream(_socket);

                _state = ConectionState.CONNECTED;
                Trace.TraceInformation("Client successfully connected");
                StateChaged?.Invoke(this, ConectionState.CONNECTED);
            }
            catch (SocketException ex)
            {
                SocketExceptionRaising?.Invoke(this, ex);
                Trace.TraceInformation($"{ex}");
            }
        }

        public void BeginConnect(AsyncCallback requestCallback)
        {
            Debug.Assert(_state == ConectionState.DISCONNECTED);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.BeginConnect(ServerAddress, ServerPort, requestCallback, null);
        }

        public void EndConnect(IAsyncResult ar)
        {
            _socket.EndConnect(ar);
            _stream = new NetworkStream(_socket);
            Trace.TraceInformation($"ar.IsCompleted: {ar.IsCompleted}");
            _state = ConectionState.CONNECTED;
            Trace.TraceInformation("Client successfully connected");
        }

        public async Task HandleAsync()
        {
            Task task = Receiver(_stream, _cts);
            task.Start();
            await task;
        }

        public void Disconnect()
        {
            Debug.Assert(_state == ConectionState.CONNECTED);
            Send(MessageType.CLIENT_DISCONNECT);
            _cts.Cancel();
        }

        private Task Receiver(NetworkStream stream, CancellationTokenSource cts)
        {
            return new Task(() =>
            {
                Trace.TraceInformation("Receiver task starting...");
                //stream.ReadTimeout = 5000;
                try
                {
                    while (true)
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }
                        DataPackage dataPackage = DataPackage.FromStream(stream);
                        switch (dataPackage.Type)
                        {
                            case MessageType.SYSTEM_LOGIN_OK:
                                {
                                    LoginInfo info = Serializer.Deserialize<LoginInfo>(dataPackage.Data);
                                    Name = info.Name;
                                    LoginOK?.Invoke(this, info);
                                }
                                break;
                            case MessageType.SYSTEM_LOGIN_FAILED:
                                {
                                    LoginInfo info = Serializer.Deserialize<LoginInfo>(dataPackage.Data);
                                    LoginFailed?.Invoke(this, info);
                                }
                                break;
                            case MessageType.SYSTEM_JOIN_ROOM_OK:
                                {
                                    RoomInfo roomInfo = Serializer.Deserialize<RoomInfo>(dataPackage.Data);
                                    JoinedInRoom?.Invoke(this, roomInfo);
                                }
                                break;
                            case MessageType.SYSTEM_LEAVE_ROOM_OK:
                                {
                                    RoomInfo roomInfo = Serializer.Deserialize<RoomInfo>(dataPackage.Data);
                                    LeavedFromRoom?.Invoke(this, roomInfo);
                                }
                                break;
                            case MessageType.CLIENT_MESSAGE:
                                {
                                    Message message = Serializer.Deserialize<Message>(dataPackage.Data);
                                    MessageReceived?.Invoke(this, message);
                                }
                                break;
                            case MessageType.CLIENT_IMAGE_MESSAGE:
                                {
                                    var imageMessage = Serializer.Deserialize<ImageMessage>(dataPackage.Data);
                                    ImageMessageReceived?.Invoke(this, imageMessage);
                                }
                                break;
                            case MessageType.CLIENT_FILE_MESSAGE:
                                {
                                    var fileMessage = Serializer.Deserialize<FileMessage>(dataPackage.Data);
                                    FileMessageReived?.Invoke(this, fileMessage);
                                }
                                break;
                            case MessageType.SYSTEM_FILE_TRANSFER:
                                {
                                    var fileMessage = Serializer.Deserialize<FileMessage>(dataPackage.Data);
                                    FileReived?.Invoke(this, fileMessage);
                                }
                                break;
                            case MessageType.SYSTEM_MESSAGE_OK:
                                {
                                    Receipt receipt = Serializer.Deserialize<Receipt>(dataPackage.Data);
                                    MessageArrivied?.Invoke(this, receipt);
                                }
                                break;
                            case MessageType.SYSTEM_MESSAGE:
                            {
                                var message = Encoding.UTF8.GetString(dataPackage.Data);
                                SystemMessageReceived?.Invoke(this, message);
                                break;
                            }
                            case MessageType.CLIENT_LOGOUT:
                                {
                                    Name = null;
                                    LogoutOK?.Invoke(this, null);
                                    goto default;
                                }
                            default:
                                UnknownMessageReceived?.Invoke(this, new SystemMessageEventArgs(dataPackage.Type, dataPackage.Data));
                                break;
                        }
                    }
                }
                catch (EndOfStreamException ex)
                {
                    Debug.WriteLine($"Client {Name} Receiver: {ex.Message}");
                }
                catch (IOException ex)
                {
                    if (ex.InnerException is SocketException)
                    {
                        SocketExceptionRaising?.Invoke(this, (SocketException)ex.InnerException);
                    }
                    else
                    {
                        Trace.TraceInformation($"{ex}");
                    }
                }
                ReceiverTaskExited?.Invoke(this, null);
                Trace.TraceInformation("Receiver task exited");
            }, TaskCreationOptions.LongRunning);
        }

        private void Send(MessageType type, byte[] data = null)
        {
            if (!Connected)
            {
                FireError("FAILED TO SEND MESSAGE, PLEASE CHECK YOUR CONNECTION");
                FireInfo("PLEASE USE /connect TO CONNECT");
                return;
            }
            try
            {
                _stream.Write(new DataPackage(type, data));
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException)
                {
                    FireError(ex.InnerException.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private void FireInfo(string message)
        {
            MessageReceived?.Invoke(this, new Message("[Info]", "[System]", message));
        }

        private void FireError(string message)
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

        public void SendMessage(Message message)
        {
            Send(MessageType.CLIENT_MESSAGE, Serializer.SerializeToBytes(message));
        }

        public void SendMessage(string id, string roomName, Bitmap image)
        {
            Send(MessageType.CLIENT_IMAGE_MESSAGE, Serializer.SerializeToBytes(new ImageMessage(id, roomName, Name, image)));
        }


        public void PushFile(FileMessage fileMessage)
        {
            Send(MessageType.CLIENT_PUSH_FILE, Serializer.SerializeToBytes(fileMessage));
            fileMessage.data = null;
        }

        public void PullFile(string fileId)
        {
            Send(MessageType.CLIENT_PULL_FILE, Encoding.UTF8.GetBytes(fileId));
        }

        //public event EventHandler<FileMessage> PullFileProgressChanged;
        //public event EventHandler<FileMessage> PullFileProgressFinished;
        //public event EventHandler<FileMessage> PushFileProgressChanged;
        //public event EventHandler<FileMessage> PushFileProgressFinished;
        public event EventHandler<FileMessage> FileMessageReived;
        public event EventHandler<FileMessage> FileReived;
    }
}
