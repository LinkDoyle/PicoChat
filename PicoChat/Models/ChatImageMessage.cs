using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PicoChat.Common;

namespace PicoChat
{
    public class ChatImageMessage : ChatMessage
    {
        public ImageSource ImageSource { get; private set; }
        public ChatImageMessage(string id, string name, string room, ImageSource image) : base(id, DateTime.Now, name, room)
        {
            ImageSource = image;
        }

        public ChatImageMessage(ImageMessage message) : base(message.ID, message.UtcTime, message.Name, message.Room)
        {
            using (var memory = new MemoryStream())
            {
                if (message.Image == null) return;
                message.Image.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                ImageSource = bitmapImage;
            }
        }
    }
}