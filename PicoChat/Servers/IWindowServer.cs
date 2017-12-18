using PicoChat.Common;

namespace PicoChat.Servers
{
    public interface IWindowServer
    {
        void ShowChatWindow();
        void CloseLoginWindow();
        string GetSaveFilePath(string filename);
        void ShowHelpDialog();

        MessageFontInfo GetFontInfo();
        MessageColorInfo GetColorInfo(); 
    }
}
