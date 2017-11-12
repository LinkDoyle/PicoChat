using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public class Message
    {
        public DateTime utcTime;
        public string room;
        public string author;
        public string content;
        public Message() : this("", "", "") { }
        public Message(string author, string room, string content)
        {
            this.utcTime = DateTime.UtcNow;
            this.author = author;
            this.room = room;
            this.content = content;
        }
        public override string ToString()
        {
            return $"uctTime: {utcTime}, room: {room}, author: {author}, content: {content}";
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

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Message));
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
