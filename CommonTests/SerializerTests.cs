using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PicoChat.Common.Tests
{
    [TestClass()]
    public class SerializerTests
    {
        [TestMethod()]
        public void SerializeTest0()
        {
            Message message = new Message("Pico", "Room", "Hello");
            string serializedMessage = Serializer.Serialize(message);
            Message deserializedMessage = Serializer.DeserializeMessage(serializedMessage);
            Assert.AreEqual(message, deserializedMessage);

            Console.WriteLine(message);
            Console.WriteLine(serializedMessage);
            Console.WriteLine(deserializedMessage);
        }

        [TestMethod()]
        public void SerializeTest1()
        {
            RoomInfo roomInfo = new RoomInfo("Room");
            string serializedRoomInfo = Serializer.Serialize(roomInfo);
            RoomInfo deserializedMessage = Serializer.Deserialize<RoomInfo>(serializedRoomInfo);
            Assert.AreEqual(roomInfo, deserializedMessage);

            Console.WriteLine(roomInfo);
            Console.WriteLine(serializedRoomInfo);
            Console.WriteLine(deserializedMessage);
        }
    }
}