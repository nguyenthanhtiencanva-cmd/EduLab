using System;
using System.Diagnostics;

namespace EduLab.Shared.Utils
{
    public static class PowerHelper
    {
        public static void Shutdown(int delaySeconds = 0)
        {
            try
            {
                var psi = new ProcessStartInfo("shutdown.exe", $"/s /f /t {delaySeconds}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing shutdown: {ex.Message}");
            }
        }

        public static void Restart(int delaySeconds = 0)
        {
            try
            {
                var psi = new ProcessStartInfo("shutdown.exe", $"/r /f /t {delaySeconds}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing restart: {ex.Message}");
            }
        }

        public static void Logoff()
        {
            try
            {
                var psi = new ProcessStartInfo("shutdown.exe", "/l")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing logoff: {ex.Message}");
            }
        }
    }
}
