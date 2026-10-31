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
        private string? _studentId;
        private ClientStatus _status = ClientStatus.Offline;
        private ImageSource? _thumbnailSource;
        private bool _isSelected;
        private DateTime _lastActive = DateTime.MinValue;

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
            set
            {
                _studentName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStudent));
                OnPropertyChanged(nameof(StudentDisplayInfo));
            }
        }

        public string? StudentId
        {
            get => _studentId;
            set { _studentId = value; OnPropertyChanged(); }
        }

        public string? ClassName
        {
            get => _className;
            set
            {
                _className = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StudentDisplayInfo));
            }
        }

        public bool HasStudent => !string.IsNullOrWhiteSpace(StudentName);

        public string StudentDisplayInfo
        {
            get
            {
                if (string.IsNullOrWhiteSpace(StudentName)) return "--- Trống (Chưa có HS) ---";
                return string.IsNullOrWhiteSpace(ClassName) ? StudentName : $"{StudentName} ({ClassName})";
            }
        }

        public ClientStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusBadgeBg));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsOnline));
                OnPropertyChanged(nameof(IsLocked));
                OnPropertyChanged(nameof(CardBorderBrush));
            }
        }

        public bool IsOnline => Status != ClientStatus.Offline && Status != ClientStatus.Disconnected;
        public bool IsLocked => Status == ClientStatus.Locked;

        public string StatusColor => Status switch
        {
            ClientStatus.InUse => "#10B981",    // Xanh lá tươi: Đang học
            ClientStatus.Locked => "#EF4444",   // Đỏ: Đang khóa
            ClientStatus.Online => "#06B6D4",   // Xanh Cyan: Sẵn sàng
            _ => "#64748B"                      // Xám: Offline
        };

        public string StatusBadgeBg => Status switch
        {
            ClientStatus.InUse => "#064E3B",
            ClientStatus.Locked => "#7F1D1D",
            ClientStatus.Online => "#164E63",
            _ => "#1E293B"
        };

        public string CardBorderBrush => Status switch
        {
            ClientStatus.InUse => "#059669",
            ClientStatus.Locked => "#DC2626",
            ClientStatus.Online => "#0284C7",
            _ => "#334155"
        };

        public string StatusText => Status switch
        {
            ClientStatus.InUse => "Đang học",
            ClientStatus.Locked => "Đang khóa",
            ClientStatus.Online => "Sẵn sàng",
            _ => "Chưa bật"
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

        public DateTime LastActive
        {
            get => _lastActive;
            set { _lastActive = value; OnPropertyChanged(); }
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
                image.Freeze();
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