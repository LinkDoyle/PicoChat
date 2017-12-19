using PicoChat.Common;
using PicoChat.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PicoChat.SampleData
{
    public class SampleChatView
    {
        public ChatViewModel ViewModel { get; } = new ChatViewModel();
    }
    public class ChatViewModel : BindableBase
    {
        private readonly ObservableCollection<ChatMessage> _messagesWaitToConfirm =
            new ObservableCollection<ChatMessage>();

        private readonly ObservableCollection<ChatFileMessage> _transferingFiles =
            new ObservableCollection<ChatFileMessage>();

        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();
        public ObservableCollection<string> JoinRooms { get; } = new ObservableCollection<string>();

        private MessageColorInfo _messageColorInfo = new MessageColorInfo("#FF000000");

        public MessageColorInfo MessageColorInfo
        {
            get => _messageColorInfo;
            set => SetProperty(ref _messageColorInfo, value);
        }


        private MessageFontInfo _messageFontInfo = new MessageFontInfo("Consolas", 14);

        public MessageFontInfo MessageFontInfo
        {
            get => _messageFontInfo;
            set => SetProperty(ref _messageFontInfo, value);
        }

        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _connection;

        public string Conncetion
        {
            get => _connection;
            set => SetProperty(ref _connection, value);
        }

        private string _selectedRoom;

        public string SelectedRoom
        {
            get => _selectedRoom;
            set => SetProperty(ref _selectedRoom, value);
        }

        private string _messageToSend;

        public string MessageToSend
        {
            get => _messageToSend;
            set => SetProperty(ref _messageToSend, value);
        }

        public ICommand SendMessageCommand { get; }
        public ICommand SendImageCommand { get; }
        public ICommand SendFileCommand { get; }
        public ICommand PullFileCommand { get; }
        public ICommand SetMessageColorCommand { get; }
        public ICommand SetMessageFontCommand { get; }

        public ChatViewModel()
        {
            JoinRooms.Add("R1");
            JoinRooms.Add("R2");
            JoinRooms.Add("R3");
            JoinRooms.Add("R4");

            Messages.Add(new ChatSystemMessage("[Info]", "Hello"));
            Messages.Add(new ChatTextMessage(Utility.GenerateID(), "Jack", "R1", "Hello"));
            Messages.Add(new ChatFileMessage(new FileMessage("", "R1", "Admin", "", "Test", 2048)));
        }
    }
}
