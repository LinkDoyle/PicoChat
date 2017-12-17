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

        public string ID { get; }
        public DateTime UtcTime { get; }
        public string Name { get; }
        public string Room { get; }

        protected ChatMessage(string id, DateTime uctTime, string name, string room)
        {
            ID = id;
            UtcTime = uctTime;
            Name = name;
            Room = room;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}