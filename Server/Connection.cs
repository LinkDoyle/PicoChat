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
        Socket socket;
        readonly NetworkStream networkStream;
        bool closed;
        List<string> joinedRooms = new List<string>();

        public List<string> JoinedRooms => joinedRooms;

        public string ClientName { get; set; }
        public Socket Socket => socket;

        public event EventHandler<string> Login;
        public event EventHandler Logout;
        public event EventHandler<string> JoinRoom;
        public event EventHandler<string> LeaveRoom;
        public event EventHandler ListRooms;
        public event EventHandler<Message> MessageReceived;
        public event EventHandler Closing;
        public event EventHandler Closed;

        public Connection(Socket socket)
        {
            this.socket = socket;
            networkStream = new NetworkStream(socket, false);
        }

        public void Dispose()
        {
            networkStream.Dispose();
            socket.Dispose();
        }

        public void Close()
        {
            closed = true;
            OnClosing();
            Closed?.Invoke(this, new EventArgs());
            socket = null;
        }

        void SendMessage(MessageType messageType, byte[] content)
        {
            networkStream.Write(new DataPackage(messageType, content));
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

        public void SendMessage(IMessage message)
        {
            SendMessage(MessageType.CLIENT_MESSAGE, message);
        }

        string ReceiveMessage()
        {
            IPEndPoint endPoint = (IPEndPoint)socket.RemoteEndPoint;
            byte[] buffer = new byte[1024];
            int read = socket.Receive(buffer, 0, 1024, SocketFlags.None);
            string text = Encoding.UTF8.GetString(buffer, 0, read);
            Console.WriteLine($"Client {endPoint.Address}:{endPoint.Port} " +
                $"sent {read} bytes: {text}");
            return text;
        }

        void OnLogin(byte[] data)
        {
            string name = Encoding.UTF8.GetString(data);
            Login?.Invoke(this, name);
        }

        void OnLogout()
        {
            Logout?.Invoke(this, null);
        }

        void OnMessageReceived(byte[] data)
        {
            Message message = Serializer.DeserializeMessage(Encoding.UTF8.GetString(data));
            MessageReceived?.Invoke(this, message);
        }

        void OnJoinRoom(byte[] data)
        {
            string roomName = Encoding.UTF8.GetString(data);
            JoinRoom?.Invoke(this, roomName);
        }

        void OnLeaveRoom(byte[] data)
        {
            string roomName = Encoding.UTF8.GetString(data);
            LeaveRoom?.Invoke(this, roomName);
        }

        void OnListJoinedRooms()
        {
            ListRooms?.Invoke(this, null);
        }

        void OnClosing()
        {
            Closing?.Invoke(this, null);
        }

        public void Handle()
        {
            IPEndPoint endPoint = (IPEndPoint)socket.RemoteEndPoint;
            try
            {
                do
                {
                    DataPackage package = DataPackage.FromStream(networkStream);
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
                    }

                } while (!closed);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Client {endPoint.Address}:{endPoint.Port} : {ex.Message}");
            }
            catch (EndOfStreamException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                Close();
            }
        }
    }
}
