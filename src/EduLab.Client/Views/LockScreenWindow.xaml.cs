using System;
using System.ComponentModel;
using System.Windows;
using EduLab.Client.Security;
using EduLab.Client.Services;

namespace EduLab.Client.Views
{
    public partial class LockScreenWindow : Window
    {
        private bool _allowClose = false;
        private StudentToolbarWindow? _toolbar;

        public LockScreenWindow()
        {
            InitializeComponent();
            TxtMachineName.Text = Environment.MachineName.ToUpperInvariant();

            Loaded += LockScreenWindow_Loaded;
            Closing += LockScreenWindow_Closing;

            // Đăng ký sự kiện từ mạng
            ClientNetworkManager.Instance.OnStatusChanged += UpdateNetworkStatus;
            ClientNetworkManager.Instance.OnLocked += HandleServerLock;
            ClientNetworkManager.Instance.OnUnlocked += HandleServerUnlock;
            ClientNetworkManager.Instance.OnBroadcastReceived += HandleBroadcast;
        }

        private void LockScreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLockSecurity();
        }

        private void ApplyLockSecurity()
        {
            try
            {
                KeyboardHook.EnableLockHook();
                SystemPolicy.SetTaskManagerDisabled(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi bật bảo vệ: {ex.Message}");
            }
        }

        private void RemoveLockSecurity()
        {
            try
            {
                KeyboardHook.DisableLockHook();
                SystemPolicy.SetTaskManagerDisabled(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tắt bảo vệ: {ex.Message}");
            }
        }

        private void LockScreenWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true; // Ngăn chặn tắt cửa sổ khóa
            }
        }

        private void UpdateNetworkStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                TxtNetworkStatus.Text = status;
            });
        }

        private void HandleServerLock()
        {
            Dispatcher.Invoke(() =>
            {
                ApplyLockSecurity();
                Show();
                WindowState = WindowState.Maximized;
                Topmost = true;
                Activate();

                _toolbar?.Hide();
            });
        }

        private void HandleServerUnlock()
        {
            Dispatcher.Invoke(() =>
            {
                RemoveLockSecurity();
                Hide();

                if (_toolbar == null)
                {
                    _toolbar = new StudentToolbarWindow(
                        TxtStudentName.Text.Trim(),
                        TxtClassName.Text.Trim()
                    );
                }
                else
                {
                    _toolbar.UpdateStudentInfo(TxtStudentName.Text.Trim(), TxtClassName.Text.Trim());
                }
                _toolbar.Show();
            });
        }

        private void HandleBroadcast(string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(this, message, "Thông báo từ Giáo Viên", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private async void BtnCheckIn_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtStudentName.Text.Trim();
            string id = TxtStudentId.Text.Trim();
            string className = TxtClassName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập họ và tên của em!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnCheckIn.IsEnabled = false;
            BtnCheckIn.Content = "Đang gửi báo danh...";

            try
            {
                await ClientNetworkManager.Instance.CheckInAsync(name, id, className);
                MessageBox.Show("Đã báo danh thành công! Vui lòng chờ thầy cô mở máy.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi báo danh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnCheckIn.IsEnabled = true;
                BtnCheckIn.Content = "Báo Danh Vào Tiết Học";
            }
        }

        public void ForceExit()
        {
            _allowClose = true;
            RemoveLockSecurity();
            Close();
        }
    }
}
