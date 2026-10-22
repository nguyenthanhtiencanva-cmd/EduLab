using System;
using System.Windows;
using EduLab.Client.Security;
using EduLab.Client.Services;
using EduLab.Client.Views;

namespace EduLab.Client
{
    public partial class App : Application
    {
        private LockScreenWindow? _lockScreen;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Khởi động mạng và tự động dò tìm máy giáo viên
            ClientNetworkManager.Instance.Start();

            // Mở màn hình khóa mặc định
            _lockScreen = new LockScreenWindow();
            _lockScreen.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Dọn dẹp chính sách và giải phóng hook khi tắt ứng dụng
            KeyboardHook.DisableLockHook();
            SystemPolicy.SetTaskManagerDisabled(false);
            ClientNetworkManager.Instance.Stop();

            base.OnExit(e);
        }
    }
}
