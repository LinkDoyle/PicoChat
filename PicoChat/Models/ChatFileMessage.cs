using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PicoChat.Common;

namespace PicoChat
{
    public class ChatFileMessage : ChatMessage
    {
        public string FileName { get; }
        public string FileId { get; }

        private long _fileSize;
        public string FileSize
        {
            get
            {
                if (_fileSize < 1024)
                    return $"{_fileSize} Bytes";
                if(_fileSize < 1024 * 1024)
                    return $"{_fileSize / 1024.0 : 0.00} KB";
                return _fileSize < 1024 * 1024 * 1024 ? $"{_fileSize / 1024.0 / 1024.0 : 0.00} MB" : $"{_fileSize / 1024.0 / 1024.0 / 1024.0 : 0.00} GB";
            }
        }
        private bool _isTransfering;
        public bool IsTransfering
        {
            get => _isTransfering;
            set
            {
                _isTransfering = value;
                OnPropertyChanged();
            }
        }

        private int _process;
        public int Process
        {
            get => _process;
            set
            {
                _process = value;
                OnPropertyChanged();
            }
        }

        public ChatFileMessage(FileMessage fileMessage) 
            : base(fileMessage.ID, fileMessage.UtcTime, fileMessage.Name, fileMessage.Room)
        {
            FileName = fileMessage.FileName;
            FileId = fileMessage.FileId;
            _fileSize = fileMessage.FileSize;
        }
    }
}
