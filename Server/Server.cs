using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PicoChat.Common;

namespace PicoChat
{
    public class Room
    {
        private readonly LinkedList<Connection> _connections = new LinkedList<Connection>();
        public string Name { get; }

        public Room(string name)
        {
            Name = name;
        }

        public bool Add(Connection connection)
        {
            lock (_connections)
            {
                if (_connections.Contains(connection)) return false;
                _connections.AddFirst(connection);
                return true;
            }
        }

        public bool Remove(Connection connection)
        {
            lock (_connections)
            {
                if (!_connections.Contains(connection)) return false;
                _connections.Remove(connection);
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendMessage(Message message)
        {
            lock (_connections)
            {
                foreach (var connection in _connections)
                {
                    if (!connection.ClientName.Equals(message.Name))
                    {
                        connection.SendMessage(MessageType.CLIENT_MESSAGE, message);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendImageMessage(ImageMessage message)
        {
            lock (_connections)
            {
                foreach (var connection in _connections)
                {
                    if (!connection.ClientName.Equals(message.Name))
                    {
                        connection.SendMessage(MessageType.CLIENT_IMAGE_MESSAGE, message);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendFileMessage(FileMessage message)
        {
            lock (_connections)
            {
                foreach (var connection in _connections)
                {
                    if (!connection.ClientName.Equals(message.Name))
                    {
                        connection.SendMessage(MessageType.CLIENT_FILE_MESSAGE, message);
                    }
                }
            }
        }
    }

    public sealed class Server
    {
        private long _clientCount;
        private readonly Dictionary<string, Connection> _connections = new Dictionary<string, Connection>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public IPAddress Address { get; }
        public int Port { get; }
        public Dictionary<string, Room> Rooms { get; } = new Dictionary<string, Room>();

        public Server(IPAddress address, int port)
        {
            var dir = new DirectoryInfo("attachments");
            if (!dir.Exists) dir.Create();

            Address = address;
            Port = port;
        }

        public void Start()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Bind(new IPEndPoint(Address, Port));
                socket.Listen(backlog: 1024);
                Console.WriteLine($"Listening on {Address}:{Port}...");

                var tf = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
                tf.StartNew(() =>
                {
                    Console.WriteLine("Listener task started");
                    while (true)
                    {
                        if (_cts.Token.IsCancellationRequested)
                        {
                            _cts.Token.ThrowIfCancellationRequested();
                            break;
                        }
                        Console.WriteLine("Waiting for accept...");
                        Socket client = socket.Accept();
                        if (!client.Connected)
                        {
                            Console.WriteLine("Not connected");
                            continue;
                        }
                        IPEndPoint remoteEndPoint = (IPEndPoint)client.RemoteEndPoint;
                        Console.WriteLine($"Client {remoteEndPoint.Address}:{remoteEndPoint.Port} connected; " +
                            $"Current connection: {Interlocked.Increment(ref _clientCount)}");

                        Task task = CommunicateWithClientUsingSocketAsync(client);
                        task.GetAwaiter().OnCompleted(() =>
                        {
                            Console.WriteLine($"Client {remoteEndPoint.Address}:{remoteEndPoint.Port} disconnected; " +
                                $"Current connection: {Interlocked.Decrement(ref _clientCount)}");
                        });
                    }

                    socket.Dispose();
                    Console.WriteLine("The sever is closed.");
                }, _cts.Token);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to Listening on {Address}:{Port}...");
                Console.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        private Task CommunicateWithClientUsingSocketAsync(Socket socket)
        {
            Connection connection = new Connection(socket);
            IPEndPoint remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            connection.Login += (sender, name) =>
            {
                Connection @this = sender as Connection;
                if (@this == null) return;
                if (@this.ClientName != null)
                {
                    @this.SendMessage(MessageType.SYSTEM_LOGIN_FAILED, $"Already logged as \"{@this.ClientName}\"");
                }
                else
                {
                    lock (_connections)
                    {
                        if (!_connections.ContainsKey(name))
                        {
                            _connections.Add(name, @this);
                            @this.ClientName = name;
                            @this.SendMessage(
                                MessageType.SYSTEM_LOGIN_OK,
                                new LoginInfo(@this.ClientName, $"Hello {@this.ClientName}~"));
                            Console.WriteLine($"Client {remoteEndPoint.Address}:{remoteEndPoint.Port} logged in as \"{@this.ClientName}\".");
                        }
                        else
                        {
                            @this.SendMessage(
                                MessageType.SYSTEM_LOGIN_FAILED,
                                new LoginInfo(@this.ClientName, $"Sorry, the name \"{name}\" is already taken"));
                        }
                    }
                }
            };
            connection.Logout += (sender, name) =>
            {
                Connection @this = sender as Connection;
                if (@this == null) return;
                if (@this.ClientName == null)
                {
                    @this.SendMessage(MessageType.NO_LOGGED);
                    return;
                }
                lock (_connections)
                {
                    if (_connections.ContainsKey(@this.ClientName))
                    {
                        lock (@this.JoinedRooms)
                        {
                            @this.JoinedRooms.ForEach(rname =>
                            {
                                if (Rooms.TryGetValue(rname, out Room room))
                                {
                                    room.Remove(@this);
                                }
                            });
                        }

                        @this.SendMessage(MessageType.SYSTEM_OK, $"Logged out");
                        Console.WriteLine($"\"{@this.ClientName}\" [{remoteEndPoint.Address}:{remoteEndPoint.Port}] logged out.");

                        _connections.Remove(@this.ClientName);
                        @this.ClientName = null;
                    }
                    else
                    {
                        @this.SendMessage(MessageType.NO_LOGGED);
                    }
                }

            };
            connection.JoinRoom += (sender, roomName) =>
            {
                Connection @this = sender as Connection;
                if (@this == null) return;
                if (@this.ClientName == null)
                {
                    @this.SendMessage(MessageType.NO_LOGGED, "Please logged in first.");
                    return;
                }
                lock (Rooms)
                {
                    if (!Rooms.ContainsKey(roomName))
                    {
                        Rooms.Add(roomName, new Room(roomName));
                        Console.WriteLine($"Room[{roomName}] created.");
                    }
                    Room room = Rooms[roomName];
                    if (room.Add(@this))
                    {
                        lock (@this.JoinedRooms) @this.JoinedRooms.Add(room.Name);
                        @this.SendMessage(MessageType.SYSTEM_JOIN_ROOM_OK, new RoomInfo(room.Name));
                        Console.WriteLine($"{@this.ClientName} [{remoteEndPoint.Address}:{remoteEndPoint.Port}] joined the room[{room.Name}].");
                    }
                    else
                    {
                        @this.SendMessage(MessageType.ALREADY_JOINNED, $"Already joined the room[{room.Name}]");
                    }
                }
            };
            connection.LeaveRoom += (sender, roomName) =>
            {
                Connection @this = sender as Connection;
                if (@this == null) return;
                if (@this.ClientName == null)
                {
                    @this.SendMessage(MessageType.NO_LOGGED, "Please logged in first.");
                    return;
                }

                lock (Rooms)
                {
                    if (Rooms.ContainsKey(roomName))
                    {
                        Room room = Rooms[roomName];
                        room.Remove(@this);
                        lock (@this.JoinedRooms) @this.JoinedRooms.Remove(room.Name);

                        IPEndPoint endPoint = (IPEndPoint)@this.Socket.RemoteEndPoint;
                        Console.WriteLine($"{@this.ClientName} [{endPoint.Address}:{endPoint.Port}] left the room [{room.Name}]");
                        @this.SendMessage(MessageType.SYSTEM_LEAVE_ROOM_OK, new RoomInfo(room.Name));
                        Console.WriteLine($"\"{@this.ClientName}\" [{remoteEndPoint.Address}:{remoteEndPoint.Port}] left the room[{room.Name}].");
                    }
                    else
                    {
                        @this.SendMessage(MessageType.SYSTEM_ERROR, $"No joinned the room[{roomName}]");
                    }
                }
            };
            connection.ListRooms += (sender, e) =>
            {
                Connection @this = sender as Connection;
                if (@this == null) return;
                if (@this.ClientName == null)
                {
                    @this.SendMessage(MessageType.NO_LOGGED, "Please logged in first.");
                    return;
                }
                StringBuilder stringBuilder = new StringBuilder();
                lock (@this.JoinedRooms)
                {
                    @this.JoinedRooms.ForEach(r =>
                    {
                        stringBuilder.Append(r);
                        stringBuilder.Append('\n');
                    });
                }
                @this.SendMessage(MessageType.CLIENT_LIST_JOINED_ROOMS, stringBuilder.ToString());
            };
            connection.MessageReceived += (sender, message) =>
            {
                Connection @this = sender as Connection;
                if (@this == null) return;
                if (message == null)
                {
                    @this.SendMessage(MessageType.SYSTEM_ERROR);
                }
                else if (@this.ClientName == null || !@this.ClientName.Equals(message.Name))
                {
                    @this.SendMessage(MessageType.NO_LOGGED, "Please logged in first.");
                }
                else if (string.IsNullOrEmpty(message.Room))
                {
                    @this.SendMessage(MessageType.SYSTEM_UNJOIN_ROOM, "Please joinned in a room first.");
                }
                else if (!@this.JoinedRooms.Contains(message.Room))
                {
                    @this.SendMessage(MessageType.SYSTEM_UNJOIN_ROOM, $"Please joinned in the room {message.Room} first.");
                }
                else if (Rooms.TryGetValue(message.Room, out Room room))
                {
                    room.SendMessage(message);
                    @this.SendMessage(MessageType.SYSTEM_MESSAGE_OK, new Receipt(message.ID));
                }
                else
                {
                    @this.SendMessage(MessageType.SYSTEM_UNJOIN_ROOM, "Please joinned in a room first.");
                }
            };
            connection.ImageMessageReceived += (sender, imageMessage) =>
            {
                Connection @this = sender as Connection;
                if (@this == null) return;
                if (imageMessage == null)
                {
                    @this.SendMessage(MessageType.SYSTEM_ERROR);
                }
                else if (@this.ClientName == null || !@this.ClientName.Equals(imageMessage.Name))
                {
                    @this.SendMessage(MessageType.NO_LOGGED, "Please logged in first.");
                }
                else if (string.IsNullOrEmpty(imageMessage.Room))
                {
                    @this.SendMessage(MessageType.SYSTEM_UNJOIN_ROOM, "Please joinned in a room first.");
                }
                else if (!@this.JoinedRooms.Contains(imageMessage.Room))
                {
                    @this.SendMessage(MessageType.SYSTEM_UNJOIN_ROOM, $"Please joinned in the room {imageMessage.Room} first.");
                }
                else if (Rooms.TryGetValue(imageMessage.Room, out Room room))
                {
                    room.SendImageMessage(imageMessage);
                    @this.SendMessage(MessageType.SYSTEM_MESSAGE_OK, new Receipt(imageMessage.ID));
                }
                else
                {
                    @this.SendMessage(MessageType.SYSTEM_UNJOIN_ROOM, "Please joinned in a room first.");
                }
            };
            connection.Closed += (sender, e) =>
            {
                Connection @this = sender as Connection;
                if (@this == null) return;
                lock (Rooms)
                {
                    foreach (string name in @this.JoinedRooms)
                    {
                        if (Rooms.ContainsKey(name))
                        {
                            Room room = Rooms[name];
                            room.Remove(@this);
                        }
                    }
                }
                if (@this.ClientName != null)
                {
                    lock (_connections)
                    {
                        _connections.Remove(@this.ClientName);
                    }
                }
                IPEndPoint endPoint = (IPEndPoint)@this.Socket.RemoteEndPoint;
                Debug.WriteLine($"Client [{endPoint.Address}:{endPoint.Port}] closed");
                connection.Dispose();
            };

            connection.ClientPushFile += Connection_ClientPushFile;

            connection.ClientPullFile += Connection_ClientPullFile;

            return Task.Run(() => connection.Handle());
        }

        private readonly Dictionary<string, string> _filenames = new Dictionary<string, string>();
        private void Connection_ClientPushFile(object sender, byte[] data)
        {
            var @this = sender as Connection;
            var message = Serializer.Deserialize<FileMessage>(data);
            if (@this == null) return;
            if (message == null)
            {
                @this.SendMessage(MessageType.SYSTEM_ERROR);
            }
            else if (@this.ClientName == null || !@this.ClientName.Equals(message.Name))
            {
                @this.SendMessage(MessageType.NO_LOGGED, "Please logged in first.");
            }
            else if (string.IsNullOrEmpty(message.Room))
            {
                @this.SendMessage(MessageType.SYSTEM_UNJOIN_ROOM, "Please joinned in a room first.");
            }
            else if (!@this.JoinedRooms.Contains(message.Room))
            {
                @this.SendMessage(MessageType.SYSTEM_UNJOIN_ROOM, $"Please joinned in the room {message.Room} first.");
            }
            else if (Rooms.TryGetValue(message.Room, out Room room))
            {
                using (var fileStream = new FileStream("attachments/" + message.FileId, FileMode.Create))
                {
                    fileStream.Write(message.data, 0, message.data.Length);
                }
                message.data = null;
                lock (_filenames)
                {
                    _filenames.Add(message.FileId, message.FileName);
                }
                room.SendFileMessage(message);
                @this.SendMessage(MessageType.SYSTEM_MESSAGE_OK, new Receipt(message.ID));

                IPEndPoint endPoint = (IPEndPoint)@this.Socket.RemoteEndPoint;
                Console.WriteLine($"Client [{endPoint.Address}:{endPoint.Port}]({@this.ClientName}) push file {message.FileName}({message.FileId}).");
            }
            else
            {
                @this.SendMessage(MessageType.SYSTEM_UNJOIN_ROOM, "Please joinned in a room first.");
            }
        }

        private void Connection_ClientPullFile(object sender, byte[] data)
        {
            var @this = sender as Connection;
            if (@this == null) return;
            var fileId = Encoding.UTF8.GetString(data);
            lock (_filenames)
            {
                if (_filenames.TryGetValue(fileId, out string fileName))
                {
                    IPEndPoint endPoint = (IPEndPoint)@this.Socket.RemoteEndPoint;
                    Console.WriteLine($"Client [{endPoint.Address}:{endPoint.Port}]({@this.ClientName}) pull file {fileName}({fileId}).");

                    FileInfo fileInfo = new FileInfo("attachments/" + fileId);
                    if (!fileInfo.Exists)
                    {
                        //FIXME
                        return;
                    }

                    byte[] fileBytes = File.ReadAllBytes(fileInfo.FullName);
                    var fileMessage = new FileMessage(Utility.GenerateID(), null, null, fileId, fileName, fileInfo.Length)
                    {
                        data = fileBytes
                    };
                    @this.SendMessage(MessageType.SYSTEM_FILE_TRANSFER, fileMessage);
                }
            }
        }
    }
}
