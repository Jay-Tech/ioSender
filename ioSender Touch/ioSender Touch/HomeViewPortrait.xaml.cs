
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CNC.Controls;
using CNC.Controls.Probing;
using CNC.Controls.Viewer;
using CNC.Core;
using ioSenderTouch.Views;
using ConfigControl = CNC.Controls.Probing.ConfigControl;

namespace ioSenderTouch
{
    
    public partial class HomeViewPortrait : UserControl
    {
        private bool? initOK = null;
        private bool isBooted = false;
        private IInputElement focusedControl = null;
        private Controller Controller = null;
        private readonly GrblViewModel _model;
        private readonly GrblConfigView _grblSettingView;
        private readonly AppConfigView _grblAppSettings;
        private ProbingView _probeView;
        private readonly RenderControl _renderView;
        private readonly OffsetView _offsetView;
        private SDCardView _sdView;
        private ToolView _toolView;
        private readonly UtilityView _utilityView;


        public HomeViewPortrait(GrblViewModel model)
        {
            _model = model;
            DataContext = _model;
            InitializeComponent();
            
            Grbl.GrblViewModel = _model;
            _renderView = new RenderControl(_model);
            _grblSettingView = new GrblConfigView(_model);
            _grblAppSettings = new AppConfigView(_model);
            _offsetView = new OffsetView(_model);
            _utilityView = new UtilityView(_model);
            FillBorder.Child = _renderView;
            AppConfig.Settings.OnConfigFileLoaded += AppConfiguationLoaded;
            AppConfig.Settings.SetupAndOpen(_model, Application.Current.Dispatcher);
            _model.PollingInterval = AppConfig.Settings.Base.PollInterval;
            InitSystem();
            BuildOptionalUi();
            GCode.File.FileLoaded += File_FileLoaded;
        }

        private void BuildOptionalUi()
        {
            if (_model.HasSDCard)
            {
                _sdView = new SDCardView(_model);
            }

            if (_model.HasATC)
            {
                _toolView = new ToolView(_model);
            }
        }


        private bool InitSystem()
        {
            initOK = true;
            int timeout = 5;

            using (new UIUtils.WaitCursor())
            {
                GCodeSender.EnablePolling(false);
                while (!GrblInfo.Get())
                {
                    if (--timeout == 0)
                    {
                        _model.Message = (string)FindResource("MsgNoResponse");
                        return false;
                    }
                    Thread.Sleep(500);
                }
                GrblAlarms.Get();
                GrblErrors.Get();
                GrblSettings.Load();
                if (GrblInfo.IsGrblHAL)
                {
                    GrblParserState.Get();
                    GrblWorkParameters.Get();
                }
                else
                    GrblParserState.Get(true);
                GCodeSender.EnablePolling(true);
            }

            GrblCommand.ToolChange = GrblInfo.ManualToolChange ? "M61Q{0}" : "T{0}";


            if (GrblInfo.HasProbe && GrblSettings.ReportProbeCoordinates)
            {
                _model.HasProbing = true;
                _probeView = new ProbingView(_model);
                _probeView.Activate(true, ViewType.Probing);

            }
            return true;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!(e.Handled = ProcessKeyPreview(e)))
            {
                if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                    Focus();

                base.OnPreviewKeyDown(e);
            }
        }
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (!(e.Handled = ProcessKeyPreview(e)))
                base.OnPreviewKeyDown(e);
        }

        protected bool ProcessKeyPreview(KeyEventArgs e)
        {
            return _model.Keyboard.ProcessKeypress(e, !(MdiControl.IsFocused || DRO.IsFocused || spindleControl.IsFocused || workParametersControl.IsFocused));
        }

        private void Button_ClickSDView(object sender, RoutedEventArgs e)
        {
            FillBorder.Child = _sdView;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LeftBorder.Visibility = Visibility.Collapsed;
            RightMenuBorder.Visibility = Visibility.Collapsed;
            FillBorder.Child = _grblSettingView;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            LeftBorder.Visibility = Visibility.Collapsed;
            RightMenuBorder.Visibility = Visibility.Collapsed;
            FillBorder.Child = _probeView;

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            LeftBorder.Visibility = Visibility.Visible;
            RightMenuBorder.Visibility = Visibility.Visible;
            FillBorder.Child = _renderView;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            LeftBorder.Visibility = Visibility.Collapsed;
            RightMenuBorder.Visibility = Visibility.Collapsed;
            FillBorder.Child = _grblAppSettings;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            LeftBorder.Visibility = Visibility.Collapsed;
            RightMenuBorder.Visibility = Visibility.Collapsed;
            FillBorder.Child = _offsetView;
        }

        private void Button_Click_Utility(object sender, RoutedEventArgs e)
        {
            LeftBorder.Visibility = Visibility.Collapsed;
            RightMenuBorder.Visibility = Visibility.Collapsed;
            FillBorder.Child = _utilityView;
        }
        private void Button_Click_Tools(object sender, RoutedEventArgs e)
        {
            LeftBorder.Visibility = Visibility.Collapsed;
            RightMenuBorder.Visibility = Visibility.Collapsed;
            FillBorder.Child = _toolView;
        }

        private void AppConfiguationLoaded(object sender, EventArgs e)
        {
            BuildOptionalUi();
            _model.PollingInterval = AppConfig.Settings.Base.PollInterval;
            var controls = new ObservableCollection<UserControl>();

            controls.Add(new BasicConfigControl());
            controls.Add(new ConfigControl());
            if (AppConfig.Settings.Jog.Mode != JogConfig.JogMode.Keypad)
            {
                controls.Add(new JogUiConfigControl());
            }

            if (AppConfig.Settings.Jog.Mode != JogConfig.JogMode.UI)
            {
                controls.Add(new JogConfigControl());
            }
            controls.Add(new StripGCodeConfigControl());
            if (AppConfig.Settings.GCodeViewer.IsEnabled)
            {
                controls.Add(new CNC.Controls.Viewer.ConfigControl());
            }
            _grblAppSettings.Setup(controls, AppConfig.Settings);

        }

        private void File_FileLoaded(object sender, bool fileLoaded)
        {
            if (fileLoaded) return;
            FileClosedEnableConsole();
        }

        private void FileClosedEnableConsole()
        {
            if (CodeListControl.Visibility == Visibility.Visible)
            {
                CodeListControl.Visibility = Visibility.Hidden;
                ConsoleControl.Visibility = Visibility.Visible;
            }

            btnShowConsole.Content = ConsoleControl.Visibility == Visibility.Hidden ? "Console" : "GCode Viewer";
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (CodeListControl.Visibility == Visibility.Visible)
            {
                CodeListControl.Visibility = Visibility.Hidden;
                ConsoleControl.Visibility = Visibility.Visible;
            }
            else
            {
                CodeListControl.Visibility = Visibility.Visible;
                ConsoleControl.Visibility = Visibility.Hidden;
            }


            btnShowConsole.Content = ConsoleControl.Visibility == Visibility.Hidden ? "Console" : "GCode Viewer";
        }
    }
}
