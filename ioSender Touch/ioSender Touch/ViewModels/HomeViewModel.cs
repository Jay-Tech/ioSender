﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ioSenderTouch.Controls;
using ioSenderTouch.Controls.Probing;
using ioSenderTouch.Controls.Render;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Comands;
using ioSenderTouch.GrblCore.Config;
using ioSenderTouch.Utility;
using ioSenderTouch.Views;



namespace ioSenderTouch.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {

        private bool? initOK = null;
        private bool isBooted = false;
        private GrblViewModel _model;
        private Controller _controller = null;
        private ToolView _toolView;
        private RenderControl _renderView;
        private ProbingView _probeView;
        private SDCardView _sdView;
        
        private GrblConfigView _grblSettingView;
        private AppConfigView _grblAppSettings;
        private OffsetView _offsetView;
        private UtilityView _utilityView;
        private readonly ContentManager _contentManager;
        private object _view;
        private string _consoleModeText;
        private bool _showGCodeConsole;
        public ICommand SwitchConsoleCommand { get; }
        public ICommand ChangeView { get; }

      
        public object View
        {
            get => _view;
            set
            {
                if (Equals(value, _view)) return;
                _view = value;
                OnPropertyChanged();
            }
        }

        public bool ShowGCodeConsole
        {
            get => _showGCodeConsole;
            set
            {
                if (value == _showGCodeConsole) return;
                _showGCodeConsole = value;
                OnPropertyChanged();
            }
        }

        public string ConsoleModeText
        {
            get => _consoleModeText;
            set
            {
                if (value == _consoleModeText) return;
                _consoleModeText = value;
                OnPropertyChanged();
            }
        }
        public HomeViewModel(GrblViewModel grblViewModel)
        {
            ChangeView = new Command(SetNewView);
            SwitchConsoleCommand = new Command(SwitchConsole);
            _model = grblViewModel;
            _contentManager = _model.ContentManager;
            _renderView = new RenderControl(_model);
            _grblSettingView = new GrblConfigView(_model);
            _grblAppSettings = new AppConfigView(_model);
            _offsetView = new OffsetView(_model);
            _utilityView = new UtilityView(_model);
          
            AppConfig.Settings.OnConfigFileLoaded += AppConfiguationLoaded;
            _controller = new Controller(_model, AppConfig.Settings);
            _controller.SetupAndOpen(Application.Current.Dispatcher);
            InitSystem();
            BuildOptionalUi();
            GCode.File.FileLoaded += File_FileLoaded;
            var gamepad = new HandController(_model);
            View = _renderView;
            ConsoleModeText = "Console";
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
            controls.Add(new AppUiSettings());
            if (AppConfig.Settings.JogMetric.Mode != JogConfig.JogMode.UI)
            {
                controls.Add(new JogConfigControl(_model));
            }
            controls.Add(new StripGCodeConfigControl());

            if (AppConfig.Settings.GCodeViewer.IsEnabled)
            {
                controls.Add(new RenderConfigControl());
            }
            _grblAppSettings.Setup(controls, AppConfig.Settings);
        }
        private void SwitchConsole(object obj)
        {
            ShowGCodeConsole = !ShowGCodeConsole;
            ConsoleModeText = ShowGCodeConsole ? "Console" : "GCode Viewer";
        }
        private void File_FileLoaded(object sender, bool fileLoaded)
        {
            ShowGCodeConsole = fileLoaded;
            ConsoleModeText = ShowGCodeConsole ? "Console" : "GCode Viewer";
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
            if (GrblInfo.HasProbe && GrblSettings.ReportProbeCoordinates)
            {
                _model.HasProbing = true;
                _probeView = new ProbingView(_model);
                _probeView.Activate(true);

            }
        }

        private bool InitSystem()
        {
            initOK = true;
            int timeout = 5;

            using (new UIUtils.WaitCursor())
            {
                _model.Poller.SetState(0);
                while (!GrblInfo.Get())
                {
                    if (--timeout == 0)
                    {
                        _model.Message = "Controller is not responding!";
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
                _model.Poller.SetState(AppConfig.Settings.Base.PollInterval);
            }

            return true;
        }

        private void SetNewView(object x)
        {
            switch (x.ToString())
            {
                case "offsetView":
                    View = _offsetView;
                    break;
                case "sdCardView":
                    View = _sdView;
                    break;
                case "grblSettingsView":
                    View = _grblSettingView;
                    break;
                case "appSettingsView":
                    View  = _grblAppSettings;
                    break;
                case "probeView":
                    View = _probeView;
                    break;
                case "utilityView":
                    View = _utilityView;
                    break;
                case "toolView":
                    View = _toolView;
                    break;
                case "renderView":
                    View = _renderView;
                    break;
                case "jobView":
                    View = _renderView;
                    break;
            }

            _contentManager.SetActiveUiElement(nameof(View));
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

