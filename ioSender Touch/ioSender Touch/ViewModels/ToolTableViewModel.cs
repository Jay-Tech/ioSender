using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using CNC.Converters;
using CNC.Core;
using CNC.Core.Comands;

namespace ioSenderTouch.ViewModels
{
    public class ToolTableViewModel: INotifyPropertyChanged
    {
        private readonly GrblViewModel _grblViewModel;
        private ObservableCollection<ToolData> _toolDataCollection;
        public ICommand GetPositionCommand { get; set; }
        public ObservableCollection<ToolData> ToolDataCollection
        {
            get => _toolDataCollection;
            set
            {
                if (Equals(value, _toolDataCollection)) return;
                _toolDataCollection = value;
                OnPropertyChanged();
                GetPositionCommand = new Command(GetPosition);
            }
        }

        private void GetPosition(object obj)
        {
            var pos = _grblViewModel.MachinePosition;
        }

        public ToolTableViewModel(GrblViewModel grblViewModel)
        {
            _grblViewModel = grblViewModel;
            var toolList = GrblWorkParameters.Tools;
            ToolDataCollection = new ObservableCollection<ToolData>();
            foreach (var tool in toolList)
            {
                ToolDataCollection.Add(new ToolData(tool.Name, tool.Code, tool.X, tool.Y, tool.Z));
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

public class ToolData
{
    public string Name { get; set; }
    public string Index { get; set; }
    public double PosX { get; set; }
    public double PosY { get; set; }
    public double PosZ { get; set; }
    public ToolData(string name, string index, double posX, double posY, double posZ)
    {
        Name = name;
        Index = index;
        PosX = posX;
        PosY = posY;
        PosZ = posZ;
    }
}