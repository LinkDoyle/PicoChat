using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Xml.Serialization;

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
        ALREADY_JOINNED
    }


    public struct DataPackage
    {
        public Int16 Version;
        public MessageType Type;
        public Int32 Length;
        public Byte[] Data;

        public DataPackage(MessageType messageType, byte[] data)
        {
            Version = 0;
            Type = messageType;
            if (data != null)
            {
                Length = data.Length;
                Data = data;
            }
            else
            {
                Length = 0;
                Data = new byte[0];
            }
        }

        public static DataPackage FromStream(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                DataPackage dataPackage = new DataPackage
                {
                    Version = reader.ReadInt16(),
                    Type = (MessageType)reader.ReadInt16(),
                    Length = reader.ReadInt32()
                };
                if (Enum.IsDefined(typeof(MessageType), dataPackage.Type))
                {
                    dataPackage.Data = reader.ReadBytes(dataPackage.Length);
                }
                else
                {
                    Debug.WriteLine($"Warning: unknown message type value {(Int16)dataPackage.Type}.");
                    dataPackage.Type = MessageType.SYSTEM_UNKNOWN;
                }
                return dataPackage;
            }
        }
    }

    public static class Extension
    {
        public static void Write(this Stream stream, DataPackage dataPackage)
        {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(dataPackage.Version);
                writer.Write((Int16)dataPackage.Type);
                writer.Write(dataPackage.Data.Length);
                writer.Write(dataPackage.Data);
            }
        }
    }

    [XmlRoot]
    public interface IMessage
    {
    }

    public class LoginInfo : IMessage
    {
        public LoginInfo() : this(null, null)
        {

        }

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

        protected MessageBase(string id, string name, string room)
        {
            ID = id;
            UtcTime = DateTime.Now;
            Name = name;
            Room = room;
        }
    }

    public class Message : MessageBase
    {
        [XmlElement]
        public string Content { get; set; }

        public Message() : this("", "", "") { }
        public Message(string name, string room, string content) : this(null, name, room, content) { }
        public Message(string id, string name, string room, string content) : base(id, name, room)
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

        public ImageMessage(string id, string room, string name, Bitmap bitmap) : base(id, name, room)
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
}
