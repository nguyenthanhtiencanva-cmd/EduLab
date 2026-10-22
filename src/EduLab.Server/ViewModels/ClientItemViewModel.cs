using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EduLab.Shared.Enums;

namespace EduLab.Server.ViewModels
{
    public class ClientItemViewModel : INotifyPropertyChanged
    {
        private string _machineName = string.Empty;
        private string _displayName = string.Empty;
        private string? _ipAddress;
        private string? _macAddress;
        private string? _studentName;
        private string? _className;
        private ClientStatus _status = ClientStatus.Offline;
        private ImageSource? _thumbnailSource;
        private bool _isSelected;

        public string MachineName
        {
            get => _machineName;
            set { _machineName = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string? IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        public string? MacAddress
        {
            get => _macAddress;
            set { _macAddress = value; OnPropertyChanged(); }
        }

        public string? StudentName
        {
            get => _studentName;
            set { _studentName = value; OnPropertyChanged(); }
        }

        public string? ClassName
        {
            get => _className;
            set { _className = value; OnPropertyChanged(); }
        }

        public ClientStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsOnline));
            }
        }

        public bool IsOnline => Status != ClientStatus.Offline && Status != ClientStatus.Disconnected;

        public string StatusColor => Status switch
        {
            ClientStatus.InUse => "#22C55E",    // Xanh lá: Đang sử dụng
            ClientStatus.Locked => "#EF4444",   // Đỏ: Đang khóa
            ClientStatus.Online => "#38BDF8",   // Xanh dương: Đã kết nối
            _ => "#64748B"                      // Xám: Offline
        };

        public string StatusText => Status switch
        {
            ClientStatus.InUse => "Đang học",
            ClientStatus.Locked => "Đang khóa",
            ClientStatus.Online => "Sẵn sàng",
            _ => "Offline"
        };

        public ImageSource? ThumbnailSource
        {
            get => _thumbnailSource;
            set { _thumbnailSource = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public void UpdateThumbnailFromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;

            try
            {
                using var ms = new MemoryStream(bytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze(); // Cho phép dùng an toàn qua các luồng UI WPF
                ThumbnailSource = image;
            }
            catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
