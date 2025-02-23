using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CNC.Core;
using CNC.Core.Config;
using ioSenderTouch.Controls;
using ioSenderTouch.Utility;
using ioSenderTouch.Views;
using MaterialDesignThemes.Wpf;



namespace ioSenderTouch
{
    public partial class MainWindow : Window
    {
        private const string Version = "1.0.1.6";
        private const string App_Name = "IO Sender Touch";

        private readonly GrblViewModel _viewModel;
        private readonly HomeView _homeView;
        private readonly HomeViewPortrait _homeViewPortrait;
        private bool _shown;
        private bool _windowStyle;
        private JogConfig _jogConfig;
        public string BaseWindowTitle { get; set; }
        public MainWindow()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config" + Path.DirectorySeparatorChar);
            CNC.Core.Resources.Path = path;
            InitializeComponent();
            Title = string.Format(Title, Version);
            _viewModel = DataContext as GrblViewModel ?? new GrblViewModel();
            BaseWindowTitle = Title;
            AppConfig.Settings.OnConfigFileLoaded += Settings_OnConfigFileLoaded;
            _viewModel.PropertyChanged += _viewModel_PropertyChanged;
            if (SystemInformation.ScreenOrientation == ScreenOrientation.Angle90 || SystemInformation.ScreenOrientation == ScreenOrientation.Angle270)
            {
                _homeViewPortrait = new HomeViewPortrait(_viewModel);
                DockPanel.SetDock(_homeViewPortrait, Dock.Left);
                DockPanel.Children.Add(_homeViewPortrait);
                MenuBorder.Child = new PortraitMenu();
                MenuBorder.DataContext = _viewModel;
            }
            else
            {
                _homeView = new HomeView(_viewModel);
                DockPanel.SetDock(_homeView, Dock.Left);
                DockPanel.Children.Add(_homeView);
                var menu = new LandScapeMenu
                {
                    VerisonLabel =
                    {
                        Content = $"{App_Name} {Version}"
                    }
                };
                MenuBorder.Child = menu;
                MenuBorder.DataContext = _viewModel;
            }
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Comms.com.DataReceived -= ((GrblViewModel)DataContext).DataReceived;

            using (new UIUtils.WaitCursor())
            {
                Comms.com.Close();
                AppConfig.Settings.Shutdown();
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (_shown)
                return;
            _shown = true;
            _viewModel.LoadComplete();
        }

        private void SetPrimaryColor(Color color)
        {
            try
            {
                PaletteHelper paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                theme.SetPrimaryColor(color);
                paletteHelper.SetTheme(theme);
            }
            catch (Exception)
            {

            }
        }
        private void Settings_OnConfigFileLoaded(object sender, EventArgs e)
        {
            _viewModel.DisplayMenuBar = AppConfig.Settings.AppUiSettings.EnableToolBar;
            CheckAndSetScale();
            var color = AppConfig.Settings.AppUiSettings.UIColor;
            SetPrimaryColor(color);
            Left = 0;
            Top = 0;
            SetUpKeyBoard();
        }



        private void _viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GrblViewModel.IsMetric))
            {
                SetUpKeyBoard();
            }
        }
        private void SetUpKeyBoard()
        {
            if (_jogConfig != null)
            {
                _jogConfig.PropertyChanged -= JogConfig_PropertyChanged;
            }
            _jogConfig = _viewModel.IsMetric ? AppConfig.Settings.JogMetric : AppConfig.Settings.JogImperial;
            _jogConfig.PropertyChanged += JogConfig_PropertyChanged;
            ApplyKeyboardJogging();
        }
        private void ApplyKeyboardJogging()
        {
            _viewModel.Keyboard.JogStepDistance = AppConfig.Settings.JogMetric.LinkStepJogToUI ? AppConfig.Settings.JogUiMetric.Distance0 : _jogConfig.StepDistance;
            _viewModel.Keyboard.JogDistances[(int)KeypressHandler.JogMode.Slow] = _jogConfig.SlowDistance;
            _viewModel.Keyboard.JogDistances[(int)KeypressHandler.JogMode.Fast] = _jogConfig.FastDistance;
            _viewModel.Keyboard.JogFeedrates[(int)KeypressHandler.JogMode.Step] = _jogConfig.StepFeedrate;
            _viewModel.Keyboard.JogFeedrates[(int)KeypressHandler.JogMode.Slow] = _jogConfig.SlowFeedrate;
            _viewModel.Keyboard.JogFeedrates[(int)KeypressHandler.JogMode.Fast] = _jogConfig.FastFeedrate;
            _viewModel.Keyboard.IsJoggingEnabled = _jogConfig.Mode != JogConfig.JogMode.UI;
        }

        private void JogConfig_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ApplyKeyboardJogging();
        }

        private void CheckAndSetScale()
        {
            _windowStyle = !AppConfig.Settings.AppUiSettings.EnableToolBar;
            var width = AppConfig.Settings.AppUiSettings.Width;
            var height = AppConfig.Settings.AppUiSettings.Height;
            var dpi = DPIProvider.GetDpiScale();
            var h = height / dpi.DpiX;
            var w = width / dpi.DpiY;
            if (SystemInformation.ScreenOrientation == ScreenOrientation.Angle90 ||
                SystemInformation.ScreenOrientation == ScreenOrientation.Angle270)
            {
                Width = h;
                Height = w;
            }
            else
            {
                Width = w;
                Height = h;
            }
           
        }

      

        private void Window_Load(object sender, EventArgs e)
        {

            System.Threading.Thread.Sleep(50);
            Comms.com.PurgeQueue();
            if (!string.IsNullOrEmpty(AppConfig.Settings.FileName))
            {
                // Delay loading until app is ready
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new System.Action(() =>
                {
                    GCode.File.Load(AppConfig.Settings.FileName);
                }));
            }
        }

        private void MenuBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_windowStyle) return;
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        // Dynamic Resize

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register(nameof(ScaleValue), typeof(double),
            typeof(MainWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));



        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            if (o is MainWindow mainWindow)
                return mainWindow.OnCoerceScaleValue((double)value);
            return value;
        }
        protected virtual void OnScaleValueChanged(double oldValue, double newValue) { }
        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is MainWindow mainWindow)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        

        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }

        private void MainGrid_SizeChanged(object sender, EventArgs e) => CalculateScale();

        private void CalculateScale()
        {
            double yScale;
            double xScale;
            if (SystemInformation.ScreenOrientation == ScreenOrientation.Angle90 || SystemInformation.ScreenOrientation == ScreenOrientation.Angle270)
            {
                 yScale = ActualHeight / 1920;
                 xScale = ActualWidth / 1080;
            }
            else
            {
                yScale = ActualHeight / 1080;
                xScale = ActualWidth / 1920;
            }
             
            double value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(AppMainWindow, value);
        }
    }
}
