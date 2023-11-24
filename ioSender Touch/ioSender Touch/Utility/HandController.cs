using System;
using System.Linq;
using Windows.Gaming.Input;
using CNC.Core;
using ioSenderTouch.Controls;
using Timer = System.Timers.Timer;
using CNC.Controls;


namespace ioSenderTouch.Utility
{
    public class HandController
    {
        private readonly GrblViewModel _grblViewModel;
        private Gamepad _controller;
        private Timer _timer;
        private bool _singleActionPress;
        private GamepadButtons _previousDown;
        private int[] _feedRate;
        private double[] _distanceRate;
        private bool _stepMode;
        private bool _jogProcessed;

        public double DistanceRate => _distanceRate[(int)JogStepRate];
        public double FeedRate => _feedRate[(int)JogFeedRate];
        public JogFeed JogFeedRate { get; set; }
        public JogStep JogStepRate { get; set; }

        public HandController(GrblViewModel grblViewModel)
        {
            _grblViewModel = grblViewModel;
            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
            _timer = new Timer(20);
            _timer.Elapsed += _timer_Elapsed;
            _grblViewModel.GrblInitialized += _grblViewModel_GrblInitialized;
        }

        private void _grblViewModel_GrblInitialized(object sender, EventArgs e)
        {
            SetupRates();
        }

        private void SetupRates()
        {
            var isMetric = GrblSettings.GetInteger(GrblSetting.ReportInches) == 0;
            _distanceRate = isMetric ? AppConfig.Settings.JogUiMetric.Distance : AppConfig.Settings.JogUiImperial.Distance;
            _feedRate = isMetric ? AppConfig.Settings.JogUiMetric.Feedrate : AppConfig.Settings.JogUiImperial.Feedrate;
            JogStepRate = (JogStep)Array.FindIndex(_distanceRate, row => row.Equals(_grblViewModel.JogStep));
            JogFeedRate = (JogFeed)Array.FindIndex(_feedRate, row => row.Equals((int)_grblViewModel.JogRate));
        }

        private void Gamepad_GamepadRemoved(object sender, Gamepad e)
        {
            _controller = null;
            _timer.Stop();
        }

        private void Gamepad_GamepadAdded(object sender, Gamepad e)
        {
            _controller = Gamepad.Gamepads.First();
            _timer.Start();
        }
        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_controller == null) return;
            string command = string.Empty;
            var input = _controller.GetCurrentReading();
            switch (input.Buttons)
            {
                case GamepadButtons.None:
                    _singleActionPress = false;
                    _jogProcessed = false;
                    if (input.LeftTrigger != 1)
                    {
                        _stepMode = false;
                    }
                    break;
                case GamepadButtons.A:
                    command = $"$J = G91G21Z{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                    ProcessJogCommand(command);
                    break;
                case GamepadButtons.B:
                    command = $"$J = G91G21Z-{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                    ProcessJogCommand(command);
                    break;
                case GamepadButtons.DPadLeft:
                    command = $"$J = G91G21X-{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                    ProcessJogCommand(command);
                    break;
                case GamepadButtons.DPadRight:
                    command = $"$J = G91G21X{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                    ProcessJogCommand(command);
                    break;
                case GamepadButtons.DPadUp:
                    command = $"$J = G91G21Y{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                    ProcessJogCommand(command);
                    break;
                case GamepadButtons.DPadDown:
                    command = $"$J = G91G21Y-{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                    ProcessJogCommand(command);
                    break;
                case GamepadButtons.Y:
                    ProcessSinglePressCommand("G10L20P0Y0");
                    return;
                    break;
                case GamepadButtons.X:
                    ProcessSinglePressCommand("G10L20P0X0");
                    return;
                    break;
                case GamepadButtons.LeftShoulder:
                    ProcessJogDistance();
                    return;
                    break;
                case GamepadButtons.RightShoulder:
                    ProcessJogFeedRate();
                    return;
                    break;
                case GamepadButtons.Menu:
                    ProcessSinglePressCommand("G10L20P0Z0");
                    return;
                    break;
                case GamepadButtons.RightThumbstick:
                    ProcessSinglePressCommand(GrblConstants.CMD_HOMING);
                    return;
                    break;
                case GamepadButtons.LeftThumbstick:
                    ProcessSinglePressCommand(GrblConstants.CMD_UNLOCK);
                    return;
                    break;
            }
            if (Math.Abs(input.LeftTrigger - 1) < 0.1)
            {
                _stepMode = true;
            }
            //if (Math.Abs(input.RightTrigger - 1) < 0.1)
            //{

            //}

            //var x = Math.Round(input.LeftThumbstickX, 1);
            //var y = Math.Round(input.LeftThumbstickY, 1);

            if (input.Buttons == GamepadButtons.None
                && _previousDown != GamepadButtons.None
                                                     && !_stepMode)
            {
                Comms.com.PurgeQueue();
                Comms.com.WriteByte(GrblConstants.CMD_JOG_CANCEL);
            }

            _previousDown = input.Buttons;
        }

        private void ProcessPos(double y, string command)
        {

            if (y > .8)
            {
                command += $"{_grblViewModel.JogStep}F{_feedRate[3]}";
            }
            else if (y > .6)
            {
                command += $"{_grblViewModel.JogStep}F{_feedRate[2]}";
            }
            else if (y > .4)
            {
                command += $"{_grblViewModel.JogStep}F{_feedRate[1]}";
            }
            else if (y > .2)
            {
                command += $"{_grblViewModel.JogStep}F{_feedRate[0]}";
            }

            if (y < .2)
            {
                Comms.com.WriteByte(GrblConstants.CMD_JOG_CANCEL);
                command = string.Empty;
            }

            if (!string.IsNullOrEmpty(command))
            {
                Send(command);
            }
        }
        private void ProcessNeg(double y)
        {
            string command = null;
            if (y > .8)
            {
                command = $"$J = G91G21Y{_grblViewModel.JogStep}F{_feedRate[3]}";

            }
            else if (y > .6)
            {
                command = $"$J = G91G21Y{_grblViewModel.JogStep}F{_feedRate[2]}";

            }
            else if (y > .4)
            {
                command = $"$J = G91G21Y{_grblViewModel.JogStep}F{_feedRate[1]}";

            }
            else if (y > .2)
            {
                command = $"$J = G91G21Y{_grblViewModel.JogStep}F{_feedRate[0]}";

            }
            else if (y < .2)
            {
                Comms.com.WriteByte(GrblConstants.CMD_JOG_CANCEL);
                command = null;
            }

            if (string.IsNullOrEmpty(command))
            {
                Send(command);
            }

        }
        private void ProcessJogCommand(string command )
        {
            if (!_stepMode)
            {
                Send(command);
            }
            else
            {
                if (_jogProcessed) return;
                Send(command);
                _jogProcessed = true;
            }
        }
        private void Send(string command)
        {
            Comms.com.WriteCommand(command);
        }

        private void ProcessSinglePressCommand(string command)
        {
            if (_singleActionPress) return;
            {
                Send(command);
            }
            _singleActionPress = true;
        }

        private void ProcessJogDistance()
        {
            if (_singleActionPress) return;
            if (JogStepRate != JogStep.Step3)
            {
                JogStepRate += 1;
            }
            else
            {
                JogStepRate = JogStep.Step0;
            }
            SetJogDistance();
            _singleActionPress = true;
        }
        private void ProcessJogFeedRate()
        {
            if (_singleActionPress) return;
            if (JogFeedRate != JogFeed.Feed3)
            {
                JogFeedRate += 1;
            }
            else
            {
                JogFeedRate = JogFeed.Feed0;
            }
            SetJogRate();
            _singleActionPress = true;
        }
        private void SetJogRate()
        {
            _grblViewModel.JogRate = FeedRate;
        }
        private void SetJogDistance()
        {
            _grblViewModel.JogStep = DistanceRate;
        }
    }
}
