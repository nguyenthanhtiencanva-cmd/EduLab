using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using EduLab.Server.Services;
using EduLab.Shared.Enums;
using EduLab.Shared.Models;

namespace EduLab.Server.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ClientItemViewModel> AllMachines { get; } = new();
        private ICollectionView _filteredMachinesView;
        public ICollectionView Machines => _filteredMachinesView;

        private int _totalCount = 40;
        private int _onlineCount = 0;
        private int _lockedCount = 0;
        private int _inUseCount = 0;
        private int _checkedInCount = 0;
        private string _statusNotification = "Hệ thống EduLab Server đã sẵn sàng điều khiển phòng máy.";
        private string _selectedFilter = "ALL"; // ALL, ONLINE, INUSE, LOCKED, OFFLINE

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

        public int CheckedInCount
        {
            get => _checkedInCount;
            set { _checkedInCount = value; OnPropertyChanged(); }
        }

        public string StatusNotification
        {
            get => _statusNotification;
            set { _statusNotification = value; OnPropertyChanged(); }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (_selectedFilter != value)
                {
                    _selectedFilter = value;
                    OnPropertyChanged();
                    _filteredMachinesView.Refresh();
                }
            }
        }

        public string ServerIpDisplay { get; set; } = $"IP Giáo Viên: {GetServerLocalIp()} (Cổng: 8888)";

        private static string GetServerLocalIp()
        {
            try
            {
                using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                return endPoint?.Address.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public MainViewModel()
        {
            // Khởi tạo trước sơ đồ 40 máy phòng máy
            for (int i = 1; i <= 40; i++)
            {
                string num = i.ToString("D2");
                AllMachines.Add(new ClientItemViewModel
                {
                    MachineName = $"MAY-{num}",
                    DisplayName = $"Máy {num}",
                    Status = ClientStatus.Offline
                });
            }

            _filteredMachinesView = CollectionViewSource.GetDefaultView(AllMachines);
            _filteredMachinesView.Filter = FilterPredicate;

            // Đăng ký nhận sự kiện từ dịch vụ mạng
            ServerNetworkManager.Instance.OnClientConnected += HandleClientConnected;
            ServerNetworkManager.Instance.OnClientDisconnected += HandleClientDisconnected;
            ServerNetworkManager.Instance.OnClientUpdated += HandleClientUpdated;
            ServerNetworkManager.Instance.OnThumbnailReceived += HandleThumbnailReceived;
            ServerNetworkManager.Instance.OnSubmissionCompleted += HandleSubmissionCompleted;

            UpdateCounters();
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not ClientItemViewModel item) return false;

            return _selectedFilter switch
            {
                "ONLINE" => item.IsOnline,
                "INUSE" => item.Status == ClientStatus.InUse || item.HasStudent,
                "LOCKED" => item.Status == ClientStatus.Locked,
                "OFFLINE" => !item.IsOnline,
                _ => true
            };
        }

        public ClientItemViewModel? FindSlot(string machineName, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(machineName)) return null;

            // 1. Khớp chính xác theo MachineName
            var exact = AllMachines.FirstOrDefault(m => string.Equals(m.MachineName, machineName, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // 2. Khớp theo số máy (Ví dụ: MAY-05, PC05, DESKTOP-5 -> Máy 05)
            var matchNumber = Regex.Match(machineName, @"\d+");
            if (matchNumber.Success && int.TryParse(matchNumber.Value, out int idx) && idx >= 1 && idx <= AllMachines.Count)
            {
                return AllMachines[idx - 1];
            }

            // 3. Khớp theo địa chỉ IP nếu có
            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                var byIp = AllMachines.FirstOrDefault(m => string.Equals(m.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase));
                if (byIp != null) return byIp;
            }

            // 4. Nếu không khớp số, gán vào máy đầu tiên đang Offline chưa có IP
            return AllMachines.FirstOrDefault(m => !m.IsOnline && string.IsNullOrEmpty(m.IpAddress));
        }

        private void HandleClientConnected(ClientInfo info)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var slot = FindSlot(info.MachineName, info.IpAddress);
                if (slot != null)
                {
                    slot.MachineName = info.MachineName;
                    slot.IpAddress = info.IpAddress;
                    slot.MacAddress = info.MacAddress;
                    slot.Status = info.IsScreenLocked ? ClientStatus.Locked : ClientStatus.InUse;
                    if (!string.IsNullOrWhiteSpace(info.StudentName))
                    {
                        slot.StudentName = info.StudentName;
                        slot.ClassName = info.ClassName;
                        slot.StudentId = info.StudentId;
                    }
                    slot.LastActive = DateTime.UtcNow;
                }
                StatusNotification = $"Máy [{info.MachineName}] đã kết nối (IP: {info.IpAddress})";
                UpdateCounters();
            });
        }

        private void HandleClientDisconnected(string machineName)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var slot = AllMachines.FirstOrDefault(m => string.Equals(m.MachineName, machineName, StringComparison.OrdinalIgnoreCase));
                if (slot != null)
                {
                    slot.Status = ClientStatus.Offline;
                }
                StatusNotification = $"Máy [{machineName}] vừa ngắt kết nối.";
                UpdateCounters();
            });
        }

        private void HandleClientUpdated(ClientInfo info)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var slot = FindSlot(info.MachineName, info.IpAddress);
                if (slot != null)
                {
                    if (!string.IsNullOrWhiteSpace(info.StudentName))
                    {
                        slot.StudentName = info.StudentName;
                        slot.ClassName = info.ClassName;
                        slot.StudentId = info.StudentId;
                    }
                    slot.Status = info.Status;
                    slot.LastActive = DateTime.UtcNow;

                    StatusNotification = $"Học sinh [{slot.StudentName}] ({slot.ClassName}) đã báo danh tại {slot.DisplayName}!";
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
                    slot.LastActive = DateTime.UtcNow;
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
            OnlineCount = AllMachines.Count(m => m.IsOnline);
            LockedCount = AllMachines.Count(m => m.Status == ClientStatus.Locked);
            InUseCount = AllMachines.Count(m => m.Status == ClientStatus.InUse);
            CheckedInCount = AllMachines.Count(m => m.HasStudent);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}