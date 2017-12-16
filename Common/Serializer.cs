using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace PicoChat.Common
{
    public static class Serializer
    {
        public static string Serialize(IMessage @object)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(@object.GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, @object);
                return textWriter.ToString();
            }
        }

        public static byte[] SerializeToBytes(IMessage @object)
        {
            var xmlSerializer = new XmlSerializer(@object.GetType());
            var memoryStream = new MemoryStream();
            using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
            {
                xmlSerializer.Serialize(streamWriter, @object);
                return memoryStream.ToArray();
            }
        }

        public static T Deserialize<T>(string message) where T : IMessage
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using (var textReader = new StringReader(message))
            {
                var result = default(T);
                try
                {
                    result = (T)xmlSerializer.Deserialize(textReader);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                return result;
            }
        }

        public static T Deserialize<T>(byte[] message) where T : IMessage
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            var memoryStream = new MemoryStream(message);
            using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
            {
                var result = default(T);
                try
                {
                    result = (T)xmlSerializer.Deserialize(streamReader);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine(ex.StackTrace);
                }
                return result;
            }
        }
    }
}