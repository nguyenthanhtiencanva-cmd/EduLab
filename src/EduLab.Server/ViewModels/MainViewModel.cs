using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using EduLab.Server.Services;
using EduLab.Shared.Enums;
using EduLab.Shared.Models;

namespace EduLab.Server.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ClientItemViewModel> Machines { get; } = new();

        private int _totalCount = 40;
        private int _onlineCount = 0;
        private int _lockedCount = 0;
        private int _inUseCount = 0;
        private string _statusNotification = "Hệ thống EduLab Server đang hoạt động trên cổng 8888.";

        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); }
        }

        public int OnlineCount
        {
            get => _onlineCount;
            set { _onlineCount = value; OnPropertyChanged(); }
        }

        public int LockedCount
        {
            get => _lockedCount;
            set { _lockedCount = value; OnPropertyChanged(); }
        }

        public int InUseCount
        {
            get => _inUseCount;
            set { _inUseCount = value; OnPropertyChanged(); }
        }

        public string StatusNotification
        {
            get => _statusNotification;
            set { _statusNotification = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            // Khởi tạo sơ đồ 40 máy chuẩn cho phòng tin
            for (int i = 1; i <= 40; i++)
            {
                string num = i.ToString("D2");
                Machines.Add(new ClientItemViewModel
                {
                    MachineName = $"MAY-{num}",
                    DisplayName = $"Máy {num}",
                    Status = ClientStatus.Offline
                });
            }

            // Đăng ký nhận sự kiện từ dịch vụ mạng
            ServerNetworkManager.Instance.OnClientConnected += HandleClientConnected;
            ServerNetworkManager.Instance.OnClientDisconnected += HandleClientDisconnected;
            ServerNetworkManager.Instance.OnClientUpdated += HandleClientUpdated;
            ServerNetworkManager.Instance.OnThumbnailReceived += HandleThumbnailReceived;
            ServerNetworkManager.Instance.OnSubmissionCompleted += HandleSubmissionCompleted;

            UpdateCounters();
        }

        private ClientItemViewModel? FindSlot(string machineName)
        {
            // Khớp theo tên chuẩn MAY-01 hoặc máy đầu tiên đang offline
            var exact = Machines.FirstOrDefault(m => string.Equals(m.MachineName, machineName, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // Tìm máy theo số thứ tự nếu tên máy chứa số
            var matchNumber = System.Text.RegularExpressions.Regex.Match(machineName, @"\d+");
            if (matchNumber.Success && int.TryParse(matchNumber.Value, out int idx) && idx >= 1 && idx <= 40)
            {
                return Machines[idx - 1];
            }

            return Machines.FirstOrDefault(m => !m.IsOnline);
        }

        private void HandleClientConnected(ClientInfo info)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var slot = FindSlot(info.MachineName);
                if (slot != null)
                {
                    slot.MachineName = info.MachineName;
                    slot.IpAddress = info.IpAddress;
                    slot.MacAddress = info.MacAddress;
                    slot.Status = ClientStatus.Locked;
                    slot.StudentName = info.StudentName;
                    slot.ClassName = info.ClassName;
                }
                StatusNotification = $"Máy [{info.MachineName}] vừa kết nối (IP: {info.IpAddress}).";
                UpdateCounters();
            });
        }

        private void HandleClientDisconnected(string machineName)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var slot = Machines.FirstOrDefault(m => string.Equals(m.MachineName, machineName, StringComparison.OrdinalIgnoreCase));
                if (slot != null)
                {
                    slot.Status = ClientStatus.Offline;
                }
                StatusNotification = $"Máy [{machineName}] đã ngắt kết nối.";
                UpdateCounters();
            });
        }

        private void HandleClientUpdated(ClientInfo info)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var slot = FindSlot(info.MachineName);
                if (slot != null)
                {
                    slot.StudentName = info.StudentName;
                    slot.ClassName = info.ClassName;
                    slot.Status = info.Status;
                }
                UpdateCounters();
            });
        }

        private void HandleThumbnailReceived(string machineName, byte[] thumbBytes)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var slot = FindSlot(machineName);
                if (slot != null)
                {
                    slot.UpdateThumbnailFromBytes(thumbBytes);
                }
            });
        }

        private void HandleSubmissionCompleted(FileTransferMetadata meta, string filePath)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusNotification = $"Học sinh [{meta.StudentName}] (Lớp {meta.ClassName}) vừa nộp bài: {meta.FileName}";
            });
        }

        public void UpdateCounters()
        {
            OnlineCount = Machines.Count(m => m.IsOnline);
            LockedCount = Machines.Count(m => m.Status == ClientStatus.Locked);
            InUseCount = Machines.Count(m => m.Status == ClientStatus.InUse);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
