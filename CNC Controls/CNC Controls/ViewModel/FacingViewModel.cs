using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using CNC.Core;

namespace CNC.Controls.ViewModel
{
    public class FacingViewModel :  INotifyPropertyChanged
    {
        private bool _usingInches;
        private string _unit;
        private string _unitPerMin;

        private const double Inches_To_MM = 25.4;
        public ICommand MyCommand { get; set; }
        
        public double ToolDiameter { get; set; }
        public double StockLength { get; set; }
        public double StockWidth { get; set; }
        public double DepthOfCut { get; set; }
        public int Passes { get; set; }
        public double FeedRate { get; set; }
        public double SpindleRpm { get; set; }

        public double OverLap { get; set; }

        public string Unit
        {
            get { return _unit; }
            set
            {
                if (_unit != value)
                {
                    _unit = value;
                    UnitPerMin = value;
                    OnPropertyChanged("Unit");
                }
            }
        }

        public string UnitPerMin
        {
            get { return _unitPerMin;}
            set
            {
                _unitPerMin = $"{value}/min";
                OnPropertyChanged("UnitPerMin");
            }
        }
        public bool UsingInches
        {
            get => _usingInches;
            set
            {
                if (_usingInches != value)
                {
                    _usingInches = value;
                    UpdateMeasurementUnit();
                }
            }
        }

        public FacingViewModel()
        {
            MyCommand = new RelayCommand(Executemethod, Canexecutemethod);
            
            Unit = "mm";
        }

        private void UpdateMeasurementUnit()
        {
            Unit = _usingInches ? "mm" : "inch";
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
           // var startOffset = 
            if (_usingInches)
            {
                width = width * Inches_To_MM; 
                length = length * Inches_To_MM;
                feedRate = feedRate * Inches_To_MM;
                dia = dia * Inches_To_MM;
            }

            var gcode = new GcodeFacingBuilder(width, length,feedRate,dia, numberOfPasses,depth, overlap);
        }


        private bool Canexecutemethod(object parameter)
        {
            return parameter != null;
        }

        private void Executemethod(object parameter)
        {
            if (!(parameter is string content)) return;
            switch (parameter)
            {
                case  "Generate":
                    GenerateGcode();
                    break;
                default:
                    SetupUnits(content);
                    break;
            }
        }

        private void SetupUnits(string unit)
        {
            UsingInches = string.Equals("Inches", unit, StringComparison.CurrentCultureIgnoreCase);
            Unit = UsingInches ? "in" : "mm";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }

    }
}
public class RelayCommand : ICommand
{

    Action<object> _executemethod;
    Func<object, bool> _canexecutemethod;

    public RelayCommand(Action<object> executemethod, Func<object, bool> canexecutemethod)
    {
        _executemethod = executemethod;
        _canexecutemethod = canexecutemethod;
    }


    public bool CanExecute(object parameter)
    {
        if (_canexecutemethod != null)
        {
            return _canexecutemethod(parameter);
        }
        else
        {
            return false;
        }
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public void Execute(object parameter)
    {
        _executemethod(parameter);
    }
}

public class GcodeFacingBuilder
{
    private const string End = "M30";
    private const string TurnOffFlood = "M9";
    private const string Flood = "M8";
    private const string TravelFeedRate = "G1";
    private double _feedRate;
    private double _toolDiamete;
    private double overLap;

    public List<Movement> Movements { get; set; }
    private double X;
    private double Y;
    private double width;
    private double length;
    private double feedRate;
    private double dia;
    private int numberOfPasses;
    private double depth;

    public GcodeFacingBuilder(double width)
    {
        GenerateGcode();
    }

    public GcodeFacingBuilder(double width, double length, double feedRate, double dia, int numberOfPasses, double depth, double overlap)
    {
        this.width = width;
        this.length = length;
        _feedRate = feedRate;
        _toolDiamete = dia;
        this.numberOfPasses = numberOfPasses;
        this.depth = depth;
        this.overLap = overlap;
    }

    private void GenerateGcode()
    {
        var lines = width / (dia / overLap);
    }
    
}
public class Movement
{
    private const string LinearTravel = "G01";
    private const string FeedRate = "F";
    private const string X = "X";
    private const string Y = "Y";
    public string LinearMovement { get; internal set; }

    public Movement(string x, string y, string feedRate)
    {
        LinearMovement = $"{LinearTravel} {x} {y} {FeedRate}{feedRate}";
    }
}