/*
 * JobControl.xaml.cs - part of CNC Controls library for Grbl
 *
 * v0.43 / 2023-05-31 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2018-2023, Io Engineering (Terje Io)
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
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Config;
using ioSenderTouch.ViewModels;

namespace ioSenderTouch.Controls
{
    public partial class JobControl : UserControl
    {
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

        private static bool keyboardMappingsOk = false;
        private int serialSize = 128;

        private bool holdSignal = false,
            cycleStartSignal = false,
            initOK = false,
            isActive = false,
            useBuffering = false;

        private GrblState grblState;
        private GrblViewModel model;
        private JobData job;
        private int missed = 0;
        private StreamingState _streamingState = StreamingState.NoFile;

        public bool JobPending
        {
            get { return GCode.File.IsLoaded && !JobTimer.IsRunning; }
        }

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

        public JobControl()
        {
            InitializeComponent();
            DataContextChanged += JobControl_DataContextChanged;
            grblState.State = GrblStates.Unknown;
            grblState.Substate = 0;
            grblState.MPG = false;
            job.PgmEndLine = -1;
            AppConfig.Settings.OnConfigFileLoaded += Settings_OnConfigFileLoaded;
            this.Loaded += JobControl_Loaded;
            Name = nameof(JobControl);
        }

        private void JobControl_Loaded(object sender, RoutedEventArgs e)
        {
            serialSize = Math.Min(AppConfig.Settings.Base.MaxBufferSize, (int)(GrblInfo.SerialBufferSize * 0.9f));
        }

        private void Settings_OnConfigFileLoaded(object sender, EventArgs e)
        {
            var uiSettings = AppConfig.Settings.AppUiSettings;
            if (uiSettings.EnableStopLightTheme)
            {
                btnStart.Background = Brushes.Green;
                btnHold.Background = Brushes.Yellow;
                btnStop.Background = Brushes.DarkRed;
            }

            useBuffering = AppConfig.Settings.Base.UseBuffering;
            ProcessKeyMappings();
        }

        private void ProcessKeyMappings()
        {
            
            GCode.File.Parser.Dialect = GrblInfo.IsGrblHAL ? Dialect.GrblHAL : Dialect.Grbl;
            GCode.File.Parser.ExpressionsSupported = GrblInfo.ExpressionsSupported;
            model = Grbl.GrblViewModel;
            AppConfig.Settings.Base.PropertyChanged += Base_PropertyChanged;

            if (!keyboardMappingsOk && DataContext is GrblViewModel)
            {
                KeypressHandler keyboard = model.Keyboard;
                keyboardMappingsOk = true;
                var parent = UIUtils.TryFindParent<UserControl>(this);
                keyboard.AddHandler(Key.R, ModifierKeys.Alt, StartJob, parent);
                keyboard.AddHandler(Key.S, ModifierKeys.Alt, StopJob, parent);
                keyboard.AddHandler(Key.H, ModifierKeys.Control, Home, parent);
                keyboard.AddHandler(Key.U, ModifierKeys.Control, Unlock);
                keyboard.AddHandler(Key.R, ModifierKeys.Shift | ModifierKeys.Control, Reset);
                keyboard.AddHandler(Key.Space, ModifierKeys.None, FeedHold, parent);
                keyboard.AddHandler(Key.F1, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F2, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F3, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F4, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F5, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F6, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F7, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F8, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F9, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F10, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F11, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.F12, ModifierKeys.None, FnKeyHandler);
                keyboard.AddHandler(Key.OemMinus, ModifierKeys.Control, FeedRateDown);
                keyboard.AddHandler(Key.OemPlus, ModifierKeys.Control, FeedRateUp);
                keyboard.AddHandler(Key.OemMinus, ModifierKeys.Shift | ModifierKeys.Control, FeedRateDownFine);
                keyboard.AddHandler(Key.OemPlus, ModifierKeys.Shift | ModifierKeys.Control, FeedRateUpFine);
            }
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
        private void JobControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null && e.OldValue is INotifyPropertyChanged)
                ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= OnDataContextPropertyChanged;
            if (e.NewValue != null && e.NewValue is INotifyPropertyChanged)
            {
                model = (GrblViewModel)e.NewValue;
                model.PropertyChanged += OnDataContextPropertyChanged;
                model.OnRealtimeStatusProcessed += RealtimeStatusProcessed;
                model.OnCommandResponseReceived += ResponseReceived;
                GCode.File.Model = model;
            }
        }

        private void RealtimeStatusProcessed(string response)
        {
            if (JobTimer.IsRunning && !JobTimer.IsPaused)
                model.RunTime = JobTimer.RunTime;
        }

        private void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs e)
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
                        if (isActive)
                        {
                            var signals = vm.Signals.Value;
                            if (JobPending && signals.HasFlag(Signals.CycleStart) && !signals.HasFlag(Signals.Hold) &&
                                !cycleStartSignal)
                                CycleStart();
                            holdSignal = signals.HasFlag(Signals.Hold);
                            cycleStartSignal = signals.HasFlag(Signals.CycleStart);
                        }

                        break;

                    case nameof(GrblViewModel.ProgramEnd):
                        if (!GCode.File.IsLoaded)
                            StreamingState = model.IsSDCardJob ? StreamingState.JobFinished : StreamingState.NoFile;
                        else if (JobTimer.IsRunning && job.State != JobState.Competed)
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
                                                string.Format((string)FindResource("JobToolChanges"),
                                                    GCode.File.ToolChanges), "ioSender", MessageBoxButton.OK,
                                                MessageBoxImage.Warning);
                                        else if (GrblSettings.GetInteger(grblHALSetting.ToolChangeMode) > 0 &&
                                                 !model.IsTloReferenceSet)
                                            MessageBox.Show(
                                                string.Format((string)FindResource("JobToolReference"),
                                                    GCode.File.ToolChanges), "ioSender", MessageBoxButton.OK,
                                                MessageBoxImage.Warning);
                                    }

                                    if (GCode.File.HasGoPredefinedPosition && vm.IsGrblHAL &&
                                        vm.HomedState != HomedState.Homed)
                                        MessageBox.Show((string)FindResource("JobG28G30"), "ioSender", MessageBoxButton.OK,
                                            MessageBoxImage.Warning);
                                    StreamingState = GCode.File.IsLoaded ? StreamingState.Idle : StreamingState.NoFile;
                                }

                                btnStart.IsEnabled = true;
                                btnStop.IsEnabled = false;
                                btnStop.IsEnabled = !btnStart.IsEnabled;
                            }

                            break;
                        }

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

                    btnStart.IsEnabled = btnHold.IsEnabled && GCode.File.IsLoaded;
                    btnHold.IsEnabled = !(grblState.MPG || grblState.State == GrblStates.Alarm);
                    btnStop.IsEnabled = false;
                    btnRewind.IsEnabled = false;
                    break;

                case GrblStates.Jog:
                    model.IsJobRunning = !model.IsToolChanging;

                    break;

                //case GrblStates.Check
                //    streamingHandler.Call(StreamingState.Send, false);
                //    break;

                case GrblStates.Run:

                    if (JobTimer.IsPaused)
                        JobTimer.Pause = false;
                    if (model.StreamingState != StreamingState.Error)
                        StreamingState = StreamingState.Send;
                    if (newstate.Substate == 1)
                    {
                        btnStart.IsEnabled = !grblState.MPG;
                        btnHold.IsEnabled = false;
                    }
                    else if (grblState.Substate == 1)
                    {
                        btnStart.IsEnabled = false;
                        btnHold.IsEnabled = !grblState.MPG;
                    }

                    if (!GrblInfo.IsGrblHAL)
                        btnStop.Content = (string)FindResource("JobPause");

                    btnStart.IsEnabled = false;
                    btnHold.IsEnabled = !(grblState.MPG || grblState.State == GrblStates.Alarm);
                    btnStop.IsEnabled = true;
                    btnRewind.IsEnabled = false;
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
                        btnStart.IsEnabled = true;
                        btnHold.IsEnabled = false;
                        btnStop.IsEnabled = true;
                        StreamingState = StreamingState.ToolChange;
                        if (!grblState.MPG)
                            Comms.com.WriteByte(GrblConstants.CMD_TOOL_ACK);
                    }

                    break;

                case GrblStates.Hold:
                    StreamingState = StreamingState.FeedHold;
                    btnHold.IsEnabled = false;
                    btnStart.IsEnabled = true;
                    btnStop.IsEnabled = true;
                    btnRewind.IsEnabled = false;
                    break;

                case GrblStates.Door:
                    if (newstate.Substate > 0)
                    {
                        if (StreamingState == StreamingState.Send)
                            StreamingState = StreamingState.FeedHold;
                        else
                            btnStart.IsEnabled = false;
                    }
                    else
                        btnStart.IsEnabled = true;

                    break;

                case GrblStates.Alarm:
                    StreamingState = StreamingState.Stop;
                    break;
            }

            grblState.State = newstate.State;
            grblState.Substate = newstate.Substate;
            grblState.MPG = newstate.MPG;
        }
        public void EnablePolling(bool enable)
        {
            if (enable)
                model.Poller.SetState(AppConfig.Settings.Base.PollInterval);
            else if (model.Poller.IsEnabled)
                model.Poller.SetState(0);
        }

        private bool FeedRateUpFine(Key key)
        {
            Comms.com.WriteByte((byte)GrblConstants.CMD_FEED_OVR_FINE_PLUS);
            return true;
        }

        private bool FeedRateDownFine(Key key)
        {
            Comms.com.WriteByte((byte)GrblConstants.CMD_FEED_OVR_FINE_MINUS);
            return true;
        }

        private bool FeedRateUp(Key key)
        {
            Comms.com.WriteByte((byte)GrblConstants.CMD_FEED_OVR_COARSE_PLUS);
            return true;
        }

        private bool FeedRateDown(Key key)
        {
            Comms.com.WriteByte((byte)GrblConstants.CMD_FEED_OVR_COARSE_MINUS);
            return true;
        }

        private bool StopJob(Key key)
        {
            StreamingState = StreamingState.Stop;
            return true;
        }

        private bool StartJob(Key key)
        {
            CycleStart();
            return true;
        }

        private bool Home(Key key)
        {
            model.ExecuteCommand(GrblConstants.CMD_HOMING);
            return true;
        }

        private bool Unlock(Key key)
        {
            model.ExecuteCommand(GrblConstants.CMD_UNLOCK);
            return true;
        }

        private bool Reset(Key key)
        {
            Comms.com.WriteByte((byte)GrblConstants.CMD_RESET);
            return true;
        }

        private bool FeedHold(Key key)
        {
            if (grblState.State != GrblStates.Idle)
                btnHold_Click(null, null);
            return grblState.State != GrblStates.Idle;
        }

        private bool FnKeyHandler(Key key)
        {
            if (!model.IsJobRunning)
            {
                int id = int.Parse(key.ToString().Substring(1));
                var macro = AppConfig.Settings.Macros.FirstOrDefault(o => o.Id == id);
                if (macro != null && (!macro.ConfirmOnExecute ||
                                      MessageBox.Show(string.Format("Run {0} macro?", macro.Name), "Run macro",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes))
                {
                    model.ExecuteCommand(macro.Code);
                    return true;
                }
            }

            return false;
        }

        void btnRewind_Click(object sender, RoutedEventArgs e)
        {
            RewindFile();
        }

        void btnHold_Click(object sender, RoutedEventArgs e)
        {
            Comms.com.WriteByte(GrblLegacy.ConvertRTCommand(GrblConstants.CMD_FEED_HOLD));
        }

        void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (job.State == JobState.Running && StreamingState == StreamingState.FeedHold || job.State == JobState.Error)
            {
                ActiveJobStop();
            }
            else
            {
                StreamingState = StreamingState.Stop;
            }
        }
        void btnStart_Click(object sender, RoutedEventArgs e)
        {
            CycleStart();
        }
        private void ActiveJobCompleted()
        {
            StreamingState = StreamingState.JobFinished;
            FinalizeJobCleanup();
        }
        private void ActiveJobError()
        {
            StreamingState = StreamingState.Error;
            job.State = JobState.Error;
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnHold.IsEnabled = false;
            btnRewind.IsEnabled = false;
        }
        private void ActiveJobStop()
        {
            if (model.IsSDCardJob && !GCode.File.IsLoaded)
                model.FileName = string.Empty;
            if (!grblState.MPG)
            {
                if (GrblInfo.IsGrblHAL && !(grblState.State == GrblStates.Home || grblState.State == GrblStates.Alarm))
                {
                    if (!model.GrblReset)
                    {
                        Comms.com.WriteByte(GrblConstants.CMD_STOP);
                        if (!model.IsParserStateLive)
                            SendCommand(GrblConstants.CMD_GETPARSERSTATE);
                    }
                }
                else if (grblState.State == GrblStates.Hold && !model.GrblReset)
                    Comms.com.WriteByte(GrblConstants.CMD_RESET);
            }
            StreamingState = StreamingState.Stop;
            FinalizeJobCleanup();
        }

        private void FinalizeJobCleanup()
        {
            job.State = JobState.Competed;
            job.ACKPending = job.CurrLine = 0;
            job.CurrentRow = job.NextRow = null;
            model.RunTime = JobTimer.RunTime;
            JobTimer.Stop();
            
            model.IsJobRunning = false;
            IsEnabled = !grblState.MPG;
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnHold.IsEnabled = true;
            btnRewind.IsEnabled = false;
            StreamingState = StreamingState.Idle;
            RewindFile();
        }

        public void CycleStart()
        {
            switch (grblState.State)
            {
                case GrblStates.Hold:
                case GrblStates.Run when grblState.Substate == 1:
                case GrblStates.Door when grblState.Substate == 0:
                    StreamingState = StreamingState.Start;
                    Comms.com.WriteByte(GrblLegacy.ConvertRTCommand(GrblConstants.CMD_CYCLE_START));
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
                        }
                        else if (GCode.File.IsLoaded)
                        {
                            model.Message = model.RunTime = string.Empty;
                            if (model.IsSDCardJob)
                            {
                                Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_RUN + model.FileName.Substring(7));
                            }
                            else
                            {
                                job.ToolChangeLine = -1;
                                model.BlockExecuting = 0;
                                job.ACKPending = job.CurrLine = missed = 0;
                                job.serialUsed = missed = 0;
                                job.State = JobState.Running;
                                job.NextRow = GCode.File.Data.Rows[0];
                                Comms.com.PurgeQueue();
                                JobTimer.Start();
                                StreamingState = StreamingState.Send;
                                if ((job.IsChecking = model.GrblState.State == GrblStates.Check))
                                    model.Message = (string)FindResource("Checking");

                                //bool? res = null;
                                //CancellationToken cancellationToken = new CancellationToken();


                                // Wait a bit for unlikely event before starting...
                                //new Thread(() =>
                                //{
                                //    res = WaitFor.SingleEvent<string>(
                                //    cancellationToken,
                                //    null,
                                //    a => model.OnGrblReset += a,
                                //    a => model.OnGrblReset -= a,
                                //   250);
                                //}).Start();

                                //while (res == null)
                                //    EventUtils.DoEvents();

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
                    btnStart.IsEnabled = false;
                    GCode.File.ClearStatus();
                    model.ScrollPosition = 0;
                    job.ToolChangeLine = -1;
                    job.CurrLine = job.PendingLine = job.ACKPending = model.BlockExecuting = 0;
                    job.PgmEndLine = GCode.File.Blocks - 1;
                    btnStart.IsEnabled = true;
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

                        if (job.PendingLine > 5)
                            model.ScrollPosition = job.PendingLine - 5;
                    }
                }

                if (isError)
                {
                    if (job.IsChecking &&  job.State != JobState.Error)
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
                    SendNextLine();

                if (job.State == JobState.Competed)
                {
                    model.BlockExecuting = 0;
                    model.Message = (string)FindResource("TransferComplete");
                }
                else if (job.PendingLine != job.PgmEndLine)
                {
                    job.PendingLine++;
                    if (!job.IsChecking || job.PendingLine % 250 == 0)
                        model.BlockExecuting = job.PendingLine;
                }
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
                        if (!useBuffering)
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

    }
}



