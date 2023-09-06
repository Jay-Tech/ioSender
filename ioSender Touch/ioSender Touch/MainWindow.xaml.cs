using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using CNC.Controls;
using CNC.Converters;
using CNC.Core;
using ioSenderTouch.Controls;


namespace ioSenderTouch
{
    public partial class MainWindow : Window
    {
        private const string Version = "0.0.5";
        private const string App_Name = "IO Sender Touch";
        public static MainWindow ui = null;
        public static UIViewModel UIViewModel { get; } = new UIViewModel();
        private readonly GrblViewModel _viewModel;
        private readonly HomeView _homeView;
        private readonly HomeViewPortrait _homeViewPortrait;

        public string BaseWindowTitle { get; set; }
        public bool JobRunning => _viewModel.IsJobRunning;
        public MainWindow()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"+Path.DirectorySeparatorChar);
            CNC.Core.Resources.Path = path;
            InitializeComponent();
            ui = this;
            Title = string.Format(Title, Version);
            int res;
            //if ((res = AppConfig.Settings.SetupAndOpen(Title, (GrblViewModel)DataContext, App.Current.Dispatcher)) != 0)
            //    Environment.Exit(res);
            _viewModel = DataContext as GrblViewModel ?? new GrblViewModel();
            BaseWindowTitle = Title;

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
            new PipeServer(App.Current.Dispatcher);
            PipeServer.FileTransfer += Pipe_FileTransfer;

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

            IGCodeConverter c = new Excellon2GCode();
            GCode.File.AddConverter(c.GetType(), c.FileType, c.FileExtensions);
            c = new HpglToGCode();
            GCode.File.AddConverter(c.GetType(), c.FileType, c.FileExtensions);
            GCode.File.AddTransformer(typeof(GCodeRotateViewModel), (string)FindResource("MenuRotate"), UIViewModel.TransformMenuItems);
            GCode.File.AddTransformer(typeof(ArcsToLines), (string)FindResource("MenuArcsToLines"), UIViewModel.TransformMenuItems);
            GCode.File.AddTransformer(typeof(GCodeCompress), (string)FindResource("MenuCompress"), UIViewModel.TransformMenuItems);
        }
  
        private void Pipe_FileTransfer(string filename)
        {
            if (!JobRunning)
                GCode.File.Load(filename);
        }

    }
}
