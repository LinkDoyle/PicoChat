using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Serialization;
using System.Windows;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Windows.FontStyle;

namespace PicoChat.Common
{
    public enum MessageType : Int16
    {
        SYSTEM_OK,
        SYSTEM_ERROR,
        SYSTEM_UNKNOWN,

        SYSTEM_LOGIN_OK,
        SYSTEM_LOGIN_FAILED,
        SYSTEM_UNJOIN_ROOM,
        SYSTEM_JOIN_ROOM_OK,
        SYSTEM_LEAVE_ROOM_OK,
        SYSTEM_MESSAGE_OK,

        CLIENT_LOGIN,
        CLIENT_LOGOUT,
        CLIENT_JOIN_ROOM,
        CLIENT_LEAVE_ROOM,
        CLIENT_LIST_JOINED_ROOMS,
        CLIENT_MESSAGE,
        CLIENT_IMAGE_MESSAGE,
        CLIENT_DISCONNECT,
        NO_LOGGED,
        ALREADY_JOINNED,

        CLIENT_PUSH_FILE,
        CLIENT_PULL_FILE,
        CLIENT_FILE_MESSAGE,
        SYSTEM_FILE_TRANSFER,
        SYSTEM_MESSAGE
    }


    [XmlRoot]
    public interface IMessage
    {
    }

    public class LoginInfo : IMessage
    {
        public LoginInfo() : this(null, null) {}

        public LoginInfo(string name, string content)
        {
            Name = name;
            Content = content;
        }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlElement]
        public string Content { get; set; }
    }

    public class RoomInfo : IMessage, IEquatable<RoomInfo>
    {
        public RoomInfo() : this(null)
        {
        }

        public RoomInfo(string name)
        {
            Name = name;
        }

        [XmlAttribute]
        public string Name { get; set; }

        public override string ToString()
        {
            return $"name: {Name}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RoomInfo);
        }

        public bool Equals(RoomInfo other)
        {
            return other != null &&
                   Name == other.Name;
        }

        public override int GetHashCode()
        {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
        }

        public static bool operator ==(RoomInfo info1, RoomInfo info2)
        {
            return EqualityComparer<RoomInfo>.Default.Equals(info1, info2);
        }

        public static bool operator !=(RoomInfo info1, RoomInfo info2)
        {
            return !(info1 == info2);
        }
    }

    public abstract class MessageBase : IMessage
    {
        [XmlAttribute]
        public string ID { get; set; }
        [XmlAttribute]
        public DateTime UtcTime { get; set; }
        [XmlAttribute]
        public string Room { get; set; }
        [XmlAttribute]
        public string Name { get; set; }

        protected MessageBase()
        {
        }

        protected MessageBase(string id, string room, string name)
        {
            ID = id;
            UtcTime = DateTime.Now;
            Name = name;
            Room = room;
        }
    }

    public class MessageFontInfo
    {
        public string FontFamily { get; set; }
        public double FontSize { get; set; }
        public FontWeight FontWeight { get; set; } = FontWeights.Regular;
        public FontStyle FontStyle { get; set; } = FontStyles.Normal;
        //public TextDecorationCollection TextDecorations { get; set; } = new TextDecorationCollection();

        public MessageFontInfo(string fontFamily, double fontSize)
        {
            FontFamily = fontFamily;
            FontSize = fontSize;
        }

        public MessageFontInfo() : this("Consolas", 14) {}
    }

    public class MessageColorInfo
    {
        public string Foreground { get; set; }

        public MessageColorInfo(string foreground)
        {
            Foreground = foreground;
        }

        public MessageColorInfo() : this("#FF000000") { }
    }

    public class Message : MessageBase
    {
        [XmlElement]
        public MessageColorInfo ColorInfo { get; set; }
        [XmlElement]
        public MessageFontInfo FontInfo { get; set; }

        [XmlElement]
        public string Content { get; set; }

        public Message() : this("", "", "") { }
        public Message(string name, string room, string content) : this(null, name, room, content) { }
        public Message(string id, string name, string room, string content) : base(id, room, name)
        {
            Content = content;
        }

        public override string ToString()
        {
            return $"uctTime: {UtcTime}, room: {Room}, name: {Name}, content: {Content}";
        }

        public override bool Equals(object obj)
        {
            var message = obj as Message;
            return message != null &&
                   UtcTime == message.UtcTime &&
                   Room == message.Room &&
                   Name == message.Name &&
                   Content == message.Content;
        }

        public override int GetHashCode()
        {
            var hashCode = 2007374510;
            hashCode = hashCode * -1521134295 + UtcTime.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Room);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Content);
            return hashCode;
        }

        public static bool operator ==(Message message1, Message message2)
        {
            return EqualityComparer<Message>.Default.Equals(message1, message2);
        }

        public static bool operator !=(Message message1, Message message2)
        {
            return !(message1 == message2);
        }
    }

    public class ImageMessage : MessageBase
    {
        public ImageMessage() { }

        public ImageMessage(string room, string name, Bitmap bitmap) : this(null, room, name, bitmap) { }

        public ImageMessage(string id, string room, string name, Bitmap bitmap) : base(id, room, name)
        {
            Image = bitmap;
        }

        [XmlIgnore]
        public Bitmap Image { get; private set; }

        [XmlElement("Image")]
        public byte[] ImageBuffer
        {
            get
            {
                if (Image == null) return null;
                using (var stream = new MemoryStream())
                {
                    Image.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
            set
            {
                if (value == null)
                {
                    Image = null;
                }
                else
                {
                    using (var stream = new MemoryStream(value))
                    {
                        Image = System.Drawing.Image.FromStream(stream) as Bitmap;
                    }
                }
            }
        }
    }

    public class Receipt : IMessage
    {
        [XmlAttribute]
        public string ID { get; set; }

        public Receipt() { }
        public Receipt(string id)
        {
            ID = id;
        }
    }

    public class FileMessage : MessageBase
    {
        [XmlAttribute]
        public string FileId { get; set; }
        [XmlAttribute]
        public string FileName { get; set; }
        [XmlAttribute]
        public long FileSize { get; set; }

        [XmlAttribute]
        public byte[] data { get; set; }

        public FileMessage()
        {
        }

        public FileMessage(string id, string room, string name, string filename, long fileSize) 
            : this(id, room, name, Utility.GenerateID(), filename, fileSize)
        {
        }

        public FileMessage(string id, string room, string name, string fileId, string filename, long fileSize) 
            : base(id, room, name)
        {
            FileId = fileId;
            FileName = filename;
            FileSize = fileSize;
        }
    }
}
