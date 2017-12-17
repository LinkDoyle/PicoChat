using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PicoChat.Common
{
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
}