using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ioSenderTouch.Controls;
using System.Windows;
using System.Windows.Input;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Comands;
using ioSenderTouch.Utility;

namespace ioSenderTouch.ViewModels
{
    public class OffsetViewModel : ViewModelBase, IActiveViewModel
    {
        private CoordinateSystem _selectedOffset;
        private bool _awaitCoord = false;
        private Action<string> GotPosition;
        private GrblViewModel _grblViewModel;
        private bool _isPredefined;
        public bool Active { get; set; }
        public string Name { get; }

        public GrblViewModel GrblViewModel
        {
            get => _grblViewModel;
            set
            {
                if (Equals(value, _grblViewModel)) return;
                _grblViewModel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AxisLetter));
                OnPropertyChanged(nameof(Coordinates));
            }
        }
        public AxisLetter AxisLetter => GrblViewModel.AxisLetter;

        public CoordinateSystem Offset { get; private set; } = new CoordinateSystem
        {
            X = 0.000,
            Y = 0.000,
            Z = 0.000
        };
        public ObservableCollection<CoordinateSystem> Coordinates => GrblViewModel.CoordinateSystems;
        public CoordinateSystem SelectedOffset
        {
            get => _selectedOffset;
            set
            {
                if (Equals(value, _selectedOffset)) return;
                _selectedOffset = value;
                SelectionChanged();
                OnPropertyChanged();
            }
        }

        public bool IsPredefined
        {
            get => _isPredefined;
            set
            {
                if (value == _isPredefined) return;
                _isPredefined = value;
                OnPropertyChanged();
            }
        }
        public ICommand GetCurrPosCommand { get; }
        public ICommand SetOffsetCommand { get; }
        public ICommand SetAllCommand { get; }
        public ICommand ClearAllCommand { get; }
        public OffsetViewModel(GrblViewModel grblViewModel)
        {
            GrblViewModel = grblViewModel;
            Name = nameof(OffsetViewModel);
            GetCurrPosCommand = new Command(GetCurrPos);
            SetAllCommand = new Command(SetAll);
            ClearAllCommand = new Command(ClearAll);
            SetOffsetCommand = new Command(SetOffset);
        }
        public void Activated()
        {
            GrblViewModel.WorkPositionOffset.PropertyChanged += WorkPositionOffset_PropertyChanged;
        }
        public void Deactivated()
        {
            GrblViewModel.WorkPositionOffset.PropertyChanged -= WorkPositionOffset_PropertyChanged;
        }

        private void WorkPositionOffset_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_grblViewModel.MachinePosition):
                    _awaitCoord = !double.IsNaN(GrblViewModel.MachinePosition.Values[0]);
                    if (_awaitCoord)
                    {
                        Offset.Set(GrblViewModel.MachinePosition);
                        _awaitCoord = false;
                    }
                    break;
            }
        }

        private void SelectionChanged()
        {
            IsPredefined = SelectedOffset.Code == "G28" || SelectedOffset.Code == "G30";
            Offset.X = 0.000;
            Offset.Y = 0.000;
            Offset.Z = 0.000;
            Offset.Code = SelectedOffset.Code;
        }
        // G10 L1 P- axes <R- I- J- Q-> Set Tool Table
        // L10 - ref G5x + G92 - useful for probe (G38)
        // L11 - ref g59.3 only
        // Q: 1 - 8: 1: 135, 2: 45, 3: 315, 4: 225, 5: 180, 6: 90, 7: 0, 8: 270

        void SaveOffset(string axis)
        {
            string cmd;

            Position newpos = new Position(Offset);

            newpos.X = GrblWorkParameters.ConvertX(GrblWorkParameters.LatheMode, GrblParserState.LatheMode, SelectedOffset.X);

            if (SelectedOffset.Id == 0)
            {
                string code = SelectedOffset.Code == "G28" || SelectedOffset.Code == "G30" ? SelectedOffset.Code + ".1" : SelectedOffset.Code;

                if (axis == "ClearAll" || IsPredefined)
                    cmd = SelectedOffset.Code == "G43.1" ? "G49" : SelectedOffset.Code + ".1";
                else
                    cmd = string.Format("G90{0}{1}", code, newpos.ToString(axis == "All" ? GrblInfo.AxisFlags : GrblInfo.AxisLetterToFlag(axis)));
            }
            else
                cmd =
                    $"G90G10L2P{SelectedOffset.Id}{newpos.ToString(axis == "All" || axis == "ClearAll" ? GrblInfo.AxisFlags : GrblInfo.AxisLetterToFlag(axis))}";

            Comms.com.WriteCommand(cmd);
        }


        private void SetOffset(object e)
        {
            if (SelectedOffset != null)
            {
                string axisletter = e.ToString();
                int axis = GrblInfo.AxisLetterToIndex(axisletter);

                SelectedOffset.Values[axis] = Offset.Values[axis];
                SaveOffset(axisletter);
            }
        }

        void SetAll(object e)
        {
            if (SelectedOffset != null)
            {
                for (var i = 0; i < Offset.Values.Length; i++)
                    SelectedOffset.Values[i] = Offset.Values[i];

                SaveOffset("All");
            }
        }

        private void ClearAll(object e)
        {
            if (SelectedOffset != null)
            {
                for (var i = 0; i < Offset.Values.Length; i++)
                    Offset.Values[i] = SelectedOffset.Values[i] = 0d;

                SaveOffset("ClearAll");
            }
        }

        void GetCurrPos(object e)
        {
            _awaitCoord = true;
            _grblViewModel.Clear();
            _grblViewModel.MachinePosition.Clear();
            var t = RequestExtension.SendSettings(_grblViewModel, GrblLegacy.ConvertRTCommand(GrblConstants.CMD_STATUS_REPORT), "Mpos", InfoReceived);
        }

        public void InfoReceived()
        {
            if (!double.IsNaN(GrblViewModel.MachinePosition.Values[0]))
            {
                Offset.Set(GrblViewModel.MachinePosition);
            }
        }

    }
}



