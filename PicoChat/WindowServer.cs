using Microsoft.Win32;

namespace PicoChat
{
    public class WindowServer : IWindowServer
    {
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
    }
}
