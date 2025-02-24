using System;
using System.Windows.Input;
using System.Windows.Media;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Config;
using ViewModelBase = CNC.Core.ViewModelBase;

namespace ioSenderTouch.ViewModels
{
    public class RenderViewModel : ViewModelBase, IActiveViewModel
    {
        private bool _showOverlay;
        private SolidColorBrush _foregroundColor;

        public bool ShowOverlay
        {
            get => _showOverlay;
            set
            {
                if (value == _showOverlay) return;
                _showOverlay = value;
                OnPropertyChanged();
            }
        }
        public RenderViewModel()
        {
            AppConfig.Settings.OnConfigFileLoaded += AppConfigurationLoaded;
        }

        public SolidColorBrush ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (Equals(value, _foregroundColor)) return;
                _foregroundColor = value;
                OnPropertyChanged();
            }
        }

        private void AppConfigurationLoaded(object sender, EventArgs e)
        {
            ShowOverlay = AppConfig.Settings.GCodeViewer.ShowTextOverlay;
            ForegroundColor = AppConfig.Settings.GCodeViewer.BlackBackground ?
                Brushes.White : Brushes.Black;
        }

        public bool EnableStart { get; set; }

        public bool EnableHold { get; set; }

        public bool EnableStop { get; set; }
        public bool EnableRewind { get; set; }
        public void Activated()
        {
            
        }

        public void Deactivated()
        {
            
        }

        public bool Active { get; set; }
        public string Name { get; }
    }
}
