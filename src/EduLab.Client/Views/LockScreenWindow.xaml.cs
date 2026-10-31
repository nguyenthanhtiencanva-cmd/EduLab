using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using EduLab.Client.Security;
using EduLab.Client.Services;

namespace EduLab.Client.Views
{
    public partial class LockScreenWindow : Window
    {
        private bool _allowClose = false;
        private StudentToolbarWindow? _toolbar;
        private readonly string _studentCachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "student_info.txt");

        public LockScreenWindow()
        {
            InitializeComponent();
            TxtMachineName.Text = Environment.MachineName.ToUpperInvariant();

            Loaded += LockScreenWindow_Loaded;
            Closing += LockScreenWindow_Closing;

            // Nạp thông tin học sinh cũ nếu đã từng báo danh
            LoadSavedStudentInfo();

            // Đăng ký sự kiện từ mạng
            ClientNetworkManager.Instance.OnStatusChanged += UpdateNetworkStatus;
            ClientNetworkManager.Instance.OnLocked += HandleServerLock;
            ClientNetworkManager.Instance.OnUnlocked += HandleServerUnlock;
            ClientNetworkManager.Instance.OnBroadcastReceived += HandleBroadcast;
        }

        private void LoadSavedStudentInfo()
        {
            try
            {
                if (File.Exists(_studentCachePath))
                {
                    var lines = File.ReadAllLines(_studentCachePath);
                    if (lines.Length > 0) TxtStudentName.Text = lines[0];
                    if (lines.Length > 1) TxtClassName.Text = lines[1];
                    if (lines.Length > 2) TxtStudentId.Text = lines[2];
                }
            }
            catch { }
        }

        private void SaveStudentInfo(string name, string className, string id)
        {
            try
            {
                File.WriteAllLines(_studentCachePath, new[] { name, className, id });
            }
            catch { }
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
                e.Cancel = true;
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

            if (!ClientNetworkManager.Instance.IsConnected)
            {
                MessageBox.Show("Máy trạm CHƯA KẾT NỐI được tới Máy Giáo Viên!\n\nVui lòng kiểm tra Wi-Fi hoặc bấm nút '⚙️ Cài đặt IP' ở góc dưới màn hình để nhập IP Máy Giáo Viên.", "Chưa kết nối máy chủ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnCheckIn.IsEnabled = false;
            BtnCheckIn.Content = "Đang gửi báo danh...";

            try
            {
                await ClientNetworkManager.Instance.CheckInAsync(name, id, className);
                SaveStudentInfo(name, className, id);
                MessageBox.Show("Đã báo danh thành công!\nThông tin của em đã được chuyển tới màn hình thầy cô.", "Báo danh thành công", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void BtnConfigIp_Click(object sender, RoutedEventArgs e)
        {
            var ipDialog = new Window
            {
                Title = "Cấu Hình IP Máy Giáo Viên",
                Width = 400,
                Height = 190,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x29, 0x3B)),
                Foreground = System.Windows.Media.Brushes.White,
                Topmost = true,
                ResizeMode = ResizeMode.NoResize
            };

            var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
            sp.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Nhập địa chỉ IP của Máy Giáo Viên (ví dụ: 192.168.1.15):",
                FontSize = 12,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0xBD, 0xF8)),
                Margin = new Thickness(0, 0, 0, 8)
            });

            var txtIp = new System.Windows.Controls.TextBox
            {
                Height = 34,
                FontSize = 14,
                Text = ClientNetworkManager.Instance.GetConfiguredServerIp(),
                Padding = new Thickness(8, 4, 8, 4)
            };
            sp.Children.Add(txtIp);

            var btnSave = new System.Windows.Controls.Button
            {
                Content = "Lưu & Kết Nối Ngay",
                Height = 34,
                Margin = new Thickness(0, 12, 0, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x02, 0x84, 0xC7)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0)
            };
            btnSave.Click += (s, ev) =>
            {
                string ip = txtIp.Text.Trim();
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    ClientNetworkManager.Instance.SetManualServerIp(ip);
                    ipDialog.Close();
                    MessageBox.Show($"Đã lưu IP máy giáo viên: {ip}\nĐang thử kết nối lại...", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            sp.Children.Add(btnSave);

            ipDialog.Content = sp;
            ipDialog.ShowDialog();
        }

        public void ForceExit()
        {
            _allowClose = true;
            RemoveLockSecurity();
            Close();
        }
    }
}