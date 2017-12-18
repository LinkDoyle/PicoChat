using System;

namespace PicoChat
{
    public class ChatSystemMessage : ChatMessage
    {
        public string Content { get; }
        public string Tag => Name;

        public ChatSystemMessage(string tag, string content) 
            : base(null, DateTime.Now, tag, "[System]")
        {
            Content = content;
        }

    }
}