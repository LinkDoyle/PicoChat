using PicoChat.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace PicoChat
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string AppName = "PicoChat";
        private readonly Client _client = new Client(IPAddress.Loopback, 23333);
        private readonly ObservableCollection<ChatMessage> _messages = new ObservableCollection<ChatMessage>();
        private readonly ObservableCollection<ChatMessage> _messagesWaitToConfirm = new ObservableCollection<ChatMessage>();
        private readonly ObservableCollection<string> _joinedRooms = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();

            JoinedRoomList.ItemsSource = _joinedRooms;

            SendButton.Click += (sender, e) => OnSendMessage();
            MessageToSendBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter) OnSendMessage();
            };

            MessageListView.ItemsSource = _messages;
            _messages.CollectionChanged += (sender, e) =>
            {
                Title = $"{AppName} - {_client.Name} - [{_client.CurrentRoomName}] - {_messages.Count}";
                MessageListView.ScrollIntoView(e.NewItems[e.NewItems.Count - 1]);
            };

            var messageCollectionView = CollectionViewSource.GetDefaultView(MessageListView.ItemsSource) as CollectionView;
            Debug.Assert(messageCollectionView != null, nameof(messageCollectionView) + " != null");
            messageCollectionView.Filter = (item) =>
            {
                var message = (Message)item;
                string roomName = message.Room;
                if (roomName == null || roomName.Equals("[System]")) return true;
                var selectedItem = JoinedRoomList.SelectedItem;
                return roomName.Equals(selectedItem);
            };

            FireInfo("Hello~");
            FireInfo("Use /? for help.");
        }

        private void FireInfo(string message)
        {
            _messages.Add(new ChatMessage("[Info]", "[System]", message));
        }

        private void FireInfo(string tag, string message)
        {
            _messages.Add(new ChatMessage(tag, "[System]", message));
        }

        private void FireError(string message)
        {
            _messages.Add(new ChatMessage("[Error]", "[System]", message));
        }

        private void OnSendMessage()
        {
            if (!MessageToSendBox.Text.StartsWith("/", StringComparison.Ordinal))
            {
                if (_client.Connected)
                {
                    var id = _client.GenerateID();
                    var message = new ChatMessage("<this>", _client.CurrentRoomName, MessageToSendBox.Text)
                    {
                        ID = id
                    };
                    _messages.Add(message);
                    _messagesWaitToConfirm.Add(message);
                    _client.SendMessage(id, _client.CurrentRoomName, MessageToSendBox.Text);
                }
                else
                {
                    FireError("FAILED TO SEND MESSAGE, PLEASE CHECK YOUR CONNECTION");
                    FireInfo("PLEASE USE /connect TO CONNECT");
                }
            }
            else
            {
                string[] argv = MessageToSendBox.Text.Split(' ');
                string command = argv[0];
                switch (command)
                {
                    case "/?":
                        FireInfo("[HELP]",
                        @"
    /connect [IP] [PORT] connect to server
    /disconnect          disconnect
    /login [NAME]        login with [name]
    /logout              logout
    /join  [NAME]        join [room]
    /leave [NAME]        leave [room]
    /list                list the room you joined
    [message]            send message to current room
                        ");
                        break;
                    case "/connect":
                        if (!_client.Connected)
                        {
                            if (argv.Length != 3)
                            {
                                FireError($"Invalid Argument {MessageToSendBox.Text}");
                                break;
                            }
                            if (!IPAddress.TryParse(argv[1], out IPAddress address))
                            {
                                FireError($"Invalid IP address {address}");
                                break;
                            }
                            _client.ServerAddress = address;
                            if (!int.TryParse(argv[2], out int port))
                            {
                                FireError($"Invalid port {port}");
                                break;
                            }
                            _client.ServerPort = port;
                            FireInfo($"CONNECTING to {_client.ServerAddress}:{_client.ServerPort}...");
                            _client.Connect();
                            Task _ = _client.HandleAsync();
                        }
                        else
                        {
                            FireError($"The client has connected to {_client.ServerAddress}:{_client.ServerPort}.");
                        }
                        break;
                    case "/disconnect":
                        if (_client.Connected)
                        {
                            _client.Disconnect();
                            FireInfo("Disconnecting...");
                        }
                        else
                        {
                            FireError("The client isn't connected.");
                        }
                        break;
                    case "/login":
                        if (argv.Length != 2)
                        {
                            FireError($"Invalid Command {MessageToSendBox.Text}");
                            break;
                        }
                        _client.Login(argv[1]);
                        break;
                    case "/logout":
                        _client.Logout();
                        break;
                    case "/join":
                        if (argv.Length != 2)
                        {
                            FireError($"Invalid Command {MessageToSendBox.Text}");
                            break;
                        }
                        _client.Join(argv[1]);
                        break;
                    case "/leave":
                        if (argv.Length != 2)
                        {
                            FireError($"Invalid Command {MessageToSendBox.Text}");
                            break;
                        }
                        _client.CurrentRoomName = "";
                        _client.Leave(argv[1]);
                        break;
                    case "/list":
                        _client.ListJoinedRooms();
                        break;
                    default:
                        FireError($"Invalid Command {MessageToSendBox.Text}");
                        break;
                }
            }
            MessageToSendBox.Text = "";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _client.SystemMessageReceived += Client_SystemMessageReceived;
            _client.LoginOK += Client_LoginOK;
            _client.LoginFailed += Client_LoginFailed;
            _client.LogoutOK += Client_LogoutOK;
            _client.JoinedInRoom += Client_JoinedInRoom;
            _client.LeavedFromRoom += Client_LeavedFromRoom;
            _client.MessageReceived += Client_MessageReceived;
            _client.MessageArrivied += Client_MessageArrivied;
            _client.StateChaged += Client_StateChaged;
            _client.SocketExceptionRaising += Client_SocketExceptionRaising;
            _client.ReceiverTaskExited += (_, __) => { _client.Disconnect(); };
        }

        private void JoinedRoomList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (JoinedRoomList.SelectedIndex != -1)
            {
                _client.CurrentRoomName = JoinedRoomList.SelectedItem.ToString();
                Title = $"{AppName} - {_client.Name} - [{_client.CurrentRoomName}] - {_messages.Count}";
            }
            else
            {
                _client.CurrentRoomName = null;
                Title = $"{AppName} - {_client.Name} - [{_client.CurrentRoomName}] - {_messages.Count}";
            }
            CollectionViewSource.GetDefaultView(MessageListView.ItemsSource).Refresh();
        }

        private void Client_LoginOK(object sender, LoginInfo e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Title = $"{AppName} - {_client.Name} - [{_client.CurrentRoomName}] - {_messages.Count}";
                FireInfo(e.Content);
            }));
        }

        private void Client_LoginFailed(object sender, LoginInfo e)
        {
            Dispatcher.BeginInvoke(new Action(() => FireError(e.Content)));
        }

        private void Client_LogoutOK(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Title = $"{AppName} - - [{_client.CurrentRoomName}] - {_messages.Count}";
            }));
        }

        private void Client_JoinedInRoom(object sender, RoomInfo e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _joinedRooms.Add(e.Name);
                JoinedRoomList.SelectedIndex = _joinedRooms.Count - 1;
            }));
        }

        private void Client_LeavedFromRoom(object sender, RoomInfo e)
        {
            Dispatcher.BeginInvoke(new Action(() => _joinedRooms.Remove(e.Name)));
        }

        private void Client_SystemMessageReceived(object sender, Client.SystemMessageEventArgs e)
        {
            string content = e.Data != null ? Encoding.UTF8.GetString(e.Data) : "";
            Dispatcher.BeginInvoke(new Action(() => FireInfo($"[{e.Type}]", content)));
        }

        private void Client_SocketExceptionRaising(object sender, SocketException e)
        {
            Dispatcher.BeginInvoke(new Action(() => FireInfo("[SocketException]", e.Message)));
        }

        private void Client_MessageReceived(object sender, Message e)
        {
            Dispatcher.BeginInvoke(new Action(() => _messages.Add(new ChatMessage(e))));
        }

        private void Client_MessageArrivied(object o, Receipt receipt)
        {
            //            SynchronizationContext.Current.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < _messagesWaitToConfirm.Count; ++i)
                {
                    if (_messagesWaitToConfirm[i].ID == receipt.ID)
                    {
                        _messagesWaitToConfirm[i].HasReceipt = true;
                        _messagesWaitToConfirm.RemoveAt(i);
                    }
                }
                CollectionViewSource.GetDefaultView(_messages).Refresh();
            }));
        }

        private void Client_StateChaged(object sender, Client.ConectionState e)
        {
            Dispatcher.BeginInvoke(new Action(() => FireInfo("[StateChaged]", e.ToString())));
        }
    }

    public sealed class ChatMessage : Message, INotifyPropertyChanged
    {
        private bool _hasReceipt;
        public bool HasReceipt
        {
            get => _hasReceipt;
            set
            {
                _hasReceipt = value;
                OnPropertyChanged();
            }
        }

        public ChatMessage(Message message) : base(message.ID, message.Name, message.Room, message.Content) { }
        public ChatMessage(string name, string room, string content) : base(name, room, content) { }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
