using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CNC.Core;
using CNC.Core.Config;
using ioSenderTouch.Controls;
using ioSenderTouch.Controls.Probing;
using ioSenderTouch.Controls.Render;
using ioSenderTouch.Utility;


namespace ioSenderTouch.Views
{
    
    public partial class HomeViewPortrait : UserControl
    {
        private bool? initOK = null;
        private bool isBooted = false;
        private IInputElement focusedControl = null;
        private Controller _controller = null;
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
            _controller = new Controller(_model, AppConfig.Settings);
            _controller.SetupAndOpen(Application.Current.Dispatcher);
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

            if (_model.HasToolTable)
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
                _probeView.Activate(true);

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
            _model.PollingInterval = AppConfig.Settings.Base.PollInterval;
            var controls = new ObservableCollection<UserControl>();

            controls.Add(new BasicConfigControl());
            controls.Add(new ProbingConfigControl());

            if (AppConfig.Settings.JogMetric.Mode != JogConfig.JogMode.Keypad)
            {
                controls.Add(new JogUiConfigControl(_model));
            }

            if (AppConfig.Settings.JogMetric.Mode != JogConfig.JogMode.UI)
            {
                controls.Add(new JogConfigControl(_model));
            }
            controls.Add(new StripGCodeConfigControl());
            controls.Add(new AppUiSettings());
            if (AppConfig.Settings.GCodeViewer.IsEnabled)
            {
                controls.Add(new RenderConfigControl());
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
                ConsoleDisplayControl.Visibility = Visibility.Visible;
            }

            btnShowConsole.Content = ConsoleDisplayControl.Visibility == Visibility.Hidden ? "Console" : "GCode Viewer";
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (CodeListControl.Visibility == Visibility.Visible)
            {
                CodeListControl.Visibility = Visibility.Hidden;
                ConsoleDisplayControl.Visibility = Visibility.Visible;
            }
            else
            {
                CodeListControl.Visibility = Visibility.Visible;
                ConsoleDisplayControl.Visibility = Visibility.Hidden;
            }


            btnShowConsole.Content = ConsoleDisplayControl.Visibility == Visibility.Hidden ? "Console" : "GCode Viewer";
        }
    }
}
