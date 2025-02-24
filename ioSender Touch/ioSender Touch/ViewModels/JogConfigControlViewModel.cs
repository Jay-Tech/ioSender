using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Config;

namespace ioSenderTouch.ViewModels
{
    public class JogConfigControlViewModel : INotifyPropertyChanged
    {
        private readonly GrblViewModel _grblViewModel;
        private string _unit;
        private JogConfig _measurement;
        private bool _isGrbl = false;

        public string Unit
        {
            get => _unit;
            set
            {
                if (value == _unit) return;
                _unit = value;
                OnPropertyChanged();
            }
        }
        public bool IsGrbl
        {
            get => _isGrbl;
            set
            {
                if (value == _isGrbl) return;
                _isGrbl = value;
                OnPropertyChanged();
            }
        }
        public JogConfig Measurement
        {
            get => _measurement;
            set
            {
                if (Equals(value, _measurement)) return;
                _measurement = value;
                OnPropertyChanged();
            }
        }

        public JogConfigControlViewModel(GrblViewModel grblViewModel)
        {
            _grblViewModel = grblViewModel;
            _grblViewModel.PropertyChanged += _grblViewModel_PropertyChanged;
            Measurement = _grblViewModel.IsMetric ? AppConfig.Settings.Base.JogMetric : AppConfig.Settings.Base.JogImperial;
            Unit = _grblViewModel.IsMetric ? "mm" : "in";
        }

       


        private void _grblViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Unit"))
            {
                Measurement = _grblViewModel.IsMetric ? AppConfig.Settings.Base.JogMetric : AppConfig.Settings.Base.JogImperial;
                Unit = _grblViewModel.IsMetric ? "mm" : "in";
                IsGrbl = !_grblViewModel.IsGrblHAL;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
