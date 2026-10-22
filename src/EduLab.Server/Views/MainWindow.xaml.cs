using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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

        private async void BtnLockAll_Click(object sender, RoutedEventArgs e)
        {
            await ServerNetworkManager.Instance.LockAllAsync();
            foreach (var m in _vm.Machines)
            {
                if (m.IsOnline) m.Status = Shared.Enums.ClientStatus.Locked;
            }
            _vm.UpdateCounters();
            _vm.StatusNotification = "Đã gửi lệnh khóa toàn bộ phòng máy.";
        }

        private async void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
        {
            await ServerNetworkManager.Instance.UnlockAllAsync();
            foreach (var m in _vm.Machines)
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
                Filter = "Tất cả các tệp (*.*)|*.*|Tài liệu (*.docx;*.pdf;*.pptx)|*.docx;*.pdf;*.pptx"
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
            var res = MessageBox.Show("Thầy có chắc chắn muốn TẮT NGUỒN toàn bộ 40 máy không?", "Xác nhận tắt máy", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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

        private async void MenuLockSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is ClientItemViewModel client)
            {
                await ServerNetworkManager.Instance.LockMachineAsync(client.MachineName);
                _vm.UpdateCounters();
            }
        }

        private async void MenuUnlockSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is ClientItemViewModel client)
            {
                await ServerNetworkManager.Instance.UnlockMachineAsync(client.MachineName);
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
