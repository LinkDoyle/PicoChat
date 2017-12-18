using PicoChat.Common;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

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

        public MessageFontInfo GetFontInfo()
        {
            var fd = new FontDialog();
            var result = fd.ShowDialog();
            MessageFontInfo fontInfo = null;
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var tdc = new TextDecorationCollection();
                if (fd.Font.Underline) tdc.Add(TextDecorations.Underline);
                if (fd.Font.Strikeout) tdc.Add(TextDecorations.Strikethrough);
                fontInfo = new MessageFontInfo(fd.Font.Name, fd.Font.Size * 96.0 / 72.0)
                {
                    FontWeight = fd.Font.Bold ? FontWeights.Bold : FontWeights.Regular,
                    FontStyle = fd.Font.Italic ? FontStyles.Italic : FontStyles.Normal,
                    //TextDecorations = tdc
                };
            }
            return fontInfo;
        }

        public MessageColorInfo GetColorInfo()
        {
            var cd = new ColorDialog();
            var result = cd.ShowDialog();
            MessageColorInfo colorInfo = null;
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                colorInfo = new MessageColorInfo(ColorTranslator.ToHtml(cd.Color));
            }
            return colorInfo;
        }
    }
}
