using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Screenshotter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ObservableCollection<CaptureSource> _captureSources = new ObservableCollection<CaptureSource>();

        public MainWindow()
        {
            InitializeComponent();

            lstWindows.ItemsSource = _windowTitles;
            icCaptureSources.ItemsSource = _captureSources;

            btnAddSource_Click(null, null);
        }

        private void btnAddSource_Click(object sender, RoutedEventArgs e)
        {
            _captureSources.Add(new CaptureSource(_captureSources.Count, popupWindows, _captureSources));
        }

        #region Recording logic

        private CancellationTokenSource _stopCapture;

        private void btnStartCapture_Click(object sender, RoutedEventArgs e)
        {

            int interval;
            if (!int.TryParse(txtInterval.Text, out interval))
            {
                MessageBox.Show("Invalid interval: enter a numeric value in milliseconds");
                return;
            }

            Screenshot.UpdateWindows(_captureSources);

            _stopCapture = new CancellationTokenSource();
            Task.Factory.StartNew(() => Screenshot.StartCapture(ProcessCapture, TimeSpan.FromMilliseconds(interval), _stopCapture.Token));

            txtInterval.IsEnabled = false;
            btnStartCapture.IsEnabled = false;
            btnStopCapture.IsEnabled = true;

            this.Title = "Screenshotter - recording";
        }

        private void btnStopCapture_Click(object sender, RoutedEventArgs e)
        {

            if (_stopCapture != null)
                _stopCapture.Cancel();
            Screenshot.Recording = false;

            txtInterval.IsEnabled = true;
            btnStartCapture.IsEnabled = true;
            btnStopCapture.IsEnabled = false;

            this.Title = "Screenshotter - idle";
        }

        private void ProcessCapture(Tuple<Bitmap, string>[] captures)
        {
            if (captures == null) return;

            bool scale = false;
            for (int i = 0; i < captures.Length; i++)
            {
                if (captures[i] != null && captures[i].Item1 != null)
                {

                    if (i < _captureSources.Count)
                        _captureSources[i].WindowImage = Screenshot.ToImage(captures[i].Item1);

                    var ms = ImgConv.Bitmap2Jpeg(scale ? ImgConv.ScaleBitmap(captures[i].Item1, 640, 480) : captures[i].Item1);
                    Directory.CreateDirectory(Path.GetDirectoryName(captures[i].Item2));
                    using (FileStream file = new FileStream(captures[i].Item2, FileMode.Create, FileAccess.Write))
                    {
                        ms.WriteTo(file);
                        file.Flush();
                    }
                    ms.Dispose();

                }
            }

        }

        #endregion

        #region Select window popup

        private ObservableCollection<string> _windowTitles = new ObservableCollection<string>();
        private List<IntPtr> _windowHandles = new List<IntPtr>();

        private CaptureSource _selectedSource = null;

        private void popupWindows_Opened(object sender, EventArgs e)
        {

            var windowInfos = WindowInfos.GetWindowInfos();

            _windowTitles.Clear();
            _windowHandles.Clear();

            // add browsers on top
            foreach (var window in windowInfos)
            {
                if (window.Value.IsBrowser())
                {
                    _windowTitles.Add(window.Value.ToString());
                    _windowHandles.Add(window.Value.Handle);
                }
            }

            foreach (var window in windowInfos)
            {
                if (!window.Value.IsBrowser())
                {
                    _windowTitles.Add(window.Value.ToString());
                    _windowHandles.Add(window.Value.Handle);
                }
            }

            var popup = (Popup)sender;
            _selectedSource = (CaptureSource)popup.Tag;
            for (int i = 0; i < _windowHandles.Count; i++)
            {
                if (_windowHandles[i] == _selectedSource.WindowHandle)
                {
                    lstWindows.SelectedIndex = i;
                    lstWindows.ScrollIntoView(_windowTitles[i]);
                    break;
                }
            }

        }

        private void lstWindows_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstWindows.SelectedIndex != -1 && _selectedSource != null)
            {
                var screenshot = Screenshot.PrintWindow(_windowHandles[lstWindows.SelectedIndex]);

                _selectedSource.WindowTitle = _windowTitles[lstWindows.SelectedIndex];
                _selectedSource.WindowHandle = _windowHandles[lstWindows.SelectedIndex];
                _selectedSource.WindowImage = Screenshot.ToImage(screenshot);

                Screenshot.UpdateWindows(_captureSources);

            }
        }

        #endregion

    }
}
