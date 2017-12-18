using System.Windows;
using Microsoft.Win32;

namespace PicoChat
{
    public class WindowServer : IWindowServer
    {
        private const string HelpMessageText = @"
    /connect [IP] [PORT] - connect to server
    /disconnect - disconnect
    /login [NAME] - login with [name]
    /logout - logout
    /join  [NAME] - join [room]
    /leave [NAME] - leave [room]
    /list - list the room you joined
    [message] - send message to current room";

        private const string AppName = "PicoChat";
        private readonly IClient _client;
        private readonly LoginWindow _loginWindow;
        private ChatWindow _chatWindow;
        public WindowServer(IClient client, LoginWindow loginWindow)
        {
            _client = client;
            _loginWindow = loginWindow;
        }
        public void ShowChatWindow()
        {
            if (_chatWindow == null)
            {
                _chatWindow = new ChatWindow(this, _client);
            }
            _chatWindow.Show();
        }

        public void CloseLoginWindow()
        {
            _loginWindow.Close();
        }

        public string GetSaveFilePath(string filename)
        {
            var dialog = new SaveFileDialog
            {
                FileName = filename,
                Filter = "All Files (*.*) | *.*"
            };
            return dialog.ShowDialog() == null ? null : dialog.FileName;
        }

        public void ShowHelpDialog()
        {
            MessageBox.Show(HelpMessageText, AppName);
        }
    }
}
