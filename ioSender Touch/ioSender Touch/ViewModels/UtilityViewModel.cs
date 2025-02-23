
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using CNC.Core;
using CNC.Core.Comands;
using CNC.Core.Config;
using ioSenderTouch.Controls;
using ioSenderTouch.Utility;


namespace ioSenderTouch.ViewModels
{
    public class UtilityViewModel : INotifyPropertyChanged
    {
        private string _instructions =
            "-Load spindle with center point bit\n such 90 degrees chamfer bit or pointed dial.\n\r" +
            "-Zero WCS at lower left corner.\n\r" +
            "-Enter measurement of desired triangle.\r" +
            "The larger the triangle the increase in accuracy\r\n" +
            "-Note: Using low tac tape at point A, B and C\nmay increase visibility of imprint marks \n" +
            "smaller imprint marks using less Z depth will help increase accuracy\n" +
            "but may increase difficulty in measuring";

        private const string Metric = "Metric";
        private const string Inches = "Inches";

        private readonly GrblViewModel _grblViewModel;
        private UIElement _control;
        private readonly ErrorsAndAlarms _alarmAndError;
        private readonly MacroEditor _macroEditor;
        private readonly SurfacingControl _surfacingControl;
        private readonly CalibrationControl _xyCalibrationControl;


        private const double Inches_To_MM = 25.4;
        private const double Safe_Height = .12;
        private const double Z_Safe_Height_MM = 50.8;
        private const double Z_Safe_Height_IN = 50.8;
        private bool _usingInches;
        private string _unit;
        private string _unitPerMin;
        private bool _metric;
        private bool _inches;
        private string _measurement;
        private bool _mist;
        private bool _flood;
        private double _measureA;
        private double _lengthA;
        private double _lengthB;
        private double _depthCalibration;
        private double _measureB;
        private double _measureC;
        private double _feedRateCalibration;
        private string _measurementResults;
        private readonly CalibrationTriangle _calibrationTriangle;
        private readonly GCodeTriangle _triangleGcode;
        private bool _calibrationRan;
        private string _calibrationUnit;
        private string _calibrationUnitPerMin;
        private string _calibratioMeasurement;
        private bool _usingInchesCalibration;
        private Macro _probeZMacro;
        public ICommand ShowView { get; }
        public ICommand SurfacingCommand { get; set; }
        public ICommand CalibrationRunCommand { get; set; }
        public ICommand CalibrationResultsCommand { get; set; }

        public double ToolDiameter { get; set; }
        public double StockLength { get; set; }
        public double StockWidth { get; set; }
        public double DepthOfCut { get; set; }
        public int Passes { get; set; }
        public double FeedRate { get; set; }
        public double SpindleRpm { get; set; }
        public double OverLap { get; set; }
        public string FilePath { get; set; }

        public Triangle HypothesesTriangle { get; set; }

        public bool Flood
        {
            get => _flood;
            set
            {
                if (value == _flood) return;
                _flood = value;
                OnPropertyChanged();
            }
        }

        public bool Mist
        {
            get => _mist;
            set
            {
                if (value == _mist) return;
                _mist = value;
                OnPropertyChanged();
            }
        }

        public string Measurement
        {
            get => _measurement;
            set
            {
                if (_measurement != value)
                {
                    _measurement = value;
                    SetUnits(value);
                    OnPropertyChanged();
                }
            }
        }
       
        public string Unit
        {
            get => _unit;
            set
            {
                if (_unit != value)
                {
                    _unit = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UnitPerMin
        {
            get => _unitPerMin;
            set
            {
                _unitPerMin = $"{value}/min";
                OnPropertyChanged();
            }
        }

        public string Instructions
        {
            get => _instructions;
            set
            {
                if (value == _instructions) return;
                _instructions = value;
                OnPropertyChanged();
            }
        }
        public double LengthA
        {
            get => _lengthA;
            set
            {
                if (value.Equals(_lengthA)) return;
                _lengthA = value;
                OnPropertyChanged();
            }
        }

        public double LengthB
        {
            get => _lengthB;
            set
            {
                if (value.Equals(_lengthB)) return;
                _lengthB = value;
                OnPropertyChanged();
            }
        }

        public double DepthCalibration
        {
            get => _depthCalibration;
            set
            {
                if (value.Equals(_depthCalibration)) return;
                _depthCalibration = value;
                OnPropertyChanged();
            }
        }

        public double FeedRateCalibration
        {
            get => _feedRateCalibration;
            set
            {
                if (value.Equals(_feedRateCalibration)) return;
                _feedRateCalibration = value;
                OnPropertyChanged();
            }
        }

        public double MeasureA
        {
            get => _measureA;
            set
            {
                if (value.Equals(_measureA)) return;
                _measureA = value;
                OnPropertyChanged();
            }
        }

        public double MeasureB
        {
            get => _measureB;
            set
            {
                if (value.Equals(_measureB)) return;
                _measureB = value;
                OnPropertyChanged();
            }
        }

        public double MeasureC
        {
            get => _measureC;
            set
            {
                if (value.Equals(_measureC)) return;
                _measureC = value;
                OnPropertyChanged();
            }
        }

        public string MeasurementResults
        {
            get => _measurementResults;
            set
            {
                if (value == _measurementResults) return;
                _measurementResults = value;
                OnPropertyChanged();
            }
        }
        public bool CalibrationRan
        {
            get => _calibrationRan;
            set
            {
                if (value == _calibrationRan) return;
                _calibrationRan = value;
                OnPropertyChanged();
            }
        }
        public string CalibrationMeasurement
        {
            get => _calibratioMeasurement;
            set
            {
                if (_calibratioMeasurement != value)
                {
                    _calibratioMeasurement = value;
                    SetCalibrationUnits(value);
                    OnPropertyChanged();
                }
            }
        }
        public string CalibrationUnitPerMin
        {
            get => _calibrationUnitPerMin;
            set
            {
                
                if (value == _calibrationUnitPerMin) return;
                _calibrationUnitPerMin = $"{value}/min";
                OnPropertyChanged();
            }
        }

        public string CalibrationUnit
        {
            get => _calibrationUnit;
            set
            {
                if (value == _calibrationUnit) return;
                _calibrationUnit = value;
                OnPropertyChanged();
            }
        }
        public UtilityViewModel(GrblViewModel grblViewModel)
        {
            _grblViewModel = grblViewModel;
            ShowView = new Command(SetView);
            CalibrationResultsCommand = new Command(CreateCalibrationResults);
            CalibrationRunCommand = new Command(CreateCalibrationJob);
            _alarmAndError = new ErrorsAndAlarms("");
            _surfacingControl = new SurfacingControl();
            _xyCalibrationControl = new CalibrationControl();
            _macroEditor = new MacroEditor();
            SurfacingCommand = new Command(ExecuteMethod);
            Passes = 1;
            OverLap = 50;
            BuildProbeMacro();
            AppConfig.Settings.OnConfigFileLoaded += Settings_OnConfigFileLoaded;
            _calibrationTriangle = new CalibrationTriangle();
            _triangleGcode = new GCodeTriangle();
            CalibrationMeasurement = Metric;
            _grblViewModel.GrblUnitChanged += _grblViewModel_GrblUnitChanged;
        }

        private void _grblViewModel_GrblUnitChanged(object sender, Measurement e)
        {
            if (_grblViewModel.UtilityMacros.Contains(_probeZMacro))
            {
                _grblViewModel.UtilityMacros.Remove(_probeZMacro);
                BuildProbeMacro();
            }
        }

        private void BuildProbeMacro()
        {
            var unit = _grblViewModel.IsMetric ? "G21" : "G20";
            var distance = _grblViewModel.IsMetric ? "15" : ".5";
            var command = $"{unit}G91F100\nG38.3F100Z-{distance}\nG10L20P0Z0.000\nG0Z5.000";
            _probeZMacro = new Macro
            {
                Name = "Quick Z Probe",
                Code = command,
                Id = 1,
                ConfirmOnExecute = true,
            };
            _grblViewModel.UtilityMacros.Add(_probeZMacro);
        }

        private void Settings_OnConfigFileLoaded(object sender, EventArgs e)
        {
            var surface = AppConfig.Settings.Base.Surface;
            if (surface == null) return;
            PopulateUI(surface);
        }

        private void PopulateUI(SurfaceConfig config)
        {
            _usingInches = config.IsInches;
            Measurement = _usingInches ? Inches : Metric;
            Mist = config.Mist;
            Flood = config.Flood;
            SpindleRpm = config.SpindleRPM;
            Passes = config.Passes;
            FeedRate = config.FeedRate;
            OverLap = config.Overlap;
            DepthOfCut = config.Depth;
            StockLength = config.StockLength;
            StockWidth = config.StockWidth;
            ToolDiameter = config.TooDiameter;

            if (!File.Exists(config.FilePath)) return;
            FilePath = config.FilePath;
            BuildJobMacro(config);

        }
        private void SetView(object view)
        {
            switch (view.ToString())
            {
                case "Alarm":
                    Control = _alarmAndError;
                    break;
                case "Macro":
                    Control = _macroEditor;
                    break;
                case "Surfacing":
                    Control = _surfacingControl;
                    break;
                case "Calibration":
                    Control = _xyCalibrationControl;
                    break;
            }
        }
        public void BuildJobMacro(SurfaceConfig config)
        {
            _grblViewModel.UtilityMacros.Add(new Macro
            {
                Name = "Quick Surfacing",
                Code = config.FilePath,
                Id = 2,
                ConfirmOnExecute = true,
                isJob = true,
                Path = config.FilePath
            });
        }
        private void ExecuteMethod(object parameter)
        {
            GenerateGcode();
        }
        private void SetUnits(string unit)
        {
            _usingInches = unit.Equals("Inches");
            UpdateMeasurementUnit();
        }
        private void UpdateMeasurementUnit()
        {
            Unit = UnitPerMin = _usingInches ? "inch" : "mm";
        }
        private void GenerateGcode()
        {
            var width = StockWidth;
            var length = StockLength;
            var dia = ToolDiameter;
            var rpm = SpindleRpm;
            var feedRate = FeedRate;
            var numberOfPasses = Passes;
            var depth = DepthOfCut;
            var overlap = OverLap;
            var safeHeight = Safe_Height;
            var mist = Mist;
            var flood = Flood;
            if (_usingInches)
            {
                width *= Inches_To_MM;
                length *= Inches_To_MM;
                feedRate *= Inches_To_MM;
                dia *= Inches_To_MM;
                safeHeight = Safe_Height * Inches_To_MM;
                depth *= Inches_To_MM;
            }

            var surfaceGcode = new GcodeSurfacingBuilder(width, length, feedRate, dia, numberOfPasses, depth, overlap, rpm, safeHeight, mist, flood);
            var macro = new Macro
            {
                Name = "Quick Surfacing",
                Code = surfaceGcode.FilePath,
                Id = 2,
                ConfirmOnExecute = true,
                isJob = true,
                Path = surfaceGcode.FilePath
            };
            FilePath = surfaceGcode.FilePath;

            var hasMacro = _grblViewModel.UtilityMacros.Any(x => x.Name.Equals(macro.Name));
            if (hasMacro)
            {
                var foundMacro = _grblViewModel.UtilityMacros.First(x => x.Name.Equals(macro.Name));
                _grblViewModel.UtilityMacros.Remove(foundMacro);
            }
            _grblViewModel.UtilityMacros.Add(macro);
            SaveMacro();
        }

        private void SaveMacro()
        {
            AppConfig.Settings.Base.Surface = new SurfaceConfig
            {
                SpindleRPM = SpindleRpm,
                Passes = Passes,
                FeedRate = FeedRate,
                Depth = DepthOfCut,
                FilePath = FilePath,
                IsInches = _usingInches,
                Overlap = (int)OverLap,
                StockLength = StockLength,
                StockWidth = StockWidth,
                TooDiameter = ToolDiameter,
                Flood = Flood,
                Mist = Mist
            };
            AppConfig.Settings.Save();
        }

        public UIElement Control
        {
            get => _control;
            set
            {
                if (Equals(value, _control)) return;
                _control = value;
                OnPropertyChanged();
            }
        }

        private void RunJob(List<string> job)
        {
            try
            {
                var lineCount = job.Count - 1;
                var index = 1;
                var jobComplete = false;
                var cancellationToken = new CancellationToken();
              
                _grblViewModel.Poller.SetState(0);

                void ProcessSettings(string response)
                {
                    if (response == "ok")
                    {
                        SendLine(index);
                        index++;

                    }
                }

                void SendLine(int line)
                {
                    if (line <= lineCount)
                    {
                        var command = job[line];
                        Comms.com.WriteCommand(command);
                    }
                    else
                    {
                        jobComplete = true;
                    }

                }

                void ProcessJob(List<string> jobList)
                {
                    Comms.com.DataReceived -= ProcessSettings;
                    Comms.com.DataReceived += ProcessSettings;
                    Comms.com.WriteCommand(jobList[0]);
                    while (!jobComplete)
                    {
                        Thread.Sleep(50);
                    }
                    Comms.com.DataReceived -= ProcessSettings;
                    _grblViewModel.Poller.SetState(_grblViewModel.PollingInterval);
                }

                Task.Factory.StartNew(() => ProcessJob(job), cancellationToken);
            }
            catch (Exception ex)
            {
               
                _grblViewModel.Poller.SetState(200);
                Console.WriteLine(ex);
               
            }
        }

        private void SetCalibrationUnits(string unit)
        {
            _usingInchesCalibration = unit.Equals("Inches");
            UpdateCalibrationMeasurementUnit();
        }

        private void UpdateCalibrationMeasurementUnit()
        {
            CalibrationUnit = CalibrationUnitPerMin = _usingInchesCalibration ? "inch" : "mm";
        }

      

        private void CreateCalibrationJob(object x)
        {
            HypothesesTriangle = _calibrationTriangle.CalculateTriangle(LengthA, LengthB);
            double lengthA;
            double lengthB;
            double feedRate;
            double depth;
            double safeHeight;
            if (_usingInchesCalibration)
            {
                 lengthA = HypothesesTriangle.SideA * Inches_To_MM;
                 lengthB = HypothesesTriangle.SideB * Inches_To_MM;
                 feedRate = FeedRateCalibration * Inches_To_MM;
                 depth = DepthCalibration * Inches_To_MM;
                 safeHeight = Z_Safe_Height_IN;
            }
            else
            {
                 lengthA = HypothesesTriangle.SideA;
                 lengthB = HypothesesTriangle.SideB;
                 feedRate = FeedRateCalibration;
                 depth = DepthCalibration;
                 safeHeight = Z_Safe_Height_MM;
            }
            var gCodeJob = _triangleGcode.CreateJob(lengthA, lengthB, feedRate,
                 depth, safeHeight);
            RunJob(gCodeJob);
            CalibrationRan = true;
        }

        private void CreateCalibrationResults(object x)
        {
            MeasurementResults = string.Empty;
            var triangle = _calibrationTriangle.CalculateResults(MeasureA, MeasureB, MeasureC);
            var delta = _calibrationTriangle.CalculateDelta(triangle);
            if (_usingInchesCalibration)
            {
                delta /= Inches_To_MM;
            }
            delta = Math.Round(delta, 3);
            var leftDelta = delta * -1;
            var tolerance = _usingInchesCalibration ? .001 : .0254;
            if (Math.Abs(triangle.SideA - HypothesesTriangle.SideA) > tolerance)
            {
                MeasurementResults += "X axis measurements not equal\nCalibrate X steps\n\r";
            }
            if (Math.Abs(triangle.SideB - HypothesesTriangle.SideB) > tolerance)
            {
                MeasurementResults += "Y axis measurements not equal\nCalibrate Y steps\n\r";
            }
            var results = Math.Abs(HypothesesTriangle.SideC - triangle.SideC);
            var forMattedResults = Math.Round(results, 3);
            if (forMattedResults <= tolerance)
            {
                MeasurementResults += "Gantry is Square\n\r";
            }
            MeasurementResults += $"Move Left Axis: { leftDelta}\n or Right Axis: { delta}";
            MeasurementResults += _usingInchesCalibration ? " Inches" : " MM";
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
