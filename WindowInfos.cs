using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Screenshotter
{

    public class WindowInfo
    {

        public IntPtr Handle = IntPtr.Zero;
        public FileInfo File = null;
        public string Title = String.Empty;

        public override string ToString()
        {
            if (File != null)
                return File.Name + " - " + Title;
            else
                return "(unknown) - " + Title;
        }

        public bool IsBrowser()
        {

            if (File == null)
                return false;

            switch (File.Name)
            {

                case "iexplore.exe":
                case "firefox.exe":
                case "chrome.exe":
                    return true;

                default:
                    return false;
            }

        }

    }

    public static class WindowInfos
    {

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        public static IDictionary<IntPtr, WindowInfo> GetWindowInfos()
        {

            IntPtr shellWindow = GetShellWindow();
            Dictionary<IntPtr, WindowInfo> windows = new Dictionary<IntPtr, WindowInfo>();

            EnumWindows(new EnumWindowsProc(delegate (IntPtr hWnd, int lParam) {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;
                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;
                StringBuilder builder = new StringBuilder(length);
                GetWindowText(hWnd, builder, length + 1);
                var info = new WindowInfo();
                info.Handle = hWnd;
                string file = GetProcessPath(hWnd);
                if (!String.IsNullOrEmpty(file))
                    info.File = new FileInfo(GetProcessPath(hWnd));
                info.Title = builder.ToString();
                windows[hWnd] = info;
                return true;
            }), 0);
            return windows;
        }

        private static string GetProcessPath(IntPtr hwnd)
        {

            try
            {
                uint pid = 0;
                GetWindowThreadProcessId(hwnd, out pid);
                if (hwnd != IntPtr.Zero)
                {
                    if (pid != 0)
                    {
                        var process = Process.GetProcessById((int)pid);
                        if (process != null)
                        {
                            return process.MainModule.FileName.ToString();
                        }
                    }
                }
            }
            catch { }

            return "";

        }

        [DllImport("USER32.DLL")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        //WARN: Only for "Any CPU":
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out uint processId);

    }

}
