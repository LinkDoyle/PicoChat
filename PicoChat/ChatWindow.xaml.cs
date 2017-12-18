using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;

namespace PicoChat
{
    /// <summary>
    /// ChatWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ChatWindow : Window
    {
        public ChatViewModel ViewModel { get; }

        public ChatWindow(IWindowServer windowServer, IClient client)
        {
            ViewModel = new ChatViewModel(windowServer, client);
            InitializeComponent();

            ViewModel.Messages.CollectionChanged += (sender, e) =>
            {
                MessageListView.ScrollIntoView(e.NewItems[e.NewItems.Count - 1]);
            };

            var messageCollectionView = CollectionViewSource.GetDefaultView(MessageListView.ItemsSource) as CollectionView;
            Debug.Assert(messageCollectionView != null, nameof(messageCollectionView) + " != null");
            messageCollectionView.Filter = (item) =>
            {
                var message = (ChatMessage)item;
                var roomName = message.Room;
                if (roomName == null || roomName.Equals("[System]")) return true;
                var selectedItem = JoinedRoomList.SelectedItem;
                return roomName.Equals(selectedItem);
            };

        }

        private void JoinedRoomList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(MessageListView.ItemsSource).Refresh();
        }

        private void SendImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.bmp; *.jpg; *.jpeg; *.png; *.gif) | *.bmp; *.jpg; *.jpeg; *.png; *.gif"
            };
            if (openFileDialog.ShowDialog() != true) return;
            ViewModel.SendImage(openFileDialog.FileName);
        }

        private void SendFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Files (*.*) | *.*"
            };
            if (openFileDialog.ShowDialog() != true) return;
            ViewModel.SendFile(openFileDialog.FileName);
        }

        private void DownloadButton_OnClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
