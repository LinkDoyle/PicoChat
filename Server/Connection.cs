using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PicoChat.Common;

namespace PicoChat
{
    public class Connection : IDisposable
    {
        private readonly NetworkStream _networkStream;
        private bool _closed;

        public Socket Socket { get; private set; }
        public string ClientName { get; set; }
        public List<string> JoinedRooms { get; } = new List<string>();

        public event EventHandler<string> Login;
        public event EventHandler Logout;
        public event EventHandler<string> JoinRoom;
        public event EventHandler<string> LeaveRoom;
        public event EventHandler ListRooms;
        public event EventHandler<Message> MessageReceived;
        public event EventHandler Closed;

        public Connection(Socket socket)
        {
            Socket = socket;
            _networkStream = new NetworkStream(socket, false);
        }

        public void Dispose()
        {
            _networkStream.Dispose();
            Socket.Dispose();
        }

        public void Close()
        {
            _closed = true;
            Closed?.Invoke(this, new EventArgs());
            Socket = null;
        }

        private void SendMessage(MessageType messageType, byte[] content)
        {
            var dataPackage = new DataPackage(messageType, content);
            lock (_networkStream) _networkStream.Write(dataPackage);
        }

        public void SendMessage(MessageType messageType)
        {
            SendMessage(messageType, (byte[])null);
        }

        public void SendMessage(MessageType messageType, string content)
        {
            SendMessage(messageType, Encoding.UTF8.GetBytes(content));
        }

        public void SendMessage(MessageType messageType, IMessage message)
        {
            string content = Serializer.Serialize(message);
            SendMessage(messageType, Encoding.UTF8.GetBytes(content));
        }

        private void OnLogin(byte[] data)
        {
            string name = Encoding.UTF8.GetString(data);
            Login?.Invoke(this, name);
        }

        private void OnLogout()
        {
            Logout?.Invoke(this, null);
        }

        private void OnMessageReceived(byte[] data)
        {
            Message message = Serializer.DeserializeMessage(Encoding.UTF8.GetString(data));
            MessageReceived?.Invoke(this, message);
        }

        private void OnJoinRoom(byte[] data)
        {
            string roomName = Encoding.UTF8.GetString(data);
            JoinRoom?.Invoke(this, roomName);
        }

        private void OnLeaveRoom(byte[] data)
        {
            string roomName = Encoding.UTF8.GetString(data);
            LeaveRoom?.Invoke(this, roomName);
        }

        private void OnListJoinedRooms()
        {
            ListRooms?.Invoke(this, null);
        }

        public void Handle()
        {
            var endPoint = (IPEndPoint)Socket.RemoteEndPoint;
            try
            {
                do
                {
                    DataPackage package = DataPackage.FromStream(_networkStream);
                    Debug.WriteLine($"Client [{endPoint.Address}:{endPoint.Port}] message type: {package.Type.ToString()}");
                    switch (package.Type)
                    {
                        case MessageType.CLIENT_LOGIN:
                            OnLogin(package.Data);
                            break;
                        case MessageType.CLIENT_LOGOUT:
                            OnLogout();
                            break;
                        case MessageType.CLIENT_MESSAGE:
                            OnMessageReceived(package.Data);
                            break;
                        case MessageType.CLIENT_JOIN_ROOM:
                            OnJoinRoom(package.Data);
                            break;
                        case MessageType.CLIENT_LIST_JOINED_ROOMS:
                            OnListJoinedRooms();
                            break;
                        case MessageType.CLIENT_LEAVE_ROOM:
                            OnLeaveRoom(package.Data);
                            break;
                        case MessageType.CLIENT_DISCONNECT:
                            _closed = true;
                            break;
                    }
                } while (!_closed);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Client {endPoint.Address}:{endPoint.Port} : {ex.Message}");
            }
            catch (EndOfStreamException ex)
            {
                Debug.WriteLine($"Client {endPoint.Address}:{endPoint.Port} : {ex.Message}");
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException)
                {
                    Debug.WriteLine($"Client {endPoint.Address}:{endPoint.Port} : {ex.Message}");
                }
                else
                    throw;
            }
            finally
            {
                Close();
            }
        }
    }
}
