using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PicoChat.Common.Tests
{
    [TestClass()]
    public class SerializerTests
    {
        [TestMethod()]
        public void SerializeTest()
        {
            Message message = new Message("Pico", "Room", "Hello");
            string serializedMessage = Serializer.Serialize(message);
            Message deserializedMessage = Serializer.DeserializeMessage(serializedMessage);
            Assert.AreEqual(message, deserializedMessage);

            Console.WriteLine(message);
            Console.WriteLine(serializedMessage);
            Console.WriteLine(deserializedMessage);
        }
    }
}