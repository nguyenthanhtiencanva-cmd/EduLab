using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EduLab.Server.Services;
using EduLab.Server.ViewModels;
using Microsoft.Win32;

namespace EduLab.Server.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            // Khởi động dịch vụ mạng máy giáo viên
            ServerNetworkManager.Instance.Start();

            Closed += (s, e) => ServerNetworkManager.Instance.Stop();
        }

        // ================= XỬ LÝ BỘ LỌC HIỂN THỊ =================
        private void FilterAll_Click(object sender, RoutedEventArgs e) => _vm.SelectedFilter = "ALL";
        private void FilterOnline_Click(object sender, RoutedEventArgs e) => _vm.SelectedFilter = "ONLINE";
        private void FilterInUse_Click(object sender, RoutedEventArgs e) => _vm.SelectedFilter = "INUSE";
        private void FilterLocked_Click(object sender, RoutedEventArgs e) => _vm.SelectedFilter = "LOCKED";
        private void FilterOffline_Click(object sender, RoutedEventArgs e) => _vm.SelectedFilter = "OFFLINE";

        // ================= CÁC THAO TÁC TOÀN PHÒNG =================
        private async void BtnLockAll_Click(object sender, RoutedEventArgs e)
        {
            await ServerNetworkManager.Instance.LockAllAsync();
            foreach (var m in _vm.AllMachines)
            {
                if (m.IsOnline) m.Status = Shared.Enums.ClientStatus.Locked;
            }
            _vm.UpdateCounters();
            _vm.StatusNotification = "Đã gửi lệnh khóa toàn bộ 40 máy.";
        }

        private async void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
        {
            await ServerNetworkManager.Instance.UnlockAllAsync();
            foreach (var m in _vm.AllMachines)
            {
                if (m.IsOnline) m.Status = Shared.Enums.ClientStatus.InUse;
            }
            _vm.UpdateCounters();
            _vm.StatusNotification = "Đã gửi lệnh mở khóa toàn bộ phòng máy cho học sinh làm bài.";
        }

        private async void BtnSendFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Chọn đề bài / tài liệu phát cho học sinh",
                Filter = "Tất cả các tệp (*.*)|*.*|Tài liệu (*.docx;*.pdf;*.pptx;*.xlsx)|*.docx;*.pdf;*.pptx;*.xlsx"
            };

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.FileName;
                _vm.StatusNotification = $"Đang phát tài liệu [{Path.GetFileName(path)}] xuống các máy...";
                try
                {
                    await ServerNetworkManager.Instance.DistributeFileAsync(path);
                    MessageBox.Show($"Đã phát thành công file: {Path.GetFileName(path)} xuống Desktop các máy học sinh!", "Phát tài liệu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    _vm.StatusNotification = $"Đã phát xong file: {Path.GetFileName(path)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi phát file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnOpenSubmissions_Click(object sender, RoutedEventArgs e)
        {
            string dir = ServerNetworkManager.Instance.SubmissionsDirectory;
            if (Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
            else
            {
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
        }

        private async void BtnBroadcast_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new BroadcastDialog
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.BroadcastText))
            {
                string msg = dlg.BroadcastText;
                await ServerNetworkManager.Instance.BroadcastMessageAsync(msg);
                _vm.StatusNotification = $"Đã gửi thông báo: \"{msg}\"";
            }
        }

        private void BtnScreenMatrix_Click(object sender, RoutedEventArgs e)
        {
            var matrixWin = new ScreenMatrixWindow(_vm);
            matrixWin.Show();
        }

        private async void BtnShutdownAll_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Thầy có chắc chắn muốn TẮT NGUỒN toàn bộ các máy trạm không?", "Xác nhận tắt máy", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                await ServerNetworkManager.Instance.ShutdownAllAsync();
                _vm.StatusNotification = "Đã gửi lệnh tắt toàn bộ máy.";
            }
        }

        private async void BtnRestartAll_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Thầy có chắc chắn muốn KHỞI ĐỘNG LẠI toàn bộ các máy trạm không?", "Xác nhận khởi động lại", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                await ServerNetworkManager.Instance.RestartAllAsync();
                _vm.StatusNotification = "Đã gửi lệnh khởi động lại các máy.";
            }
        }

        // ================= CÁC THAO TÁC TRÊN TỪNG MÁY RIÊNG LẺ =================
        private void MachineCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement el && el.DataContext is ClientItemViewModel client)
            {
                // Nhấp đúp mở cửa sổ xem màn hình chi tiết
                var zoomWin = new Window
                {
                    Title = $"Xem màn hình: {client.DisplayName} - {client.StudentDisplayInfo}",
                    Width = 1050,
                    Height = 680,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = System.Windows.Media.Brushes.Black,
                    Content = new System.Windows.Controls.Image
                    {
                        Source = client.ThumbnailSource,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    }
                };
                zoomWin.ShowDialog();
            }
        }

        private async void BtnQuickToggleLock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ClientItemViewModel client)
            {
                if (client.IsLocked)
                {
                    await ServerNetworkManager.Instance.UnlockMachineAsync(client.MachineName);
                    client.Status = Shared.Enums.ClientStatus.InUse;
                }
                else
                {
                    await ServerNetworkManager.Instance.LockMachineAsync(client.MachineName);
                    client.Status = Shared.Enums.ClientStatus.Locked;
                }
                _vm.UpdateCounters();
            }
        }

        private async void BtnQuickShutdown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ClientItemViewModel client)
            {
                var res = MessageBox.Show($"Thầy có chắc chắn muốn TẮT NGUỒN {client.DisplayName}?", "Xác nhận tắt máy", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    await ServerNetworkManager.Instance.ShutdownMachineAsync(client.MachineName);
                    _vm.StatusNotification = $"Đã gửi lệnh tắt {client.DisplayName}";
                }
            }
        }

        private async void MenuLockSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is ClientItemViewModel client)
            {
                await ServerNetworkManager.Instance.LockMachineAsync(client.MachineName);
                client.Status = Shared.Enums.ClientStatus.Locked;
                _vm.UpdateCounters();
            }
        }

        private async void MenuUnlockSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is ClientItemViewModel client)
            {
                await ServerNetworkManager.Instance.UnlockMachineAsync(client.MachineName);
                client.Status = Shared.Enums.ClientStatus.InUse;
                _vm.UpdateCounters();
            }
        }

        private async void MenuShutdownSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is ClientItemViewModel client)
            {
                await ServerNetworkManager.Instance.ShutdownMachineAsync(client.MachineName);
            }
        }

        private async void MenuRestartSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is ClientItemViewModel client)
            {
                await ServerNetworkManager.Instance.RestartMachineAsync(client.MachineName);
            }
        }
    }
}