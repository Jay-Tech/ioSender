using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CNC.Controls;
using CNC.Controls.Probing;
using CNC.Controls.Viewer;
using CNC.Core;
using CNC.Core.Comands;
using ioSenderTouch.Views;

namespace ioSenderTouch.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {

        private bool? initOK = null;
        private bool isBooted = false;
        private GrblViewModel _grblViewModel;
        private Controller _controller = null;
        private ToolView _toolView;
        private RenderControl _renderView;
        private ProbingView _probeView;
        private SDCardView _sdView;
        private UserControl _view;
        private GrblConfigView _grblSettingView;
        private AppConfigView _grblAppSettings;
        private OffsetView _offsetView;
        private UtilityView _utilityView;

        public ICommand ChangeView { get; }
        public UserControl View
        {
            get => _view;
            set
            {
                if (Equals(value, _view)) return;
                _view = value;
                OnPropertyChanged();
            }
        }

        public HomeViewModel(GrblViewModel grblViewModel)
        {
            _grblViewModel = grblViewModel;
            Grbl.GrblViewModel = _grblViewModel;
            _renderView = new RenderControl(_grblViewModel);
            _grblSettingView = new GrblConfigView(_grblViewModel);
            _grblAppSettings = new AppConfigView(_grblViewModel);
            _offsetView = new OffsetView(_grblViewModel);
            _utilityView = new UtilityView(_grblViewModel);
            AppConfig.Settings.SetupAndOpen(_grblViewModel, Application.Current.Dispatcher);
            InitSystem();
            ChangeView = new Command(SetNewView);
           

        }

        
        private bool InitSystem()
        {
            initOK = true;
            int timeout = 5;
            _grblViewModel.Poller.SetState(0);
            using (new UIUtils.WaitCursor())
            {
                while (!GrblInfo.Get())
                {
                    if (--timeout == 0)
                    {
                        _grblViewModel.Message = ("MsgNoResponse");
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
                _grblViewModel.Poller.SetState(AppConfig.Settings.Base.PollInterval);

            }

            GrblCommand.ToolChange = GrblInfo.ManualToolChange ? "M61Q{0}" : "T{0}";


            if (_grblViewModel.HasSDCard)
            {
                _sdView = new SDCardView(_grblViewModel);
            }

            if (_grblViewModel.HasATC)
            {
                _toolView = new ToolView(_grblViewModel);
            }

            if (GrblInfo.HasProbe && GrblSettings.ReportProbeCoordinates)
            {
                _grblViewModel.HasProbing = true;
                _probeView = new ProbingView(_grblViewModel);
                _probeView.Activate(true);

            }
            return true;
        }


        private void Button_ClickSDView(object sender, RoutedEventArgs e)
        {
            //  FillBorder.Child = _sdView;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //FillBorder.Child = _grblSettingView;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // FillBorder.Child = _probeView;

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //FillBorder.Child = _renderView;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // FillBorder.Child = _grblAppSettings;

        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            // FillBorder.Child = _offsetView;
        }

        private void Button_Click_Utility(object sender, RoutedEventArgs e)
        {

        }
        private void Button_Click_Tools(object sender, RoutedEventArgs e)
        {
            // FillBorder.Child = _toolView;
        }



        public void SetNewView(object x)
        {

            switch (x.ToString())
            {

            }
        }
        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //if (CodeListControl.Visibility == Visibility.Visible)
            //{
            //    CodeListControl.Visibility = Visibility.Hidden;
            //    ConsoleControl.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    CodeListControl.Visibility = Visibility.Visible;
            //    ConsoleControl.Visibility = Visibility.Hidden;
            //}


            //btnShowConsole.Content = ConsoleControl.Visibility == Visibility.Hidden ? "Console" : "GCode Viewer";
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

