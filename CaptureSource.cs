using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Screenshotter
{

    public class CaptureSource : INotifyPropertyChanged
    {

        protected int _index;
        private Popup _popupWindows;
        private ObservableCollection<CaptureSource> _collection;

        public CaptureSource(int index, Popup popupWindows, ObservableCollection<CaptureSource> collection)
        {
            _index = index;
            _popupWindows = popupWindows;
            _collection = collection;

            _basePath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public IntPtr WindowHandle { get; set; }

        private string _basePath;

        public string Path
        {
            get { return System.IO.Path.Combine(_basePath, (_index + 1).ToString()); }
        }

        private string _windowTitle;

        public string WindowTitle
        {
            get 
            { 
                return _windowTitle == null ? $"Source #{_index + 1} - no window selected" : _windowTitle; 
            }
            set 
            {
                _windowTitle = value;
                NotifyPropertyChanged();
            }
        }

        private BitmapImage _windowImage;

        public BitmapImage WindowImage
        {
            get { return _windowImage; }
            set 
            {
                _windowImage = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectWindowCaption
        {
            get => $"Select window #{_index + 1}";
        }

        public string RemoveSourceCaption
        {
            get => $"Remove source #{_index + 1}";
        }

        private ICommand _clickCommand;
        public ICommand SelectWindowCommand
        {
            get => _clickCommand ?? (_clickCommand = new CommandHandler(() => SelectWindowPopup(), () => true));
        }

        private void SelectWindowPopup()
        {
            _popupWindows.Tag = this;
            _popupWindows.IsOpen = true;
        }

        private ICommand _pathCommand;
        public ICommand SelectPathCommand
        {
            get => _pathCommand ?? (_pathCommand = new CommandHandler(() => SelectPathPopup(), () => true));
        }

        private void SelectPathPopup()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.SelectedPath = _basePath;
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    _basePath = dialog.SelectedPath;
                    NotifyPropertyChanged("Path");

                    Screenshot.UpdateWindows(_collection);

                }
            }
        }

        private ICommand _removeSourceCommand;
        public ICommand RemoveSourceCommand
        {
            get => _removeSourceCommand ?? (_removeSourceCommand = new CommandHandler(() => RemoveSource(), () => _collection.Count > 1 && !Screenshot.Recording));
        }

        private void RemoveSource()
        {
            _collection.RemoveAt(_index);
            for (int i = _index; i < _collection.Count; i++)
            {
                _collection[i]._index -= 1;
                _collection[i].NotifyPropertyChanged("SelectWindowCaption");
                _collection[i].NotifyPropertyChanged("RemoveSourceCaption");
                _collection[i].NotifyPropertyChanged("WindowTitle");
                _collection[i].NotifyPropertyChanged("Path");
            }
            Screenshot.UpdateWindows(_collection);
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
