using System;
using System.IO;
using System.Threading;
using System.Windows;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Config;
using ioSenderTouch.ViewModels;
using LibStrings = ioSenderTouch.GrblCore.Config.LibStrings;

namespace ioSenderTouch.Utility
{
    public class Controller
    {
        GrblViewModel model;
        private readonly AppConfig _config;

        public enum RestartResult
        {
            Ok = 0,
            NoResponse,
            Close,
            Exit
        }

        public Controller(GrblViewModel model, AppConfig config)
        {
            this.model = model;
            _config = config;
        }



        public int SetupAndOpen(System.Windows.Threading.Dispatcher dispatcher)
        {
            int status = 0;
            bool selectPort = false;
            int jogMode = -1;
            string port = string.Empty, baud = string.Empty;
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config" + Path.DirectorySeparatorChar);
            Resources.Path = path;

            string[] args = Environment.GetCommandLineArgs();

            int p = 0;
            while (p < args.GetLength(0))
                switch (args[p++].ToLowerInvariant())
                {
                    case "-inifile":
                        Resources.IniName = GetArg(args, p++);
                        break;

                    case "-debugfile":
                        Resources.DebugFile = GetArg(args, p++);
                        break;

                    case "-configmapping":
                        Resources.ConfigName = GetArg(args, p++);
                        break;

                    case "-locale":
                    case "-language": // deprecated
                        Resources.Locale = GetArg(args, p++);
                        break;

                    case "-port":
                        port = GetArg(args, p++);
                        break;

                    case "-baud":
                        baud = GetArg(args, p++);
                        break;

                    case "-selectport":
                        selectPort = true;
                        break;

                    case "-islegacy":
                        Resources.IsLegacyController = true;
                        break;

                    case "-jogmode":
                        if (int.TryParse(GetArg(args, p++), out jogMode))
                            jogMode = Math.Min(Math.Max(jogMode, 0), (int)JogConfig.JogMode.KeypadAndUI);
                        break;

                    default:
                        if (!args[p - 1].EndsWith(".exe") && File.Exists(args[p - 1]))
                            _config.FileName = args[p - 1];
                        break;
                }

            if (!_config.Load(Resources.IniFile))
            {
                if (MessageBox.Show(LibStrings.FindResource("CreateConfig"), "IoSender", MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (!_config.Save(Resources.IniFile))
                    {
                        MessageBox.Show(LibStrings.FindResource("CreateConfigFail"), "IoSender");
                        status = 1;
                    }
                }
                else
                    return 1;
            }

            if (jogMode != -1)
                _config.Base.JogMetric.Mode = (JogConfig.JogMode)jogMode;

            if (!string.IsNullOrEmpty(port))
                selectPort = false;

            if (!selectPort)
            {
                if (!string.IsNullOrEmpty(port))
                    SetPort(port, baud);

                if (_config.Base.PortParams.ToLower().StartsWith("ws://"))
                    new WebsocketStream(_config.Base.PortParams, dispatcher);
                else

                if (char.IsDigit(_config.Base.PortParams[0])) // We have an IP address
                    new TelnetStream(_config.Base.PortParams, dispatcher);
                else
#if USEELTIMA
                    new EltimaStream(Config.PortParams, Config.ResetDelay, dispatcher);
#else
                    new SerialStream(_config.Base.PortParams, _config.Base.ResetDelay,
                        dispatcher);
#endif
            }

            if ((Comms.com == null || !Comms.com.IsOpen) && string.IsNullOrEmpty(port))
            {
                ioSenderTouch.Controls.PortDialog portsel = new ioSenderTouch.Controls.PortDialog();

                port = portsel.ShowDialog(_config.Base.PortParams);
                if (string.IsNullOrEmpty(port))
                    status = 2;

                else
                {
                    SetPort(port, string.Empty);

                    if (port.ToLower().StartsWith("ws://"))
                        new WebsocketStream(_config.Base.PortParams, dispatcher);
                    else

                    if (char.IsDigit(port[0])) // We have an IP address
                        new TelnetStream(_config.Base.PortParams, dispatcher);
                    else
#if USEELTIMA
                        new EltimaStream(Config.PortParams, Config.ResetDelay, dispatcher);
#else
                        new SerialStream(_config.Base.PortParams, _config.Base.ResetDelay,
                            dispatcher);
#endif
                    _config.Save(Resources.IniFile);
                    _config.CallFileLoaded();
                }
            }

            if (Comms.com != null && Comms.com.IsOpen)
            {
                Comms.com.DataReceived += model.DataReceived;

                CancellationToken cancellationToken = new CancellationToken();

                // Wait 400ms to see if a MPG is polling Grbl...

                new Thread(() =>
                {
                    _config.MPGactive = WaitFor.SingleEvent<string>(
                        cancellationToken,
                        null,
                        a => model.OnRealtimeStatusProcessed += a,
                        a => model.OnRealtimeStatusProcessed -= a,
                        500);
                }).Start();

                while (_config.MPGactive == null)
                    EventUtils.DoEvents();

                if (_config.MPGactive == true)
                {
                    _config.MPGactive = null;

                    new Thread(() =>
                    {
                        _config.MPGactive = WaitFor.SingleEvent<string>(
                            cancellationToken,
                            null,
                            a => model.OnRealtimeStatusProcessed += a,
                            a => model.OnRealtimeStatusProcessed -= a,
                            500, () => Comms.com.WriteByte(GrblConstants.CMD_STATUS_REPORT_ALL));
                    }).Start();

                    while (_config.MPGactive == null)
                        EventUtils.DoEvents();

                    if (_config.MPGactive == true)
                    {
                        if (model.IsMPGActive != true && model.AutoReportingEnabled)
                        {
                            _config.MPGactive = false;
                            if (model.AutoReportInterval > 0)
                                Comms.com.WriteByte(GrblConstants.CMD_AUTO_REPORTING_TOGGLE);
                        }
                    }
                }

                // ...if so show dialog for wait for it to stop polling and relinquish control.


                model.IsReady = true;
            }
            else if (status != 2)
            {
                MessageBox.Show(string.Format(LibStrings.FindResource("ConnectFailed"), _config.Base.PortParams), "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                status = 2;
            }

            return status;
        }

        private bool IsComPort(string port)
        {
            return !(port.ToLower().StartsWith("ws://") || char.IsDigit(port[0]));
        }

        private void SetPort(string port, string baud)
        {
            if (IsComPort(port) && port.IndexOf(':') == -1)
            {
                string prop = string.Format(":{0},N,8,1", string.IsNullOrEmpty(baud) ? "115200" : baud);
                string[] values = port.Split('!');
                if (IsComPort(_config.Base.PortParams))
                {
                    var props = _config.Base.PortParams
                        .Substring(_config.Base.PortParams.IndexOf(':')).Split(',');
                    if (props.Length >= 4)
                        prop = string.Format(":{0},{1},{2},{3}", (string.IsNullOrEmpty(baud) ? props[0] : baud),
                            props[1], props[2], props[3]);
                }

                port = values[0] + prop + (values.Length > 1 ? ",," + values[1] : "");
            }

            _config.Base.PortParams = port;
        }

        private string GetArg(string[] args, int i)
        {
            return i < args.GetLength(0) ? args[i] : null;
        }

        public bool ResetPending { get; private set; } = false;
        public string Message { get; private set; }

        public RestartResult Restart()
        {
            Message = model.Message;
            model.Message = string.Format(LibStrings.FindResource("MsgWaiting"),
                _config.Base.PortParams);

            string response = GrblInfo.Startup(model);

            if (response.StartsWith("<"))
            {
                if (model.GrblState.State != GrblStates.Unknown)
                {
                    switch (model.GrblState.State)
                    {
                        case GrblStates.Alarm:

                            model.Poller.SetState(_config.Base.PollInterval);

                            if (!model.SysCommandsAlwaysAvailable)
                                switch (model.GrblState.Substate)
                                {
                                    case 1: // Hard limits
                                        if (!GrblInfo.IsLoaded)
                                        {
                                            if (model.LimitTriggered)
                                            {
                                                MessageBox.Show(
                                                    string.Format(LibStrings.FindResource("MsgNoCommAlarm"),
                                                        model.GrblState.Substate.ToString()), "ioSender");
                                                if (AttemptReset())
                                                    model.ExecuteCommand(GrblConstants.CMD_UNLOCK);
                                                else
                                                {
                                                    MessageBox.Show(LibStrings.FindResource("MsgResetFailed"),
                                                        "ioSender");
                                                    return RestartResult.Close;
                                                }
                                            }
                                            else if (AttemptReset())
                                                model.ExecuteCommand(GrblConstants.CMD_UNLOCK);
                                        }
                                        else
                                            response = string.Empty;

                                        break;

                                    case 2: // Soft limits
                                        if (!GrblInfo.IsLoaded)
                                        {
                                            MessageBox.Show(
                                                string.Format(LibStrings.FindResource("MsgNoCommAlarm"),
                                                    model.GrblState.Substate.ToString()), "ioSender");
                                            if (AttemptReset())
                                                model.ExecuteCommand(GrblConstants.CMD_UNLOCK);
                                            else
                                            {
                                                MessageBox.Show(LibStrings.FindResource("MsgResetFailed"),
                                                    "ioSender");
                                                return RestartResult.Close;
                                            }
                                        }
                                        else
                                            response = string.Empty;

                                        break;

                                    case 10: // EStop
                                        if (GrblInfo.IsGrblHAL && model.Signals.Value.HasFlag(Signals.EStop))
                                        {
                                            MessageBox.Show(LibStrings.FindResource("MsgEStop"), "ioSender",
                                                MessageBoxButton.OK, MessageBoxImage.Warning);
                                            while (!AttemptReset() && model.GrblState.State == GrblStates.Alarm)
                                            {
                                                if (MessageBox.Show(LibStrings.FindResource("MsgEStopExit"),
                                                        "ioSender", MessageBoxButton.YesNo,
                                                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                                                    return RestartResult.Close;
                                            }

                                            ;
                                        }
                                        else
                                            AttemptReset();

                                        if (!GrblInfo.IsLoaded)
                                            model.ExecuteCommand(GrblConstants.CMD_UNLOCK);
                                        break;

                                    case 11: // Homing required
                                        if (GrblInfo.IsLoaded)
                                            response = string.Empty;
                                        else
                                            Message = LibStrings.FindResource("MsgHome");
                                        break;
                                }

                            break;

                        case GrblStates.Tool:
                            Comms.com.WriteByte(GrblConstants.CMD_STOP);
                            break;

                        case GrblStates.Door:
                            if (!GrblInfo.IsLoaded)
                            {
                                if (MessageBox.Show(LibStrings.FindResource("MsgDoorOpen"), "ioSender",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                                    return RestartResult.Close;
                                else
                                {
                                    bool exit = false;
                                    do
                                    {
                                        Comms.com.PurgeQueue();

                                        bool? res = null;
                                        CancellationToken cancellationToken = new CancellationToken();

                                        new Thread(() =>
                                        {
                                            res = WaitFor.SingleEvent<string>(
                                                cancellationToken,
                                                s => TrapReset(s),
                                                a => model.OnGrblReset += a,
                                                a => model.OnGrblReset -= a,
                                                200, () => Comms.com.WriteByte(GrblConstants.CMD_STATUS_REPORT));
                                        }).Start();

                                        while (res == null)
                                            EventUtils.DoEvents();

                                        if (!(exit = !model.Signals.Value.HasFlag(Signals.SafetyDoor)))
                                        {
                                            if (MessageBox.Show(LibStrings.FindResource("MsgDoorExit"), "ioSender",
                                                    MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                                                MessageBoxResult.Yes)
                                            {
                                                exit = true;
                                                return RestartResult.Close;
                                            }
                                        }
                                    } while (!exit);
                                }

                                if (model.GrblState.State == GrblStates.Door && model.GrblState.Substate == 0)
                                    Comms.com.WriteByte(GrblConstants.CMD_RESET);
                            }
                            else
                            {
                                MessageBox.Show(LibStrings.FindResource("MsgDoorPersist"), "ioSender",
                                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                                response = string.Empty;
                            }

                            break;

                        case GrblStates.Hold:
                        case GrblStates.Sleep:
                            if (MessageBox.Show(
                                    string.Format(LibStrings.FindResource("MsgNoComm"),
                                        model.GrblState.State.ToString()),
                                    "ioSender", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
                                MessageBoxResult.Yes)
                                return RestartResult.Close;
                            else if (!AttemptReset())
                            {
                                MessageBox.Show(LibStrings.FindResource("MsgResetExit"), "ioSender");
                                return RestartResult.Close;
                            }

                            break;

                        case GrblStates.Idle:
                            if (response.Contains("|SD:Pending"))
                                AttemptReset();
                            break;
                    }
                }
            }
            else
            {
                MessageBox.Show(response == string.Empty
                        ? LibStrings.FindResource("MsgNoResponseExit")
                        : string.Format(LibStrings.FindResource("MsgBadResponseExit"), response),
                    "ioSender", MessageBoxButton.OK, MessageBoxImage.Stop);
                return RestartResult.Exit;
            }

            return response == string.Empty ? RestartResult.NoResponse : RestartResult.Ok;
        }

        private void TrapReset(string rws)
        {
            ResetPending = false;
        }

        private bool AttemptReset()
        {
            ResetPending = true;
            Comms.com.PurgeQueue();

            bool? res = null;
            CancellationToken cancellationToken = new CancellationToken();

            new Thread(() =>
            {
                res = WaitFor.SingleEvent<string>(
                    cancellationToken,
                    s => TrapReset(s),
                    a => model.OnGrblReset += a,
                    a => model.OnGrblReset -= a,
                    _config.Base.ResetDelay, () => Comms.com.WriteByte(GrblConstants.CMD_RESET));
            }).Start();

            while (res == null)
                EventUtils.DoEvents();

            return !ResetPending;
        }
    }
}
