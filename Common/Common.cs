using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PicoChat.Common
{
    public enum MessageType
    {
        SYSTEM_OK,
        SYSTEM_ERROR,
        SYSTEM_UNKNOWN,

        SYSTEM_LOGIN_OK,
        SYSTEM_LOGIN_FAILED,
        SYSTEM_UNJOIN_ROOM,
        SYSTEM_JOIN_ROOM_OK,
        SYSTEM_LEAVE_ROOM_OK,

        CLIENT_LOGIN,
        CLIENT_LOGOUT,
        CLIENT_JOIN_ROOM,
        CLIENT_LEAVE_ROOM,
        CLIENT_LIST_JOINED_ROOMS,
        CLIENT_MESSAGE,
        NO_LOGGED,
        ALREADY_JOINNED
    }

    public static class Constants
    {
        public const int MESSAGE_TYPE_LENGTH = 4;
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

    public class Message : IMessage
    {
        [XmlAttribute]
        public DateTime UtcTime { get; set; }
        [XmlAttribute]
        public string Room { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlElement]
        public string Content { get; set; }

        public Message() : this("", "", "") { }
        public Message(string name, string room, string content)
        {
            UtcTime = DateTime.Now;
            Name = name;
            Room = room;
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

    public static class Serializer
    {

        public static byte[] SerializeToBytes(Message message)
        {
            return Encoding.UTF8.GetBytes(Serialize(message));
        }

        public static string Serialize(IMessage @object)
        {

            XmlSerializer xmlSerializer = new XmlSerializer(@object.GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, @object);
                return textWriter.ToString();
            }
        }

        public static T Deserialize<T>(string message) where T : IMessage
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader textReader = new StringReader(message))
            {
                T result = default(T);
                try
                {
                    result = (T)xmlSerializer.Deserialize(textReader);
                }
                catch (InvalidOperationException)
                {

                }
                return result;
            }
        }

        public static Message DeserializeMessage(string message)
        {
            return Deserialize<Message>(message);
        }
    }
}
