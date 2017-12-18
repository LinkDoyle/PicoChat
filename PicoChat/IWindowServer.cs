using PicoChat.Common;

namespace PicoChat
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
