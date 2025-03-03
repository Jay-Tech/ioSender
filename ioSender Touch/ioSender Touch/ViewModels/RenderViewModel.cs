using System;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ioSenderTouch.Controls;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Comands;
using ioSenderTouch.GrblCore.Config;

namespace ioSenderTouch.ViewModels
{
    public class RenderViewModel : ViewModelBase, IActiveViewModel
    {

        private static bool keyboardMappingsOk = false;
        private int serialSize = 300;
        private bool _useBuffering = false;
        private GrblState grblState;
        private GrblViewModel model;
        private JobData job;
        private int missed = 0;
        private StreamingState _streamingState = StreamingState.NoFile;
        private bool _enableStart;
        private bool _enableHold;
        private bool _enableStop;
        private bool _enableRewind;
        private bool _showOverlay;
        private SolidColorBrush _foregroundColor;
        private bool _sdCardFileLoaded;

        public bool Active { get; set; }
        public string Name { get; }

        public bool JobPending => GCode.File.IsLoaded && !JobTimer.IsRunning;

        internal StreamingState StreamingState
        {
            get => _streamingState;
            set
            {
                if (_streamingState != value)
                {
                    _streamingState = value;
                    model.StreamingState = value;
                }
            }
        }

        public bool EnableStart
        {
            get => _enableStart;
            set
            {
                if (value == _enableStart) return;
                _enableStart = value;
                OnPropertyChanged();
            }
        }
        public bool EnableHold
        {
            get => _enableHold;
            set
            {
                if (value == _enableHold) return;
                _enableHold = value;
                OnPropertyChanged();
            }
        }
        public bool EnableStop
        {
            get => _enableStop;
            set
            {
                if (value == _enableStop) return;
                _enableStop = value;
                OnPropertyChanged();
            }
        }
        public bool EnableRewind
        {
            get => _enableRewind;
            set
            {
                if (value == _enableRewind) return;
                _enableRewind = value;
                OnPropertyChanged();
            }
        }
        public bool SdCardFileLoaded
        {
            get => _sdCardFileLoaded;
            set
            {
                if (value == _sdCardFileLoaded) return;
                _sdCardFileLoaded = value;
                OnPropertyChanged();
            }
        }
        public bool ShowOverlay
        {
            get => _showOverlay;
            set
            {
                if (value == _showOverlay) return;
                _showOverlay = value;
                OnPropertyChanged();
            }
        }
        public SolidColorBrush ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (Equals(value, _foregroundColor)) return;
                _foregroundColor = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled { get; set; }
        public ICommand StartJobCommand { get; }
        public ICommand HoldJobCommand { get; }
        public ICommand StopJobCommand { get; }
        public ICommand RewindJobCommand { get; }
        public RenderViewModel(GrblViewModel grblViewmodel)
        {
            Name = nameof(RenderViewModel);
            AppConfig.Settings.OnConfigFileLoaded += AppConfigurationLoaded;
            StartJobCommand = new Command(StartJob);
            HoldJobCommand = new Command(PauseJob);
            StopJobCommand = new Command(StopJob);
            RewindJobCommand = new Command(RewindJob);
            model = grblViewmodel;
            grblState.State = GrblStates.Unknown;
            grblState.Substate = 0;
            grblState.MPG = false;
            job.PgmEndLine = -1;
            SetInitialState();
            model.GrblInitialized += Model_GrblInitialized;
        }

        private void Model_GrblInitialized(object sender, EventArgs e)
        {
            serialSize = Math.Min(AppConfig.Settings.Base.MaxBufferSize, GrblInfo.SerialBufferSize);
            model.PropertyChanged += ViewModelPropertyChange;
        }

        private void SetInitialState()
        {
            if (grblState.State is GrblStates.Idle or GrblStates.Unknown)
            {
                StreamingState = StreamingState.Idle;
                EnableStart = EnableHold && GCode.File.IsLoaded;
                EnableHold = !(grblState.MPG || grblState.State == GrblStates.Alarm);
                EnableStop = false;
                EnableRewind = false;
            }
        }
        private void StartJob(object x)
        {
            CycleStart();
        }

        private void PauseJob(object obj)
        {
            Comms.com.WriteByte(GrblLegacy.ConvertRTCommand(GrblConstants.CMD_FEED_HOLD));
        }
        private void StopJob(object x)
        {
            if (job.State == JobState.Running && StreamingState == StreamingState.FeedHold || job.State == JobState.Error)
            {
                ActiveJobStop();
            }
            else if(job.IsSDFile && model.GrblState.State == GrblStates.Run )
            {
                StopSdCardJob();
            }
            else
            {
                Comms.com.WriteByte(GrblConstants.CMD_STOP);
                //ActiveJobStop();
            }
        }
        private void RewindJob(object obj)
        {
            RewindFile();
        }

        private void AppConfigurationLoaded(object sender, EventArgs e)
        {
            ShowOverlay = AppConfig.Settings.GCodeViewer.ShowTextOverlay;
            ForegroundColor = AppConfig.Settings.GCodeViewer.BlackBackground ?
                Brushes.White : Brushes.Black;
            var uiSettings = AppConfig.Settings.AppUiSettings;
            if (uiSettings.EnableStopLightTheme)
            {
                //btnStart.Background = Brushes.Green;
                //btnHold.Background = Brushes.Yellow;
                //btnStop.Background = Brushes.DarkRed;
            }
            _useBuffering = AppConfig.Settings.Base.UseBuffering;
            AppConfig.Settings.GCodeViewer.PropertyChanged += AppUISettings_PropertyChanged;
            ProcessKeyMappings();
        }

        private void AppUISettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GCodeViewerConfig.ShowTextOverlay))
            {
                ShowOverlay = AppConfig.Settings.GCodeViewer.ShowTextOverlay;
            }
        }

        public void Activated()
        {
            
            model.OnRealtimeStatusProcessed -= RealtimeStatusProcessed;
            model.OnCommandResponseReceived -= ResponseReceived;
           
            model.OnRealtimeStatusProcessed += RealtimeStatusProcessed;
            model.OnCommandResponseReceived += ResponseReceived;
        }

        public void Deactivated()
        {
            model.OnRealtimeStatusProcessed -= RealtimeStatusProcessed;
            model.OnCommandResponseReceived -= ResponseReceived;
        }

        private void ProcessKeyMappings()
        {
            AppConfig.Settings.Base.PropertyChanged += Base_PropertyChanged;
            GCodeParser.IgnoreM6 = AppConfig.Settings.Base.IgnoreM6;
            GCodeParser.IgnoreM7 = AppConfig.Settings.Base.IgnoreM7;
            GCodeParser.IgnoreM8 = AppConfig.Settings.Base.IgnoreM8;
        }
        private void Base_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GCodeParser.IgnoreM6 = AppConfig.Settings.Base.IgnoreM6;
            GCodeParser.IgnoreM7 = AppConfig.Settings.Base.IgnoreM7;
            GCodeParser.IgnoreM8 = AppConfig.Settings.Base.IgnoreM8;
            GCodeParser.IgnoreG61G64 = AppConfig.Settings.Base.IgnoreG61G64;
        }
        private void RealtimeStatusProcessed(string response)
        {
            if (JobTimer.IsRunning && !JobTimer.IsPaused)
                model.RunTime = JobTimer.RunTime;
        }

        private void ViewModelPropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (sender is GrblViewModel vm)
                switch (e.PropertyName)
                {
                    case nameof(GrblViewModel.GrblState):
                        GrblStateChanged(vm.GrblState);
                        break;

                    case nameof(GrblViewModel.MDI):
                        SendCommand(vm.MDI);
                        break;

                    case nameof(GrblViewModel.IsMPGActive):
                        grblState.MPG = vm.IsMPGActive == true;
                        vm.Poller.SetState(grblState.MPG ? 0 : AppConfig.Settings.Base.PollInterval);
                        StreamingState = grblState.MPG ? StreamingState.Disabled : StreamingState.Idle;
                        break;

                    case nameof(GrblViewModel.Signals):
                        if (Active)
                        {
                            var signals = vm.Signals.Value;
                            if (JobPending && signals.HasFlag(Signals.CycleStart) && !signals.HasFlag(Signals.Hold))
                                CycleStart();
                        }
                        break;
                    case nameof(GrblViewModel.ProgramEnd):
                        if (!GCode.File.IsLoaded)
                            StreamingState = model.IsSDCardJob ? StreamingState.JobFinished : StreamingState.NoFile;
                        else if (JobTimer.IsRunning && job.State != JobState.Competed)
                            //Comms.com.WriteByte(GrblConstants.CMD_STOP);
                            StreamingState = StreamingState.JobFinished;
                        if (!model.IsParserStateLive)
                            SendCommand(GrblConstants.CMD_GETPARSERSTATE);
                        break;

                    case nameof(GrblViewModel.FileName):
                        {
                            job.IsSDFile = false;
                            if (string.IsNullOrEmpty(vm.FileName))
                            {
                                job.State = JobState.NoJob;
                                job.NextRow = null;
                                EnableStart = !EnableHold;
                            }
                            else
                            {
                                job.ToolChangeLine = -1;
                                job.CurrLine = job.PendingLine = job.ACKPending = model.BlockExecuting = 0;
                                job.PgmEndLine = GCode.File.Blocks - 1;
                                if (vm.IsPhysicalFileLoaded)
                                {
                                    if (GCode.File.ToolChanges > 0)
                                    {
                                        if (!GrblSettings.HasSetting(grblHALSetting.ToolChangeMode))
                                            MessageBox.Show(
                                                $"Job has {GCode.File.ToolChanges} tool change(s) using M6, only a few Grbl ports supports that", "ioSender", MessageBoxButton.OK,
                                                MessageBoxImage.Warning);
                                        else if (GrblSettings.GetInteger(grblHALSetting.ToolChangeMode) > 0 &&
                                                 !model.IsTloReferenceSet)
                                            MessageBox.Show(
                                                $"Job has {GCode.File.ToolChanges} tool change(s), tool length reference should be established before start", "ioSender", MessageBoxButton.OK,
                                                MessageBoxImage.Warning);
                                    }

                                    if (GCode.File.HasGoPredefinedPosition && vm.IsGrblHAL &&
                                        vm.HomedState != HomedState.Homed)
                                        MessageBox.Show(("Job has G28/G30 moves and machine is not homed"), "ioSender", MessageBoxButton.OK,
                                            MessageBoxImage.Warning);
                                    StreamingState = GCode.File.IsLoaded ? StreamingState.Idle : StreamingState.NoFile;
                                }
                                else
                                {
                                    job.IsSDFile = true;
                                    SdCardFileLoaded = true;
                                    JobTimer.Start();
                                }
                                EnableStart = true;
                                EnableStop = false;
                                EnableHold = model.GrblState.State != GrblStates.Hold;
                            }

                            break;
                        } ;
                    case nameof(GrblViewModel.GrblReset):
                        {
                            JobTimer.Stop();
                            StreamingState = StreamingState.Stop;
                        }
                        break;
                }
        }

        void GrblStateChanged(GrblState newstate)
        {
            if (grblState.State == GrblStates.Jog)
                model.IsJobRunning = false;
            switch (newstate.State)
            {
                case GrblStates.Idle:
                    StreamingState = StreamingState.Idle;
                    EnableHold = !(grblState.MPG || newstate.State == GrblStates.Alarm);
                    EnableStart = model.IsFileLoaded;
                    EnableStop = false;
                    EnableRewind = false;
                    break;

                case GrblStates.Jog:
                    model.IsJobRunning = !model.IsToolChanging;

                    break;
                case GrblStates.Run:

                    if (JobTimer.IsPaused)
                        JobTimer.Pause = false;
                    if (model.StreamingState != StreamingState.Error)
                        StreamingState = StreamingState.Send;
                    if (newstate.Substate == 1)
                    {
                        EnableStart = !grblState.MPG;
                        EnableHold = false;
                    }
                    else if (grblState.Substate == 1)
                    {
                        EnableStart = false;
                        EnableHold = !grblState.MPG;
                    }

                    // if (!GrblInfo.IsGrblHAL)
                    // btnStop.Content = (string)FindResource("JobPause");
                    EnableStart = false;
                    EnableHold = !(grblState.MPG || grblState.State == GrblStates.Alarm);
                    EnableStop = true;
                    EnableRewind = false;
                    break;

                case GrblStates.Hold:
                    StreamingState = StreamingState.FeedHold;
                    EnableStart = true;
                    EnableStop = model.IsFileLoaded && job.State == JobState.Running;
                    EnableHold = false;
                    EnableRewind = false;
                    break;
                case GrblStates.Tool:
                    if (grblState.State != GrblStates.Jog)
                    {
                        if (JobTimer.IsRunning && job.PendingLine > 0 && !model.IsSDCardJob)
                        {
                            job.ToolChangeLine = job.PendingLine - 1;
                            GCode.File.Data.Rows[job.ToolChangeLine]["Sent"] = "pending";
                        }

                        model.IsJobRunning = false; // only enable UI if no ATC?
                        EnableStart = true;
                        EnableStop = true;
                        EnableHold = false;
                        EnableRewind = false;
                        StreamingState = StreamingState.ToolChange;
                        if (!grblState.MPG)
                            Comms.com.WriteByte(GrblConstants.CMD_TOOL_ACK);
                    }
                    break;

                case GrblStates.Door:
                    if (newstate.Substate > 0)
                    {
                        if (StreamingState == StreamingState.Send)
                            StreamingState = StreamingState.FeedHold;
                        else
                            EnableStart = false;
                    }
                    else
                        EnableStart = true;
                    break;

                case GrblStates.Alarm:
                    StreamingState = StreamingState.Stop;
                    if (job.IsSDFile)
                    {
                       StopSdCardJob();
                    }
                    break;
                
            }
            grblState.State = newstate.State;
            grblState.Substate = newstate.Substate;
            grblState.MPG = newstate.MPG;
        }
        
        private void ActiveJobError()
        {
            StreamingState = StreamingState.Error;
            job.State = JobState.Error;
            EnableStart = false;
            EnableStop = true;
            EnableHold = false;
            EnableRewind = false;
        }
        private void ActiveJobCompleted()
        {
            JobTimer.Pause = true;
            if (!grblState.MPG)
            {
                Comms.com.WriteByte(GrblConstants.CMD_STOP);
            }
            FinalizeJobCleanup();
        }
        private void ActiveJobStop()
        {
            if (!model.IsSDCardJob && !GCode.File.IsLoaded) return;
            Comms.com.WriteByte(GrblConstants.CMD_STOP);
            StreamingState = StreamingState.Stop;
            FinalizeJobCleanup();
        }

        private void StopSdCardJob()
        {
            Comms.com.WriteByte(GrblConstants.CMD_STOP);
            SdCardFileLoaded = job.IsSDFile = false;
            JobTimer.Stop();
        }
        private void FinalizeJobCleanup()
        {
            var runTime = JobTimer.RunTime;
            JobTimer.Stop();
            model.RunTime = JobTimer.RunTime;
            model.IsJobRunning = false;
            StreamingState = StreamingState.JobFinished;
            job.State = JobState.Competed;
            job.ACKPending = job.CurrLine = job.serialUsed = 0;
            job.CurrentRow = job.NextRow = null;
            IsEnabled = !grblState.MPG;
            EnableStart = true;
            EnableStop = false;
            EnableHold = true;
            EnableRewind = false;
            StreamingState = StreamingState.Idle;
            model.Message = $"Program End Jog Time:{runTime}";
            RewindFile();
        }

        public void CycleStart()
        {
            if(!Active)return;
            switch (grblState.State)
            {
                case GrblStates.Hold:
                case GrblStates.Run when grblState.Substate == 1:
                case GrblStates.Door when grblState.Substate == 0:
                    StreamingState = StreamingState.Start;
                    Comms.com.WriteByte(GrblLegacy.ConvertRTCommand(GrblConstants.CMD_CYCLE_START));
                    EnableStart = false;
                    break;
                case GrblStates.Idle when model.SDRewind:
                    StreamingState = StreamingState.Start;
                    Comms.com.WriteByte(GrblLegacy.ConvertRTCommand(GrblConstants.CMD_CYCLE_START));
                    break;
                case GrblStates.Tool:
                    model.Message = "";
                    StreamingState = StreamingState.ToolChange;
                    Comms.com.WriteByte(GrblLegacy.ConvertRTCommand(GrblConstants.CMD_CYCLE_START));
                    break;
                default:
                    {
                        if (JobTimer.IsRunning)
                        {
                            JobTimer.Pause = false;
                            StreamingState = StreamingState.Send;
                            SendNextLine();
                        }
                        else if (GCode.File.IsLoaded)
                        {
                            model.Message = model.RunTime = string.Empty;
                            model.IsJobRunning = true;
                            if (model.IsSDCardJob)
                            {
                                Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_RUN + model.FileName.Substring(7));
                            }
                            else
                            {
                                job.ToolChangeLine = -1;
                                model.BlockExecuting = 0;
                                job.ACKPending = 0;
                                job.CurrLine = 0;
                                missed = 0;
                                job.serialUsed = 0;
                                job.State = JobState.Running;
                                job.NextRow = GCode.File.Data.Rows[0];
                                Comms.com.PurgeQueue();
                                JobTimer.Start();
                                StreamingState = StreamingState.Send;
                                if ((job.IsChecking = model.GrblState.State == GrblStates.Check))
                                    model.Message = "Checking";
                                EnableStart = false;
                                bool? res = null;
                                CancellationToken cancellationToken = new CancellationToken();


                                // Wait a bit for unlikely event before starting...
                                new Thread(() =>
                                {
                                    res = WaitFor.SingleEvent<string>(
                                    cancellationToken,
                                    null,
                                    a => model.OnGrblReset += a,
                                    a => model.OnGrblReset -= a,
                                   250);
                                }).Start();

                                while (res == null)
                                    EventUtils.DoEvents();

                                SendNextLine();
                            }
                        }

                        break;
                    }
            }
        }

        public void SendRtCommand(string command)
        {
            var b = Convert.ToInt32(command[0]);

            if (b > 255)
                switch (b)
                {
                    case 8222:
                        b = GrblConstants.CMD_SAFETY_DOOR;
                        break;

                    case 8225:
                        b = GrblConstants.CMD_STATUS_REPORT_ALL;
                        break;

                    case 710:
                        b = GrblConstants.CMD_OPTIONAL_STOP_TOGGLE;
                        break;

                    case 8240:
                        b = GrblConstants.CMD_SINGLE_BLOCK_TOGGLE;
                        break;
                }

            if (b <= 255)
                Comms.com.WriteByte((byte)b);
        }


        public void RewindFile()
        {
            if (GCode.File.IsLoaded)
            {
                using (new UIUtils.WaitCursor())
                {
                    job.State = JobState.FileLoaded;
                    EnableStart = false;
                    GCode.File.ClearStatus();
                    model.ScrollPosition = 0;
                    job.ToolChangeLine = -1;
                    job.CurrLine = job.PendingLine = job.ACKPending = model.BlockExecuting = 0;
                    job.PgmEndLine = GCode.File.Blocks - 1;
                    EnableStart = true;
                }
            }
            else
            {
                job.State = JobState.NoJob;
            }
        }

        private void ResponseReceived(string response)
        {
            if (Grbl.GrblViewModel.IsFileLoaded && job.State == JobState.Running)
            {
                if (job.ACKPending > 0)
                    job.ACKPending--;

                bool isError = response.StartsWith("error");
                if (!job.IsSDFile && (job.IsChecking || (string)GCode.File.Data.Rows[job.PendingLine]["Sent"] == "*"))
                    job.serialUsed = Math.Max(0, job.serialUsed - (int)GCode.File.Data.Rows[job.PendingLine]["Length"]);
                if (!(job.IsSDFile || job.IsChecking))
                {
                    if (job.State != JobState.Error)
                    {
                        GCode.File.Data.Rows[job.PendingLine]["Sent"] = response;

                        if (job.PendingLine > 2)
                            model.ScrollPosition = job.PendingLine - 2;
                    }
                }

                if (isError)
                {
                    if (job.IsChecking && job.State != JobState.Error)
                    {
                        if (job.PendingLine > 5)
                            model.ScrollPosition = job.PendingLine - 5;
                        GCode.File.Data.Rows[job.PendingLine]["Sent"] = response;
                    }

                    ActiveJobError();

                }
                else if (job.PgmEndLine == job.PendingLine)
                    ActiveJobCompleted();

                else if (response == "ok")
                {
                    job.PendingLine++;
                    if (!job.IsChecking || job.PendingLine % 250 == 0)
                        model.BlockExecuting = job.PendingLine;
                    SendNextLine();
                }

                SendNextLine();

                if (job.State == JobState.Competed)
                {
                    model.BlockExecuting = 0;
                    model.Message = "TransferComplete";
                }
                //else if (job.PendingLine != job.PgmEndLine)
                //{
                //    job.PendingLine++;
                //    if (!job.IsChecking || job.PendingLine % 250 == 0)
                //        model.BlockExecuting = job.PendingLine;
                //}
            }
            else if (response == "ok")
                missed++;

            switch (StreamingState)
            {
                case StreamingState.Send:
                    if (response == "start")
                        SendNextLine();
                    break;

                case StreamingState.SendMDI:
                    if (GCode.File.Commands.Count > 0)
                        Comms.com.WriteCommand(GCode.File.Commands.Dequeue());
                    if (GCode.File.Commands.Count == 0)
                        StreamingState = StreamingState.Idle;
                    break;

                case StreamingState.Reset:
                    Comms.com.WriteCommand(GrblConstants.CMD_UNLOCK);
                    StreamingState = StreamingState.AwaitResetAck;
                    break;
                case StreamingState.Stop:
                    ActiveJobStop();
                    break;

                case StreamingState.AwaitResetAck:
                    StreamingState = GCode.File.IsLoaded ? StreamingState.Idle : StreamingState.NoFile;
                    break;
            }
        }

        void SendNextLine()
        {
            while (job.NextRow != null)
            {
                var line = (string)job.NextRow["Data"];
                // Send comment lines as empty comment
                if ((bool)job.NextRow["IsComment"])
                {
                    line = "()";
                    job.NextRow["Length"] = line.Length + 1;
                }
                if (job.serialUsed < (serialSize - (int)job.NextRow["Length"]))
                {
                    if (GCode.File.Commands.Count > 0)
                    {
                        Comms.com.WriteCommand(GCode.File.Commands.Dequeue());
                    }
                    else
                    {
                        job.CurrentRow = job.NextRow;

                        if (!job.IsChecking)
                            job.CurrentRow["Sent"] = "*";

                        if (line == "%")
                        {
                            if (job.State != JobState.Competed)
                                job.PgmEndLine = job.CurrLine;
                        }
                        else if ((bool)job.CurrentRow["ProgramEnd"])
                            job.PgmEndLine = job.CurrLine;

                        job.NextRow = job.PgmEndLine == job.CurrLine ? null : GCode.File.Data.Rows[++job.CurrLine];
                        if (string.IsNullOrEmpty(line)) continue;
                        Comms.com.WriteString(line + '\r');
                        job.serialUsed += (int)job.CurrentRow["Length"];
                        job.ACKPending++;
                        if (!_useBuffering)
                        {
                            break;
                        }
                    }
                    job.ACKPending++;
                }
                else
                {
                    break;
                }
            }
        }

        private void SendCommand(string command)
        {
            if (command.Length == 1)
            {
                SendRtCommand(command);
            }

            else if (StreamingState == StreamingState.Idle || StreamingState == StreamingState.NoFile ||
                     StreamingState == StreamingState.ToolChange || StreamingState == StreamingState.Stop ||
                     (command == GrblConstants.CMD_UNLOCK && StreamingState != StreamingState.Send))
            {
                try
                {
                    string c = command;
                    GCode.File.Parser.ParseBlock(ref c, true);
                    GCode.File.Commands.Enqueue(command);
                    if (StreamingState != StreamingState.SendMDI)
                    {
                        StreamingState = StreamingState.SendMDI;
                        ResponseReceived("go");
                    }
                }
                catch
                {
                }
            }
        }
        public enum JobState
        {
            NoJob,
            FileLoaded,
            Running,
            Competed,
            Error,
        }
        private struct JobData
        {
            public JobState State;
            public int CurrLine, PendingLine, PgmEndLine, ToolChangeLine, ACKPending;
            public bool IsSDFile, IsChecking;
            public DataRow CurrentRow, NextRow;
            public int serialUsed;
        }

    }
}
