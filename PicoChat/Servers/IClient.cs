using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using PicoChat.Common;

namespace PicoChat.Servers
{
    public interface IClient
    {
        string Name { get; }
        bool Connected { get; }
        IPAddress ServerAddress { get; set; }
        int ServerPort { get; set; }
        string CurrentRoomName { get; set; }
        event EventHandler<LoginInfo> LoginOK;
        event EventHandler<LoginInfo> LoginFailed;
        event EventHandler LogoutOK;
        event EventHandler<RoomInfo> LeavedFromRoom;
        event EventHandler<RoomInfo> JoinedInRoom;
        event EventHandler<Message> MessageReceived;
        event EventHandler<ImageMessage> ImageMessageReceived;
        event EventHandler<Receipt> MessageArrivied;
        event EventHandler<Client.ConectionState> StateChaged;
        event EventHandler ReceiverTaskExited;
        event EventHandler<SocketException> SocketExceptionRaising;
        event EventHandler<Client.SystemMessageEventArgs> UnknownMessageReceived;
        event EventHandler<string> SystemMessageReceived;
        void Connect();
        void Disconnect();
        void Login(string name);
        void Logout();
        void Join(string roomName);
        void Leave(string roomName);
        void ListJoinedRooms();
        void SendMessage(Message message);
        void SendMessage(string id, string roomName, Bitmap image);

        void BeginConnect(AsyncCallback requestCallback);
        void EndConnect(IAsyncResult ar);

        Task HandleAsync();

        void PushFile(FileMessage fileMessage);
        //event EventHandler<FileMessage> PushFileProgressChanged;
        //event EventHandler<FileMessage> PushFileProgressFinished;

        void PullFile(string fileID);
        //event EventHandler<FileMessage> PullFileProgressChanged;
        //event EventHandler<FileMessage> PullFileProgressFinished;

        event EventHandler<FileMessage> FileMessageReived;
        event EventHandler<FileMessage> FileReived;
    }
}