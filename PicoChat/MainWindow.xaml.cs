using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PicoChat.Common;

namespace PicoChat
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        string currentRoomName;
        Client client = new Client(IPAddress.Loopback, 23333);

        public MainWindow()
        {
            InitializeComponent();
            sendButton.Click += (sender, e) => OnSendMessage();
            messageToSendBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter) OnSendMessage();
            };
        }

        void OnSendMessage()
        {
            if (!messageToSendBox.Text.StartsWith("/", StringComparison.Ordinal))
            {
                if (client.Connected)
                {
                    messageBlock.Text += $"[Sent] {messageToSendBox.Text}\n";
                    client.SendMessage(currentRoomName, messageToSendBox.Text);
                }
                else
                {
                    messageBlock.Text += $"[SERVER] FAILED TO SEND MESSAGE, PLEASE CHECK YOUR CONNECTION\n";
                    messageBlock.Text += $"[SERVER] USE /connect TO CONNECT\n";
                }
            }
            else
            {
                string[] argv = messageToSendBox.Text.Split(' ');
                string command = argv[0];
                switch (command)
                {
                    case "/connect":
                        if (!client.Connected)
                        {
                            messageBlock.Text += $"[SERVER] CONNECTING...\n";
                            client.Connect();
                        }
                        break;
                    case "/login":
                        if (argv.Length != 2)
                        {
                            messageBlock.Text += $"[SYSTEM] Invalid Command {command}\n";
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
                            messageBlock.Text += $"[SYSTEM] Invalid Command {command}\n";
                            break;
                        }
                        currentRoomName = argv[1];
                        client.Join(argv[1]);
                        break;
                    case "/leave":
                        if (argv.Length != 2)
                        {
                            messageBlock.Text += $"[SYSTEM] Invalid Command {command}\n";
                            break;
                        }
                        client.Leave(argv[1]);
                        break;
                    case "/ping":
                        client.Ping();
                        break;
                    default:
                        messageBlock.Text += $"[SYSTEM] Invalid Command {command}\n";
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
            client.MessageReceived += Client_MessageReceived;
            client.StateChaged += Client_StateChaged;
            client.SocketExceptionRaising += Client_SocketExceptionRaising;
            client.Connect();
        }

        void Client_SystemMessageReceived(object sender, Client.SystemMessageEventArgs e)
        {
            Client @this = sender as Client;
            string content = e.Data != null ? Encoding.UTF8.GetString(e.Data) : "";
            Dispatcher.BeginInvoke(new Action(() =>
            {
                messageBlock.Text += $"[SERVER] {e.Type} {content}\n";
            }));
        }

        void Client_SocketExceptionRaising(object sender, SocketException e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                messageBlock.Text += $"[SERVER] {e.Message}\n";
            }));
        }

        void Client_MessageReceived(object sender, Message e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                messageBlock.Text += $"[RECV] {e}\n";
            }));
        }

        void Client_StateChaged(object sender, Client.ConectionState e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                messageBlock.Text += $"[SERVER] {e}\n";
            }));
        }
    }
}
