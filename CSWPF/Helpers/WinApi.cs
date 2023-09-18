using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace CSWPF.Helpers
{
    public static class WinApi
    {
        public const int WM_KEYDOWN = 256;
        public const int WM_KEYUP = 257;
        public const int WM_CHAR = 261;
        public const int WM_SYSKEYDOWN = 260;
        public const int WM_SYSKEYUP = 261;
        public const int WM_PASTE = 770;
        public const int VK_CONTROL = 17;
        public const int INPUT_MOUSE = 0;
        public const int INPUT_KEYBOARD = 1;
        public const int INPUT_HARDWARE = 2;
        public const uint KEYEVENTF_EXTENDEDKEY = 1;
        public const uint KEYEVENTF_KEYUP = 2;
        public const uint KEYEVENTF_UNICODE = 4;
        public const uint KEYEVENTF_SCANCODE = 8;
        public const uint XBUTTON1 = 1;
        public const uint XBUTTON2 = 2;
        public const uint MOUSEEVENTF_MOVE = 1;
        public const uint MOUSEEVENTF_LEFTDOWN = 2;
        public const uint MOUSEEVENTF_LEFTUP = 4;
        public const uint MOUSEEVENTF_RIGHTDOWN = 8;
        public const uint MOUSEEVENTF_RIGHTUP = 16;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 32;
        public const uint MOUSEEVENTF_MIDDLEUP = 64;
        public const uint MOUSEEVENTF_XDOWN = 128;
        public const uint MOUSEEVENTF_XUP = 256;
        public const uint MOUSEEVENTF_WHEEL = 2048;
        public const uint MOUSEEVENTF_VIRTUALDESK = 16384;
        public const uint MOUSEEVENTF_ABSOLUTE = 32768;
        private const short SWP_NOMOVE = 2;
        private const short SWP_NOSIZE = 1;
        private const short SWP_NOZORDER = 4;
        private const int SWP_SHOWWINDOW = 64;
        private static bool fixing;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref WinApi.RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowPos(
          IntPtr hWnd,
          int hWndInsertAfter,
          int x,
          int y,
          int cx,
          int cy,
          int wFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WaitNamedPipe(string name, int timeout);

        [DllImport("kernel32.dll")]
        public static extern int GetPrivateProfileSection(
          string lpAppName,
          byte[] lpszReturnBuffer,
          int nSize,
          string lpFileName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out WinApi.POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(
          uint dwFlags,
          uint dx,
          uint dy,
          uint cButtons,
          uint dwExtraInfo);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.DLL")]
        private static extern void ReleaseCapture();

        [DllImport("user32.DLL")]
        private static extern void SendMessage(IntPtr hwnd, int wmsg, int wparam, int lparam);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private static extern short VkKeyScanEx(char ch);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, WinApi.INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void MoveProcessWindow(IntPtr handle, int x, int y, int w, int h) => WinApi.SetWindowPos(handle, 0, x, y, w, h, 68);

        public static void SendString(string s)
        {
            List<WinApi.INPUT> inputList = new List<WinApi.INPUT>();
            foreach (char ch in s)
            {
                bool[] flagArray = new bool[2] { false, true };
                foreach (bool flag in flagArray)
                {
                    WinApi.INPUT input = new WinApi.INPUT()
                    {
                        type = 1,
                        u = new WinApi.InputUnion()
                        {
                            ki = new WinApi.KEYBDINPUT()
                            {
                                wVk = 0,
                                wScan = (ushort)ch,
                                dwFlags = (uint)(4 | (flag ? 2 : 0)),
                                dwExtraInfo = WinApi.GetMessageExtraInfo()
                            }
                        }
                    };
                    inputList.Add(input);
                }
            }
            int num = (int)WinApi.SendInput((uint)inputList.Count, inputList.ToArray(), Marshal.SizeOf(typeof(WinApi.INPUT)));
        }

        public static void SendCtrlV() => WinApi.PostMessage(WinApi.GetForegroundWindow(), 770U, IntPtr.Zero, IntPtr.Zero);

        public static void PostCtrlV(IntPtr hwnd)
        {
            WinApi.PostMessage(hwnd, 256U, (IntPtr)17, (IntPtr)1);
            WinApi.PostMessage(hwnd, 256U, (IntPtr)86, (IntPtr)0);
            WinApi.PostMessage(hwnd, 257U, (IntPtr)86, (IntPtr)0);
            WinApi.PostMessage(hwnd, 257U, (IntPtr)17, (IntPtr)0);
        }

        public static void SendKey(IntPtr hwnd, int keyCode, bool extended)
        {
            uint lParam = (uint)(1 | (int)WinApi.MapVirtualKey((uint)keyCode, 0U) << 16);
            if (extended)
                lParam |= 16777216U;
            WinApi.PostMessage(hwnd, 256U, (IntPtr)keyCode, (IntPtr)(long)lParam);
            WinApi.PostMessage(hwnd, 257U, (IntPtr)keyCode, (IntPtr)(long)lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private struct INPUT
        {
            public int type;
            public WinApi.InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public WinApi.MOUSEINPUT mi;
            [FieldOffset(0)]
            public WinApi.KEYBDINPUT ki;
            [FieldOffset(0)]
            public WinApi.HARDWAREINPUT hi;
        }

        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(WinApi.POINT point) => new Point((double)point.X, (double)point.Y);
        }
    }
}
