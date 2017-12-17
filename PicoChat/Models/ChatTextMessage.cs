using System;
using PicoChat.Common;

namespace PicoChat
{
    public class ChatTextMessage : ChatMessage
    {

        public string Content { get; }
        public ChatTextMessage(string id, DateTime uctTime, string name, string room, string content) : base(id, uctTime, name, room)
        {
            Content = content;
        }
        public ChatTextMessage(string id, string name, string room, string content) : base(id, DateTime.Now, name, room)
        {
            Content = content;
        }
        public ChatTextMessage(string name, string room, string content) : base(null, DateTime.Now, name, room)
        {
            Content = content;
        }
        public ChatTextMessage(Message message) : this(message.ID, message.UtcTime, message.Name, message.Room, message.Content)
        {
        }
    }
}