/*
 * 
 * v0.43 / 2023-07-21 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2019-2023, Io Engineering (Terje Io)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

· Redistributions of source code must retain the above copyright notice, this
list of conditions and the following disclaimer.

· Redistributions in binary form must reproduce the above copyright notice, this
list of conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.

· Neither the name of the copyright holder nor the names of its contributors may
be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;


namespace ioSenderTouch.GrblCore.Config
{
    public class LibStrings
    {
        static ResourceDictionary resource = new ResourceDictionary();

        public static string FindResource(string key)
        {
            if (resource.Source == null)
                try
                {

                    // resource.Source = new Uri("pack://application:,,,/ioSenderTouch.GrblCore;Component/LibStrings.xaml", UriKind.Absolute);
                     resource.Source = new Uri("pack://application:,,,/GrblCore/LibStrings.xaml",
                        UriKind.Absolute);
                }
                catch
                {
                }

            return resource.Source == null || !resource.Contains(key) ? string.Empty : (string)resource[key];
        }
    }

    [Serializable]
    public class LatheConfig : ViewModelBase
    {
        private bool _isEnabled = false;
        private LatheMode _latheMode = LatheMode.Disabled;

        [XmlIgnore]
        public double ZDirFactor { get { return ZDirection == Direction.Negative ? -1d : 1d; } }

        [XmlIgnore]
        public LatheMode[] LatheModes { get { return (LatheMode[])Enum.GetValues(typeof(LatheMode)); } }

        [XmlIgnore]
        public Direction[] ZDirections { get { return (Direction[])Enum.GetValues(typeof(Direction)); } }

        [XmlIgnore]
        public bool IsEnabled { get { return _isEnabled; } set { _isEnabled = value; OnPropertyChanged(); } }

        public LatheMode XMode { get { return _latheMode; } set { _latheMode = value; IsEnabled = value != LatheMode.Disabled; } }
        public Direction ZDirection { get; set; } = Direction.Negative;
        public double PassDepthLast { get; set; } = 0.02d;
        public double FeedRate { get; set; } = 300d;
    }

    [Serializable]
    public class ProbeConfig : ViewModelBase
    {
        private bool _CheckProbeStatus = true;
        private bool _ValidateProbeConnected = false;

        public bool CheckProbeStatus { get { return _CheckProbeStatus; } set { _CheckProbeStatus = value; OnPropertyChanged(); } }
        public bool ValidateProbeConnected { get { return _ValidateProbeConnected; } set { _ValidateProbeConnected = value; OnPropertyChanged(); } }
    }

    [Serializable]
    public class CameraConfig : ViewModelBase
    {
        private string _camera = string.Empty;
        private double _xoffset = 0d, _yoffset = 0d;
        private int _guideScale = 10;
        private bool _moveToSpindle = false, _confirmMove = false;
        private CameraMoveMode _moveMode = CameraMoveMode.BothAxes;

        [XmlIgnore]
        internal bool IsDirty { get; set; } = false;

        [XmlIgnore]
        public CameraMoveMode[] MoveModes { get { return (CameraMoveMode[])Enum.GetValues(typeof(CameraMoveMode)); } }

        public string SelectedCamera { get { return _camera; } set { _camera = value; IsDirty = true; OnPropertyChanged(); } }
        public double XOffset { get { return _xoffset; } set { _xoffset = value; OnPropertyChanged(); } }
        public double YOffset { get { return _yoffset; } set { _yoffset = value; OnPropertyChanged(); } }
        public int GuideScale { get { return _guideScale; } set { _guideScale = value; IsDirty = true; OnPropertyChanged(); } }
        public bool InitialMoveToSpindle { get { return _moveToSpindle; } set { _moveToSpindle = value; IsDirty = true; OnPropertyChanged(); } }
        public bool ConfirmMove { get { return _confirmMove; } set { _confirmMove = value; IsDirty = true; OnPropertyChanged(); } }
        public CameraMoveMode MoveMode { get { return _moveMode; } set { _moveMode = value; OnPropertyChanged(); } }
    }
    [Serializable]
    public class SurfaceConfig : ViewModelBase
    {
        private string _filePath = string.Empty;
        private bool _isInches;
        private double _toolDiameter;
        private double _stockLength;
        private double _stockWidth;
        private double _depth;
        private int _overlap = 50;
        private int _passes = 1;
        private double _feedRate;
        private double _spindleRpm;
        private bool _mist;
        private bool _flood;

        public bool Flood
        {
            get => _flood;
            set
            {
                _flood = value;
                OnPropertyChanged();
            }
        }
        public bool Mist
        {
            get => _mist;
            set
            {
                _mist = value;
                OnPropertyChanged();
            }
        }

        public string FilePath { get { return _filePath; } set { _filePath = value; OnPropertyChanged(); } }
        public bool IsInches { get { return _isInches; } set { _isInches = value; OnPropertyChanged(); } }
        public double TooDiameter { get { return _toolDiameter; } set { _toolDiameter = value; OnPropertyChanged(); } }
        public double StockLength { get { return _stockLength; } set { _stockLength = value; OnPropertyChanged(); } }
        public double StockWidth { get { return _stockWidth; } set { _stockWidth = value; OnPropertyChanged(); } }
        public double Depth { get { return _depth; } set { _depth = value; OnPropertyChanged(); } }
        public int Overlap { get { return _overlap; } set { _overlap = value; OnPropertyChanged(); } }
        public int Passes { get { return _passes; } set { _passes = value; OnPropertyChanged(); } }
        public double FeedRate { get { return _feedRate; } set { _feedRate = value; OnPropertyChanged(); } }
        public double SpindleRPM { get { return _spindleRpm; } set { _spindleRpm = value; OnPropertyChanged(); } }
    }

    [Serializable]
    public class GCodeViewerConfig : ViewModelBase
    {
        private bool _isEnabled = true;
        private int _arcResolution = 10;
        private double _minDistance = 0.05d, _toolDiameter = 3d;
        private bool _showGrid = true, _showAxes = true, _showBoundingBox = false, _showViewCube = true, _showCoordSystem = false, _showWorkEnvelope = false;
        private bool _showTextOverlay = false, _renderExecuted = false, _blackBackground = false, _scaleTool = true;
        Color _cutMotion = Colors.Black, _rapidMotion = Colors.LightPink, _retractMotion = Colors.Green, _toolOrigin = Colors.Green, _grid = Colors.Gray, _highlight = Colors.Crimson;

        [XmlIgnore]
        public bool IsHomingEnabled { get { return _isEnabled && GrblInfo.HomingEnabled; } set { OnPropertyChanged(); } }

        public bool IsEnabled { get { return _isEnabled; } set { _isEnabled = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsHomingEnabled)); } }
        public int ArcResolution { get { return _arcResolution; } set { _arcResolution = value; OnPropertyChanged(); } }
        public double MinDistance { get { return _minDistance; } set { _minDistance = value; OnPropertyChanged(); } }
        public bool ToolAutoScale { get { return _scaleTool; } set { _scaleTool = value; OnPropertyChanged(); } }
        public double ToolDiameter { get { return _toolDiameter; } set { _toolDiameter = value; OnPropertyChanged(); } }
        public bool ShowGrid { get { return _showGrid; } set { _showGrid = value; OnPropertyChanged(); } }
        public bool ShowAxes { get { return _showAxes; } set { _showAxes = value; OnPropertyChanged(); } }
        public bool ShowBoundingBox { get { return _showBoundingBox; } set { _showBoundingBox = value; OnPropertyChanged(); } }
        public bool ShowWorkEnvelope { get { return _showWorkEnvelope && GrblInfo.HomingEnabled; } set { _showWorkEnvelope = value; OnPropertyChanged(); } }
        public bool ShowViewCube { get { return _showViewCube; } set { _showViewCube = value; OnPropertyChanged(); } }
        public bool ShowTextOverlay { get { return _showTextOverlay; } set { _showTextOverlay = value; OnPropertyChanged(); } }
        public bool ShowCoordinateSystem { get { return _showCoordSystem; } set { _showCoordSystem = value; OnPropertyChanged(); } }
        public bool RenderExecuted { get { return _renderExecuted; } set { _renderExecuted = value; OnPropertyChanged(); } }
        public bool BlackBackground { get { return _blackBackground; } set { _blackBackground = value; OnPropertyChanged(); } }
        public Color CutMotionColor { get { return _cutMotion; } set { _cutMotion = value; OnPropertyChanged(); } }
        public Color RapidMotionColor { get { return _rapidMotion; } set { _rapidMotion = value; OnPropertyChanged(); } }
        public Color RetractMotionColor { get { return _retractMotion; } set { _retractMotion = value; OnPropertyChanged(); } }
        public Color ToolOriginColor { get { return _toolOrigin; } set { _toolOrigin = value; OnPropertyChanged(); } }
        public Color GridColor { get { return _grid; } set { _grid = value; OnPropertyChanged(); } }
        public Color HighlightColor { get { return _highlight; } set { _highlight = value; OnPropertyChanged(); } }
        public int ViewMode { get; set; } = -1;
        public int ToolVisualizer { get; set; } = 1;
        public Point3D CameraPosition { get; set; }
        public Vector3D CameraLookDirection { get; set; }
        public Vector3D CameraUpDirection { get; set; }
    }

    [Serializable]
    public class JogUIConfig : ViewModelBase
    {
        private int[] _feedrate = new int[4];
        private double[] _distance = new double[4];

        public JogUIConfig()
        {
        }

        public JogUIConfig(int[] feedrate, double[] distance)
        {
            for (int i = 0; i < feedrate.Length; i++)
            {
                _feedrate[i] = feedrate[i];
                _distance[i] = distance[i];
            }
        }

        [XmlIgnore]
        public int[] Feedrate { get { return _feedrate; } }
        public int Feedrate0 { get { return _feedrate[0]; } set { _feedrate[0] = value; OnPropertyChanged(); } }
        public int Feedrate1 { get { return _feedrate[1]; } set { _feedrate[1] = value; OnPropertyChanged(); } }
        public int Feedrate2 { get { return _feedrate[2]; } set { _feedrate[2] = value; OnPropertyChanged(); } }
        public int Feedrate3 { get { return _feedrate[3]; } set { _feedrate[3] = value; OnPropertyChanged(); } }

        [XmlIgnore]
        public double[] Distance { get { return _distance; } }
        public double Distance0 { get { return _distance[0]; } set { _distance[0] = value; OnPropertyChanged(); } }
        public double Distance1 { get { return _distance[1]; } set { _distance[1] = value; OnPropertyChanged(); } }
        public double Distance2 { get { return _distance[2]; } set { _distance[2] = value; OnPropertyChanged(); } }
        public double Distance3 { get { return _distance[3]; } set { _distance[3] = value; OnPropertyChanged(); } }
    }
    [Serializable]
    public class AppUiSettingsConfig : ViewModelBase
    {
        private bool _enableToolBar;
        private bool _enableStopLightTheme;
        private Color _uIColor;
        private int _width = 1920;
        private int _height = 1080;
        private bool _enableLightTheme;


        public bool EnableToolBar
        {
            get => _enableToolBar;
            set
            {
                if (value == _enableToolBar) return;
                _enableToolBar = value;
                OnPropertyChanged();
            }
        }

        public bool EnableLightTheme
        {
            get => _enableLightTheme;
            set
            {
                if (value == _enableLightTheme) return;
                _enableLightTheme = value;
                OnPropertyChanged();
            }
        }
        public int Width
        {
            get => _width;
            set
            {
                if (value == _width) return;
                _width = value;
                OnPropertyChanged();
            }
        }
        public int Height
        {
            get => _height;
            set
            {
                if (value == _height) return;
                _height = value;
                OnPropertyChanged();
            }
        }


        public bool EnableStopLightTheme
        {
            get => _enableStopLightTheme;
            set
            {
                if (value == _enableStopLightTheme) return;
                _enableStopLightTheme = value;
                OnPropertyChanged();
            }
        }

        public Color UIColor
        {
            get => _uIColor;
            set
            {
                if (value == _uIColor) return;
                _uIColor = value;
                OnPropertyChanged();
            }
        }
    }

    [Serializable]
    public class JogConfig : ViewModelBase
    {
        public enum JogMode : int
        {
            UI = 0,
            Keypad,
            KeypadAndUI
        }

        private bool _kbEnable, _linkStepToUi = true;
        private JogMode _jogMode = JogMode.UI;

        private double _fastFeedrate = 500d, _slowFeedrate = 200d, _stepFeedrate = 100d;
        private double _fastDistance = 500d, _slowDistance = 500d, _stepDistance = 0.05d;

        public JogMode Mode { get { return _jogMode; } set { _jogMode = value; OnPropertyChanged(); } }
        public bool KeyboardEnable { get { return _kbEnable; } set { _kbEnable = value; OnPropertyChanged(); } }
        public bool LinkStepJogToUI { get { return _linkStepToUi; } set { _linkStepToUi = value; OnPropertyChanged(); } }
        public double FastFeedrate { get { return _fastFeedrate; } set { _fastFeedrate = value; OnPropertyChanged(); } }
        public double SlowFeedrate { get { return _slowFeedrate; } set { _slowFeedrate = value; OnPropertyChanged(); } }
        public double StepFeedrate { get { return _stepFeedrate; } set { _stepFeedrate = value; OnPropertyChanged(); } }
        public double FastDistance { get { return _fastDistance; } set { _fastDistance = value; OnPropertyChanged(); } }
        public double SlowDistance { get { return _slowDistance; } set { _slowDistance = value; OnPropertyChanged(); } }
        public double StepDistance { get { return _stepDistance; } set { _stepDistance = value; OnPropertyChanged(); } }
    }

    [Serializable]
    public class Macros : ViewModelBase
    {
        public ObservableCollection<Macro> Macro { get; private set; } = new ObservableCollection<Macro>();
    }

    [Serializable]
    public class Config : ViewModelBase
    {
        private int _pollInterval = 200, /* ms*/  _maxBufferSize = 300;
        private bool _useBuffering = false, _keepMdiFocus = true, _filterOkResponse = false, _saveWindowSize = false, _autoCompress = false;
        private GCodeParser.CommandIgnoreState _ignoreM6 = GCodeParser.CommandIgnoreState.No, _ignoreM7 = GCodeParser.CommandIgnoreState.No, _ignoreM8 = GCodeParser.CommandIgnoreState.No, _ignoreG61G64 = GCodeParser.CommandIgnoreState.Strip;
        private string _theme = "default";


        public int PollInterval { get { return _pollInterval < 100 ? 100 : _pollInterval; } set { _pollInterval = value; OnPropertyChanged(); } }
        public string PortParams { get; set; } = "COMn:115200,N,8,1";
        public int ResetDelay { get; set; } = 2000;
        public bool UseBuffering { get { return _useBuffering; } set { _useBuffering = value; OnPropertyChanged(); } }
        public bool KeepWindowSize { get { return _saveWindowSize; } set { if (_saveWindowSize != value) { _saveWindowSize = value; OnPropertyChanged(); } } }
        public double WindowWidth { get; set; } = 925;
        public double WindowHeight { get; set; } = 660;
        public int OutlineFeedRate { get; set; } = 500;
        public int MaxBufferSize { get { return _maxBufferSize < 300 ? 300 : _maxBufferSize; } set { _maxBufferSize = value; OnPropertyChanged(); } }
        public string Editor { get; set; } = "notepad.exe";
        public bool KeepMdiFocus { get { return _keepMdiFocus; } set { _keepMdiFocus = value; OnPropertyChanged(); } }
        public bool FilterOkResponse { get { return _filterOkResponse; } set { _filterOkResponse = value; OnPropertyChanged(); } }
        public bool AutoCompress { get { return _autoCompress; } set { _autoCompress = value; OnPropertyChanged(); } }

        [XmlIgnore]
        public GCodeParser.CommandIgnoreState[] CommandIgnoreStates { get { return (GCodeParser.CommandIgnoreState[])Enum.GetValues(typeof(GCodeParser.CommandIgnoreState)); } }
        public GCodeParser.CommandIgnoreState IgnoreM6 { get { return _ignoreM6; } set { _ignoreM6 = value; OnPropertyChanged(); } }
        public GCodeParser.CommandIgnoreState IgnoreM7 { get { return _ignoreM7; } set { _ignoreM7 = value; OnPropertyChanged(); } }
        public GCodeParser.CommandIgnoreState IgnoreM8 { get { return _ignoreM8; } set { _ignoreM8 = value; OnPropertyChanged(); } }
        public GCodeParser.CommandIgnoreState IgnoreG61G64 { get { return _ignoreG61G64; } set { _ignoreG61G64 = value; OnPropertyChanged(); } }
        public ObservableCollection<Macro> Macros { get; set; } = new ObservableCollection<Macro>();
        public JogConfig JogMetric { get; set; } = new JogConfig();
        public JogConfig JogImperial { get; set; } = new JogConfig();
        public JogUIConfig JogUiMetric { get; set; } = new JogUIConfig(new int[4] { 5, 100, 500, 1000 }, new double[4] { .01d, .1d, 1d, 10d });
        public JogUIConfig JogUiImperial { get; set; } = new JogUIConfig(new int[4] { 5, 10, 50, 100 }, new double[4] { .001d, .01d, .1d, 1d });

        public LatheConfig Lathe { get; set; } = new LatheConfig();
        public CameraConfig Camera { get; set; } = new CameraConfig();
        public GCodeViewerConfig GCodeViewer { get; set; } = new GCodeViewerConfig();
        public ProbeConfig Probing { get; set; } = new ProbeConfig();
        public SurfaceConfig Surface { get; set; } = new SurfaceConfig();

        public AppUiSettingsConfig AppUISettings { get; set; } = new AppUiSettingsConfig();
    }

    public class AppConfig : ViewModelBase
    {
        public event EventHandler OnConfigFileLoaded;
        private string configfile = null;
        public bool? MPGactive = null;

        public string FileName { get;  set; }

        private static readonly Lazy<AppConfig> settings = new Lazy<AppConfig>(() => new AppConfig());
        private Config _base;

        private AppConfig()
        {

        }

        public static AppConfig Settings
        {
            get { return settings.Value; }
        }


        public Config Base
        {
            get { return _base; }
            private set { _base = value; }
        }

        public ObservableCollection<Macro> Macros
        {
            get { return Base == null ? null : Base.Macros; }
        }

        public JogConfig JogMetric
        {
            get { return Base == null ? null : Base.JogMetric; }
        }

        public JogConfig JogImperial
        {
            get { return Base == null ? null : Base.JogMetric; }
        }

        public JogUIConfig JogUiMetric
        {
            get { return Base == null ? null : Base.JogUiMetric; }
        }

        public JogUIConfig JogUiImperial
        {
            get { return Base == null ? null : Base.JogUiImperial; }
        }

        public CameraConfig Camera
        {
            get { return Base == null ? null : Base.Camera; }
        }

        public LatheConfig Lathe
        {
            get { return Base == null ? null : Base.Lathe; }
        }

        public GCodeViewerConfig GCodeViewer
        {
            get { return Base == null ? null : Base.GCodeViewer; }
        }

        public ProbeConfig Probing
        {
            get { return Base == null ? null : Base.Probing; }
        }

        public SurfaceConfig Surface
        {
            get { return Base == null ? null : Base.Surface; }
        }

        public AppUiSettingsConfig AppUiSettings
        {
            get { return Base == null ? null : Base.AppUISettings; }
        }

        public bool Save(string filename)
        {
            bool ok = false;

            if (Base == null)
                Base = new Config();

            XmlSerializer xs = new XmlSerializer(typeof(Config));

            try
            {
                FileStream fsout = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                using (fsout)
                {
                    xs.Serialize(fsout, Base);
                    configfile = filename;
                    ok = true;
                }
            }
            catch
            {
            }

            return ok;
        }

        public bool Save()
        {
            Camera.IsDirty = false;
            return configfile != null && Save(configfile);
        }

        public bool Load(string filename)
        {
            bool ok = false;
            XmlSerializer xs = new XmlSerializer(typeof(Config));

            try
            {
                StreamReader reader = new StreamReader(filename);
                Base = (Config)xs.Deserialize(reader);
                reader.Close();
                configfile = filename;

                // temp hack...
                foreach (var macro in Base.Macros)
                {
                    if (macro.IsSession)
                        Base.Macros.Remove(macro);
                }

                ok = true;
                OnConfigFileLoaded?.Invoke(this, null);
            }
            catch
            {
            }

            return ok;
        }

        public void CallFileLoaded()
        {
            OnConfigFileLoaded?.Invoke(this, null);
        }
        // Move me to separate File
        public void Shutdown()
        {
            if (Camera.IsDirty)
                Save();
        }

    }
}
