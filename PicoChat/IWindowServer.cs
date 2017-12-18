namespace PicoChat
{
    public interface IWindowServer
    {
        void ShowChatWindow();
        void CloseLoginWindow();
        string GetSaveFilePath(string filename);
    }
}
