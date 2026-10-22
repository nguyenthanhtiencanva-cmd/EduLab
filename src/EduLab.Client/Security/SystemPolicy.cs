using System;
using Microsoft.Win32;

namespace EduLab.Client.Security
{
    public static class SystemPolicy
    {
        private const string SUB_KEY = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";

        /// <summary>
        /// Vô hiệu hóa Task Manager để ngăn học sinh tắt tiến trình ứng dụng
        /// </summary>
        public static void SetTaskManagerDisabled(bool disabled)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(SUB_KEY);
                if (key != null)
                {
                    if (disabled)
                    {
                        key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                    }
                    else
                    {
                        key.DeleteValue("DisableTaskMgr", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Không thể thay đổi chính sách Task Manager: {ex.Message}");
            }
        }
    }
}
