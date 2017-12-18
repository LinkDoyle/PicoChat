using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PicoChat
{
    public class ChatMessage : INotifyPropertyChanged
    {
        private bool _hasReceipt;
        public bool HasReceipt
        {
            get => _hasReceipt;
            set
            {
                _hasReceipt = value;
                OnPropertyChanged();
            }
        }

        private bool _isLocalMessage;
        public bool IsLocalMessage
        {
            get => _isLocalMessage;
            set
            {
                _isLocalMessage = value;
                OnPropertyChanged();
            }
        }

        public string ID { get; }
        public DateTime UtcTime { get; }
        public string Name { get; set; }
        public string Room { get; set; }

        protected ChatMessage(string id, DateTime uctTime, string name, string room)
        {
            ID = id;
            UtcTime = uctTime;
            Name = name;
            Room = room;
            _isLocalMessage = Name == "<this>";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}