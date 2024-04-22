using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CNC.Core;

namespace CNC.Controls.ViewModels
{
    public class UIJoggingViewModel : INotifyPropertyChanged
    {
        private readonly GrblViewModel _grblViewModel;
        private bool _useImperial;
        private JogUIConfig _measurement = new JogUIConfig();
        private string _distance = string.Empty;

        public bool UseImperial
        {
            get => _useImperial;
            set
            {
                if (value == _useImperial) return;
                _useImperial = value;
                OnPropertyChanged();
            }
        }

        public string Distance
        {
            get => _distance;
            set
            {
                if (value == _distance) return;
                _distance = value;
                OnPropertyChanged();
            }
        }

        public JogUIConfig Measurement
        {
            get => _measurement;
            set
            {
                if (Equals(value, _measurement)) return;
                _measurement = value;
                OnPropertyChanged();
            }
        }

        public UIJoggingViewModel(GrblViewModel grblViewModel)
        {
            _grblViewModel = grblViewModel;
            _grblViewModel.PropertyChanged += _grblViewModel_PropertyChanged;
            UseImperial = !_grblViewModel.IsMetric;
            Measurement = _grblViewModel.IsMetric ? AppConfig.Settings.Base.JogUiMetric : AppConfig.Settings.Base.JogUiImperial;
            Distance = _grblViewModel.IsMetric ? "mm" : "in";

        }

        private void _grblViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Unit"))
            {
                UseImperial = !_grblViewModel.IsMetric;
                Measurement = _grblViewModel.IsMetric ? AppConfig.Settings.Base.JogUiMetric : AppConfig.Settings.Base.JogUiImperial;
                Distance = _grblViewModel.IsMetric ? "mm" : "in";
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
