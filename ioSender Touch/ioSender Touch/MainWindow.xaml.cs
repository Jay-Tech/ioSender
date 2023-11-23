using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using CNC.Controls;
using CNC.Core;
using ioSenderTouch.Controls;
using MaterialDesignThemes.Wpf;


namespace ioSenderTouch
{
    public partial class MainWindow : Window
    {
        private const string Version = "1.0.3";
        private const string App_Name = "IO Sender Touch";

        private readonly GrblViewModel _viewModel;
        private readonly HomeView _homeView;
        private readonly HomeViewPortrait _homeViewPortrait;
        private bool _shown;
        public string BaseWindowTitle { get; set; }
        public MainWindow()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"+Path.DirectorySeparatorChar);
            CNC.Core.Resources.Path = path;
            InitializeComponent();
           
            Title = string.Format(Title, Version);
            int res;
            //if ((res = AppConfig.Settings.SetupAndOpen(Title, (GrblViewModel)DataContext, App.Current.Dispatcher)) != 0)
            //    Environment.Exit(res);
            _viewModel = DataContext as GrblViewModel ?? new GrblViewModel();
            BaseWindowTitle = Title;
            AppConfig.Settings.OnConfigFileLoaded += Settings_OnConfigFileLoaded;

            if (SystemInformation.ScreenOrientation ==ScreenOrientation.Angle90)
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
                MenuBorder.Child =menu;
                MenuBorder.DataContext = _viewModel;
            }
            _viewModel.OnShutDown += _viewModel_OnShutDown;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (_shown)
                return;
            _shown = true;
            _viewModel.LoadComplete();
        }

        private  void SetPrimaryColor(Color color)
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
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            var color = AppConfig.Settings.AppUiSettings.UIColor;
            SetPrimaryColor(color);
        }

        private void _viewModel_OnShutDown(object sender, EventArgs e)
        {
            AppConfig.Settings.Shutdown();
            Close();
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
    }
}
