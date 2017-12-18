using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using PicoChat.Common;
using PicoChat.Servers;

namespace PicoChat.ViewModels
{
    public class LoginViewModel : BindableBase
    {
        private static Dispatcher Dispatcher => Application.Current.Dispatcher;
        private readonly IClient _client;
        private readonly IWindowServer _windowServer;

        bool _isLogining;
        public bool IsLogging
        {
            get => _isLogining;
            set => SetProperty(ref _isLogining, value);
        }

        private IPAddress _serverAddress;
        public IPAddress ServerAddress
        {
            get => _serverAddress;
            set => SetProperty(ref _serverAddress, value);
        }

        private int _serverPort;
        public int ServerPort
        {
            get => _serverPort;
            set => SetProperty(ref _serverPort, value);
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _loginMessage;

        public string LoginMessage
        {
            get => _loginMessage;
            set => SetProperty(ref _loginMessage, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(IWindowServer windowServer, IClient client)
        {
            LoginMessage = "Please Login";

            _windowServer = windowServer;

            _client = client;
            _client.LoginOK += Client_LoginOK;
            _client.LoginFailed += Client_LoginFailed;

            _serverAddress = client.ServerAddress;
            _serverPort = client.ServerPort;

            LoginCommand = new DelegateCommand(OnLogin, () => !_isLogining);
        }

        private void OnLogin(object obj)
        {
            IsLogging = true;
            LoginMessage = $"Connecting to {_client.ServerAddress}:{_client.ServerPort}...";
            if (!_client.Connected)
            {
                _client.BeginConnect(ConnectCallback);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                _client.EndConnect(ar);
                _client.HandleAsync();
                LoginMessage = "Please wait...";
                _client.Login(Username);
            }
            catch (SocketException ex)
            {
                LoginMessage = $"Failed to connect: {ex.Message}";
                Debug.WriteLine(ex.StackTrace);
                IsLogging = false;
            }
        }


        private void Client_LoginOK(object sender, LoginInfo e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoginMessage = $"{e.Content}";
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    Dispatcher.Invoke(() =>
                    {
                        _windowServer.ShowChatWindow();
                        _windowServer.CloseLoginWindow();
                        IsLogging = false;
                    });
                });
            }));
        }

        private void Client_LoginFailed(object sender, LoginInfo e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoginMessage = $"Failed to login as {e.Name}:\n {e.Content}";
                IsLogging = false;
            }));
        }

    }
}
