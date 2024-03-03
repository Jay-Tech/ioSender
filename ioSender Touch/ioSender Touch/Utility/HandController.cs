using System;
using System.Linq;
using System.Threading.Tasks;
using CNC.Core;
using ioSenderTouch.Controls;
using CNC.Controls;
using System.Threading;
using System.Runtime;
using Windows.Gaming.Input;
using System.Collections;
using System.Text;
using System.Windows.Markup;


namespace ioSenderTouch.Utility
{
    public class HandController
    {
        private const string JogHeader = "$J = G91G21";

        private GrblViewModel _grblViewModel;
        private Gamepad _controller;
        private bool _singleActionPress;
        private GamepadButtons _previousDown;
        private int[] _feedRate;
        private double[] _distanceRate;
        private bool _stepMode;
        private bool _jogProcessed;
        private Task _buttonPollThread;
        private int _pollRate = 20;
        CancellationTokenSource _cancellationTokenSource;
        private bool _joystickJogging;
        private bool _continuousJogActive;


        public double DistanceRate => _distanceRate?[(int)JogStepRate] ?? 1;
        public double FeedRate => _feedRate?[(int)JogFeedRate] ?? 1000;
        public JogFeed JogFeedRate { get; set; }
        public JogStep JogStepRate { get; set; }

        public HandController(GrblViewModel grblViewModel)
        {
            _grblViewModel = grblViewModel;
            _grblViewModel.GrblInitialized += _grblViewModel_GrblInitialized;
            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
        }

        private void _grblViewModel_GrblInitialized(object sender, EventArgs e)
        {
            SetupRates();
        }


        private void SetupRates()
        {
            try
            {
                var isMetric = GrblSettings.GetInteger(GrblSetting.ReportInches) == 0;
                _distanceRate = isMetric ? AppConfig.Settings.JogUiMetric.Distance : AppConfig.Settings.JogUiImperial.Distance;
                _feedRate = isMetric ? AppConfig.Settings.JogUiMetric.Feedrate : AppConfig.Settings.JogUiImperial.Feedrate;
                JogStepRate = (JogStep)Array.FindIndex(_distanceRate, row => row.Equals(_grblViewModel.JogStep));
                JogFeedRate = (JogFeed)Array.FindIndex(_feedRate, row => row.Equals((int)_grblViewModel.JogRate));
            }
            catch (Exception e)
            {
                //
            }
        }

        private void Gamepad_GamepadRemoved(object sender, Gamepad e)
        {
            _controller = null;
            _cancellationTokenSource?.Cancel();
        }

        private void Gamepad_GamepadAdded(object sender, Gamepad e)
        {
            _controller = Gamepad.Gamepads.First();
            if (_buttonPollThread?.Status == TaskStatus.Running) return;
            _cancellationTokenSource = new CancellationTokenSource();
            _buttonPollThread = Task.Factory.StartNew(() => Poll(_cancellationTokenSource), TaskCreationOptions.LongRunning);
        }


        private void Poll(CancellationTokenSource cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (_controller == null) return;
                string command;
                var input = _controller.GetCurrentReading();
                double step = 0;
                switch (input.Buttons)
                {
                    case GamepadButtons.None:
                        _singleActionPress = false;
                        _jogProcessed = false;
                        _continuousJogActive = false;
                        if (input.LeftTrigger != 1)
                        {
                            _stepMode = false;
                        }

                        break;

                    case GamepadButtons.B:
                        if (_continuousJogActive) continue;
                        if (_stepMode)
                        {
                            step = _grblViewModel.JogStep;
                        }
                        else
                        {
                            _continuousJogActive = true;
                            var zCurrent = Math.Abs(_grblViewModel.MachinePosition.Z);
                            var zMax = _grblViewModel.MaxDistanceZ;
                            step = zMax - zCurrent;
                        }
                        command = $"$J = G91G21Z{step}F{_grblViewModel.JogRate}";
                        ProcessJogCommand(command);
                        break;
                    case GamepadButtons.A:
                        if (_continuousJogActive) continue;
                        if (_stepMode)
                        {
                            step = _grblViewModel.JogStep;
                        }
                        else
                        {
                            _continuousJogActive = true;
                            var zCurrent = _grblViewModel.MachinePosition.Z;
                            step = zCurrent;
                        }
                        command = $"$J = G91G21Z-{step}F{_grblViewModel.JogRate}";
                        ProcessJogCommand(command);
                        break;
                    case GamepadButtons.DPadLeft:
                        if (_continuousJogActive) continue;
                        if (_stepMode)
                        {
                            step = _grblViewModel.JogStep;
                        }
                        else
                        {
                            _continuousJogActive = true;
                            var xCurrent = _grblViewModel.MachinePosition.X;
                            step = xCurrent;
                        }
                        command = $"$J = G91G21X-{step}F{_grblViewModel.JogRate}";
                        ProcessJogCommand(command);
                        break;
                    case GamepadButtons.DPadRight:
                        if (_continuousJogActive) continue;
                        if (_stepMode)
                        {
                            step = _grblViewModel.JogStep;
                        }
                        else
                        {
                            _continuousJogActive = true;
                            var xCurrent = Math.Abs(_grblViewModel.MachinePosition.X);
                            var xMax = _grblViewModel.MaxDistanceX;
                            step = xMax - xCurrent;
                        }
                        command = $"$J = G91G21X{step}F{_grblViewModel.JogRate}";
                        ProcessJogCommand(command);
                        break;
                    case GamepadButtons.DPadUp:
                        if (_continuousJogActive) continue;
                        if (_stepMode)
                        {
                            step = _grblViewModel.JogStep;
                        }
                        else
                        {
                            _continuousJogActive = true;
                            var yCurrent = Math.Abs(_grblViewModel.MachinePosition.Y);
                            var yMax = _grblViewModel.MaxDistanceY;
                            step = yMax - yCurrent;
                        }
                        command = $"$J = G91G21Y{step}F{_grblViewModel.JogRate}";
                        ProcessJogCommand(command);
                        break;
                    case GamepadButtons.DPadDown:
                        if (_continuousJogActive) continue;
                        if (_stepMode)
                        {
                            step = _grblViewModel.JogStep;
                        }
                        else
                        {
                            _continuousJogActive = true;
                            var yCurrent = _grblViewModel.MachinePosition.Y;
                            step = yCurrent;
                        }
                        command = $"$J = G91G21Y-{step}F{_grblViewModel.JogRate}";
                        ProcessJogCommand(command);
                        break;
                    case GamepadButtons.X:
                        ProcessSinglePressCommand("G10L20P0Y0");
                        continue;
                        break;
                    case GamepadButtons.Y:
                        ProcessSinglePressCommand("G10L20P0X0");
                        continue;
                        break;
                    case GamepadButtons.LeftShoulder:
                        ProcessJogDistance();
                        continue;
                        break;
                    case GamepadButtons.RightShoulder:
                        ProcessJogFeedRate();
                        continue;
                        break;
                    //case GamepadButtons.Menu:
                    //continue;
                    // break;
                    case GamepadButtons.View:
                        ProcessSinglePressCommand("G10L20P0Z0");
                        continue;
                        break;
                    case GamepadButtons.RightThumbstick:
                        ProcessSinglePressCommand(GrblConstants.CMD_HOMING);
                        continue;
                        break;
                    case GamepadButtons.LeftThumbstick:
                        ProcessSinglePressCommand(GrblConstants.CMD_UNLOCK);
                        continue;
                        break;
                }

                if (Math.Abs(input.LeftTrigger - 1) < 0.1)
                {
                    _continuousJogActive = false;
                    _stepMode = true;

                }
                //if (Math.Abs(input.RightTrigger - 1) < 0.1)
                //{
                //todo no right trigger setting atm
                //}

                if (input.Buttons == GamepadButtons.None
                    && _previousDown != GamepadButtons.None
                    && !_stepMode)
                {

                    Comms.com.WriteByte(GrblConstants.CMD_JOG_CANCEL);
                    _continuousJogActive = false;
                }
                //TODO JoyStick has a little of drift when releasing 
                //var x = Math.Round(input.LeftThumbstickX, 1);
                //var y = Math.Round(input.LeftThumbstickY, 1);
                // ProcessJoyStick(x, y);
                _previousDown = input.Buttons;
                Thread.Sleep(_pollRate);
            }
        }

        private void ProcessJoyStick(double x, double y)
        {
            if ((x + y) == 0 && !_joystickJogging) return;
            var commandX = x > 0 ? "X" : "X-";
            var commandY = y > 0 ? "Y" : "Y-";
            var absX = Math.Abs(x);
            var absY = Math.Abs(y);
            var command = ProcessVelocity(absX, commandX, absY, commandY);
            if (string.IsNullOrEmpty(command)) return;
            Console.WriteLine(command);
            Send(command);
        }

        private string ProcessVelocity(double velocityX, string commandX, double velocityY, string commandY)
        {
            string command;
            _joystickJogging = true;
            var feedRateX = BuildJoggingCommand(velocityX);
            var feedRateY = BuildJoggingCommand(velocityY);
            var averageRate = (feedRateX + feedRateY) / 2;

            if (feedRateX == 0)
            {
                var step = CalculateJogStep(feedRateY);
                command = $"{JogHeader}{commandY}{step}F{feedRateY}";
            }
            else if (feedRateY == 0)
            {
                var step = CalculateJogStep(feedRateX);
                command = $"{JogHeader}{commandX}{step}F{feedRateX}";
            }
            else
            {
                var step = CalculateJogStep(averageRate);
                command = $"{JogHeader}{commandX}{step}{commandY}{_grblViewModel.JogStep}F{averageRate}";
            }

            if (!averageRate.Equals(0)) return command;
            _joystickJogging = false;
            Comms.com.WriteByte(GrblConstants.CMD_JOG_CANCEL);
            command = string.Empty;
            _pollRate = 50;
            return command;
        }

        private double CalculateJogStep(double feedRate)
        {
            var step = 0.0;
            if (feedRate > 1500)
            {
                step = 1.65;
                _pollRate = 50;
            }
            else if (feedRate > 500)
            {
                step = 1.5;
                _pollRate = 110;

            }
            else
            {
                step = .35;
                _pollRate = 210;
            }
            return step;
        }

        private double BuildJoggingCommand(double velocity)
        {
            double feedRate = 0;
            //if (velocity >= .7)
            //{
            //    feedRate = _feedRate[3];
            //}
            //else if (velocity > .4)
            //{
            //    feedRate = _feedRate[2];
            //}
            //else if (velocity > .3)
            //{
            //    feedRate = _feedRate[1];
            //}
            if (velocity > .3)
            {
                feedRate = _grblViewModel.JogRate;
            }
            else if (velocity <= .3)
            {
                feedRate = 0;
            }
            return feedRate;
        }


        // Single Axis Joystick movement 
        // For using joystick for Jog found to much drift on release of joystick causing machine to jog and appearance of latency 
        private void ProcessX(double movement)
        {
            if (movement == 0 && !_joystickJogging) return;
            var command = movement > 0 ? "$J = G91G21X" : "$J = G91G21X-";
            var x = Math.Abs(movement);
            var c = ProcessVelocity(x, command);
            if (!string.IsNullOrEmpty(c))
            {
                Send(c);
            }
        }
        // Single Axis Joystick movement 
        // For using joystick for Jog found to much drift on release of joystick causing machine to jog and appearance of latency
        private void ProcessY(double movement)
        {
            if (movement == 0 && !_joystickJogging) return;
            var command = movement > 0 ? $"$J = G91G21Y" : $"$J = G91G21Y-";
            var y = Math.Abs(movement);
            command += ProcessVelocity(y, command);
            if (!string.IsNullOrEmpty(command))
            {
                Send(command);
            }
        }
        //// Single Axis Joystick movement 
        private string ProcessVelocity(double velocity, string command)
        {
            _joystickJogging = true;

            if (velocity > .8)
            {
                command += $"{_grblViewModel.JogStep}F{_feedRate[3]}";
            }
            else if (velocity > .5)
            {
                command += $"{_grblViewModel.JogStep}F{_feedRate[2]}";
            }
            else if (velocity > .2)
            {
                command += $"{_grblViewModel.JogStep}F{_feedRate[1]}";
            }
            else if (velocity <= .2)
            {
                Comms.com.WriteByte(GrblConstants.CMD_JOG_CANCEL);
                command = string.Empty;
                _joystickJogging = false;
            }
            return command;
        }

        private void ProcessJogCommand(string command)
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
