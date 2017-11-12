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
    public class Message
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

        public static string Serialize(Message message)
        {

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Message));
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, message);
                return textWriter.ToString();
            }
        }

        public static string Serialize(object @object)
        {

            XmlSerializer xmlSerializer = new XmlSerializer(@object.GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, @object);
                return textWriter.ToString();
            }
        }

        public static Message DeserializeMessage(string message)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Message));
            using (StringReader textReader = new StringReader(message))
            {
                Message result = null;
                try
                {
                    result = (Message)xmlSerializer.Deserialize(textReader);
                }
                catch (InvalidOperationException)
                {

                }
                return result;
            }
        }
    }
}
