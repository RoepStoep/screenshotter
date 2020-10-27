using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Collections.ObjectModel;

namespace Screenshotter
{

    public static class Screenshot
    {

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        public static Bitmap PrintWindow(IntPtr hwnd)
        {

            RECT rc;
            GetWindowRect(hwnd, out rc);

            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            PrintWindow(hwnd, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;

        }

        public static BitmapImage ToImage(Bitmap bmp)
        {

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Bmp);

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            img.StreamSource = ms;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();

            return img;

        }

        private static readonly object _lockWindows = new object();

        private static Tuple<IntPtr, string>[] _windows = null;

        public static void UpdateWindows(ObservableCollection<CaptureSource> captureSources)
        {
            lock (_lockWindows)
            {
                Tuple<IntPtr, string>[] windows = new Tuple<IntPtr, string>[captureSources.Count];
                for (int i = 0; i < captureSources.Count; i++)
                    windows[i] = Tuple.Create(captureSources[i].WindowHandle, captureSources[i].Path);
                _windows = windows;
            }
        }

        public static bool Recording { get; set; }

        public static async Task StartCapture(Action<Tuple<Bitmap, string>[]> process, TimeSpan interval, CancellationToken cancel)
        {
            Recording = true;
            while (!cancel.IsCancellationRequested)
            {

                DateTime start = DateTime.Now;
                Tuple<Bitmap, string>[] screenshots = null;
                lock (_lockWindows)
                {
                    if (process != null && _windows != null)
                    {
                        screenshots = new Tuple<Bitmap, String>[_windows.Length];
                        for (int i = 0; i < _windows.Length; i++)
                        {
                            if (_windows[i] != null && _windows[i].Item1.ToInt64() != 0) {
                                var screenshot = PrintWindow(_windows[i].Item1);
                                var filename = Path.Combine(_windows[i].Item2, DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss.fff") + ".jpg");
                                screenshots[i] = Tuple.Create(screenshot, filename);
                            }
                        }
                    }
                }

                process(screenshots);

                if (interval > TimeSpan.Zero)
                {
                    TimeSpan skip = DateTime.Now - start;
                    TimeSpan delay = interval - skip;
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, cancel);
                }

            }
            Recording = false;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        private int _Left;
        private int _Top;
        private int _Right;
        private int _Bottom;

        public RECT(RECT Rectangle) : this(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom)
        {
        }
        public RECT(int Left, int Top, int Right, int Bottom)
        {
            _Left = Left;
            _Top = Top;
            _Right = Right;
            _Bottom = Bottom;
        }

        public int X
        {
            get { return _Left; }
            set { _Left = value; }
        }
        public int Y
        {
            get { return _Top; }
            set { _Top = value; }
        }
        public int Left
        {
            get { return _Left; }
            set { _Left = value; }
        }
        public int Top
        {
            get { return _Top; }
            set { _Top = value; }
        }
        public int Right
        {
            get { return _Right; }
            set { _Right = value; }
        }
        public int Bottom
        {
            get { return _Bottom; }
            set { _Bottom = value; }
        }
        public int Height
        {
            get { return _Bottom - _Top; }
            set { _Bottom = value + _Top; }
        }
        public int Width
        {
            get { return _Right - _Left; }
            set { _Right = value + _Left; }
        }
        public Point Location
        {
            get { return new Point(Left, Top); }
            set
            {
                _Left = value.X;
                _Top = value.Y;
            }
        }
        public Size Size
        {
            get { return new Size(Width, Height); }
            set
            {
                _Right = value.Width + _Left;
                _Bottom = value.Height + _Top;
            }
        }

        public static implicit operator Rectangle(RECT Rectangle)
        {
            return new Rectangle(Rectangle.Left, Rectangle.Top, Rectangle.Width, Rectangle.Height);
        }
        public static implicit operator RECT(Rectangle Rectangle)
        {
            return new RECT(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
        }
        public static bool operator ==(RECT Rectangle1, RECT Rectangle2)
        {
            return Rectangle1.Equals(Rectangle2);
        }
        public static bool operator !=(RECT Rectangle1, RECT Rectangle2)
        {
            return !Rectangle1.Equals(Rectangle2);
        }

        public override string ToString()
        {
            return "{Left: " + _Left + "; " + "Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool Equals(RECT Rectangle)
        {
            return Rectangle.Left == _Left && Rectangle.Top == _Top && Rectangle.Right == _Right && Rectangle.Bottom == _Bottom;
        }

        public override bool Equals(object Object)
        {
            if (Object is RECT)
            {
                return Equals((RECT)Object);
            }
            else if (Object is Rectangle)
            {
                return Equals(new RECT((Rectangle)Object));
            }

            return false;
        }

    }

}