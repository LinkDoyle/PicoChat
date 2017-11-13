using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;
using PicoChat.Common;

namespace PicoChat
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const string APP_NAME = "PicoChat";
        Client client = new Client(IPAddress.Loopback, 23333);
        ObservableCollection<Message> messages = new ObservableCollection<Message>();
        ObservableCollection<string> joinedRooms = new ObservableCollection<string>();
        public MainWindow()
        {
            InitializeComponent();

            JoinedRoomList.ItemsSource = joinedRooms;

            sendButton.Click += (sender, e) => OnSendMessage();
            messageToSendBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter) OnSendMessage();
            };

            messageListView.ItemsSource = messages;
            messages.CollectionChanged += (sender, e) =>
            {
                messageListView.ScrollIntoView(e.NewItems[e.NewItems.Count - 1]);
            };
            FireInfo("Hello~");
            FireInfo("Use /? for help.");
        }

        void FireInfo(string message)
        {
            messages.Add(new Message("[Info]", "[System]", message));
        }

        void FireInfo(string tag, string message)
        {
            messages.Add(new Message(tag, "[System]", message));
        }

        void FireError(string message)
        {
            messages.Add(new Message("[Error]", "[System]", message));
        }

        void OnSendMessage()
        {
            if (!messageToSendBox.Text.StartsWith("/", StringComparison.Ordinal))
            {
                if (client.Connected)
                {
                    messages.Add(new Message("<this>", client.CurrentRoomName, messageToSendBox.Text));
                    client.SendMessage(client.CurrentRoomName, messageToSendBox.Text);
                }
                else
                {
                    FireError("FAILED TO SEND MESSAGE, PLEASE CHECK YOUR CONNECTION");
                    FireInfo("PLEASE USE /connect TO CONNECT");
                }
            }
            else
            {
                string[] argv = messageToSendBox.Text.Split(' ');
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
                        if (!client.Connected)
                        {
                            if (argv.Length != 3)
                            {
                                FireError($"Invalid Argument {messageToSendBox.Text}");
                                break;
                            }
                            if (!IPAddress.TryParse(argv[1], out IPAddress address))
                            {
                                FireError($"Invalid IP address {address}");
                                break;
                            }
                            client.ServerAddress = address;
                            if (!int.TryParse(argv[2], out int port))
                            {
                                FireError($"Invalid port {port}");
                                break;
                            }
                            client.ServerPort = port;
                            FireInfo($"CONNECTING to {client.ServerAddress}:{client.ServerPort}...");
                            client.Connect();
                        }
                        else
                        {
                            FireError($"The client has connected to {client.ServerAddress}:{client.ServerPort}.");
                        }
                        break;
                    case "/disconnect":
                        if (!client.Connected)
                            client.Disconnect();
                        else
                            FireError($"The client isn't connected.");
                        break;
                    case "/login":
                        if (argv.Length != 2)
                        {
                            FireError($"Invalid Command {messageToSendBox.Text}");
                            break;
                        }
                        client.Login(argv[1]);
                        break;
                    case "/logout":
                        client.Logout();
                        break;
                    case "/join":
                        if (argv.Length != 2)
                        {
                            FireError($"Invalid Command {messageToSendBox.Text}");
                            break;
                        }
                        client.Join(argv[1]);
                        break;
                    case "/leave":
                        if (argv.Length != 2)
                        {
                            FireError($"Invalid Command {messageToSendBox.Text}");
                            break;
                        }
                        client.CurrentRoomName = "";
                        client.Leave(argv[1]);
                        break;
                    case "/list":
                        client.ListJoinedRooms();
                        break;
                    default:
                        FireError($"Invalid Command {messageToSendBox.Text}");
                        break;
                }
            }
            messageToSendBox.Text = "";
        }

        void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnSendMessage();
            }
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            OnSendMessage();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            client.SystemMessageReceived += Client_SystemMessageReceived;
            client.JoinedInRoom += Client_JoinedInRoom;
            client.LeavedFromRoom += Client_LeavedFromRoom;
            client.MessageReceived += Client_MessageReceived;
            client.StateChaged += Client_StateChaged;
            client.SocketExceptionRaising += Client_SocketExceptionRaising;
        }

        void JoinedRoomList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (JoinedRoomList.SelectedIndex != -1)
            {
                client.CurrentRoomName = JoinedRoomList.SelectedItem.ToString();
                Title = $"{APP_NAME} - [{client.CurrentRoomName}]";
            }
            else
            {
                client.CurrentRoomName = null;
                Title = $"{APP_NAME}";
            }

        }

        void Client_JoinedInRoom(object sender, RoomInfo e)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                joinedRooms.Add(e.Name);
                JoinedRoomList.SelectedIndex = joinedRooms.Count - 1;
            }));
        }

        void Client_LeavedFromRoom(object sender, RoomInfo e)
        {
            Dispatcher.BeginInvoke(new Action(() => joinedRooms.Remove(e.Name)));
        }

        void Client_SystemMessageReceived(object sender, Client.SystemMessageEventArgs e)
        {
            Client @this = sender as Client;
            string content = e.Data != null ? Encoding.UTF8.GetString(e.Data) : "";
            Dispatcher.BeginInvoke(new Action(() => FireInfo($"[{e.Type}]", content)));
        }

        void Client_SocketExceptionRaising(object sender, SocketException e)
        {
            Dispatcher.BeginInvoke(new Action(() => FireInfo("[SocketException]", e.Message)));
        }

        void Client_MessageReceived(object sender, Message e)
        {
            Dispatcher.BeginInvoke(new Action(() => messages.Add(e)));
        }

        void Client_StateChaged(object sender, Client.ConectionState e)
        {
            Dispatcher.BeginInvoke(new Action(() => FireInfo("[StateChaged]", e.ToString())));
        }
    }
}
