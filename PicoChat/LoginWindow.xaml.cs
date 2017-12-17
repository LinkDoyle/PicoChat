using System.Net;
using System.Windows;

namespace PicoChat
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginViewModel ViewModel { get; }

        public LoginWindow()
        {
            var client = new Client(IPAddress.Loopback, 23333);
            var windowServer = new WindowServer(client, this);
            ViewModel = new LoginViewModel(windowServer, client);
            InitializeComponent();
        }
    }
}
