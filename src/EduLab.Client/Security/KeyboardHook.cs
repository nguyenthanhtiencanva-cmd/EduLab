using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EduLab.Client.Security
{
    /// <summary>
    /// Chặn các tổ hợp phím hệ thống nguy hiểm khi khóa màn hình:
    /// Windows Key, Alt+Tab, Alt+Esc, Ctrl+Esc, Alt+F4
    /// </summary>
    public static class KeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_TAB = 0x09;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_F4 = 0x73;
        private const int VK_CONTROL = 0x11;

        private const int LLKHF_ALTDOWN = 0x20;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc? _proc;
        private static IntPtr _hookId = IntPtr.Zero;
        private static bool _isHooked = false;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public static void EnableLockHook()
        {
            if (_isHooked) return;
            _proc = HookCallback;
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            string moduleName = curModule?.ModuleName ?? string.Empty;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(moduleName), 0);
            _isHooked = true;
        }

        public static void DisableLockHook()
        {
            if (!_isHooked) return;
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _proc = null;
            _isHooked = false;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                bool isAltPressed = (kbd.flags & LLKHF_ALTDOWN) != 0;
                bool isCtrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;

                // 1. Chặn phím Windows (trái và phải)
                if (kbd.vkCode == VK_LWIN || kbd.vkCode == VK_RWIN)
                {
                    return (IntPtr)1;
                }

                // 2. Chặn Alt + Tab
                if (isAltPressed && kbd.vkCode == VK_TAB)
                {
                    return (IntPtr)1;
                }

                // 3. Chặn Alt + Esc
                if (isAltPressed && kbd.vkCode == VK_ESCAPE)
                {
                    return (IntPtr)1;
                }

                // 4. Chặn Ctrl + Esc (Mở Start Menu)
                if (isCtrlPressed && kbd.vkCode == VK_ESCAPE)
                {
                    return (IntPtr)1;
                }

                // 5. Chặn Alt + F4 (Đóng cửa sổ khóa)
                if (isAltPressed && kbd.vkCode == VK_F4)
                {
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
}
