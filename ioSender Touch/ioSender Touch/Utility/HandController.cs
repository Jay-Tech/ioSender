using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Gaming.Input;
using CNC.Core;
using ioSenderTouch.Controls;
using Timer = System.Timers.Timer;

namespace ioSenderTouch.Utility
{
    public class HandController
    {
        private readonly GrblViewModel _grblViewModel;
        private Gamepad _controller;
        private Timer _timer;
        private GamepadButtons _previousDown;
        private GamepadButtons _currentDown;
        private bool _zMoving;
        private double[] _feedRate = new []{100.0,500.0,1000.0,2000.0};

        public HandController(GrblViewModel grblViewModel)
        {
            _grblViewModel = grblViewModel;
            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
            _timer = new Timer(30);
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_controller != null)
            {
                var command = string.Empty;
                var input = _controller.GetCurrentReading();
                _currentDown = input.Buttons;
                switch (input.Buttons)
                {
                      
                    case GamepadButtons.A:
                        command = $"$J = G91G21Z{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                        //command="G10L20P0Z0";
                        break;
                    case GamepadButtons.B:
                        command = $"$J = G91G21Z-{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                        break;
                    case GamepadButtons.Y:
                       command = "G10L20P0Y0";
                        break;
                    case GamepadButtons.X:
                       command = "G10L20P0X0";
                        break;
                    case GamepadButtons.LeftShoulder:
                        break;
                    case GamepadButtons.DPadLeft:
                        command = $"$J = G91G21X-{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                        break;
                    case GamepadButtons.DPadRight:
                        command = $"$J = G91G21X{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                        break;
                    case GamepadButtons.DPadUp:
                       command = $"$J = G91G21Y{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                        break;
                    case GamepadButtons.DPadDown:
                        command=$"$J = G91G21Y-{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                        break;
                    case GamepadButtons.RightShoulder:
                        
                        if (JobFeedRate != JogFeed.Feed3)
                        {
                            JobFeedRate += 1;
                            SetJogRate();
                        }
                        else
                        {
                            JobFeedRate = JogFeed.Feed0;
                            SetJogRate();
                        }
                        break;

                }

                if (Math.Abs(input.RightTrigger - 1) < 0.1)
                {
                    command = "G10L20P0Z0";
                }
                
               
                var x = Math.Round(input.LeftThumbstickX, 1);
                var y = Math.Round(input.LeftThumbstickY, 1);
                //if (y > .8)
                //{
                //  command =   $"$J = G91G21Z{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                //  _zMoving = true;
                //}
                
                //else if (y < -.8)
                //{
                //    command = $"$J = G91G21Z-{_grblViewModel.JogStep}F{_grblViewModel.JogRate}";
                //    _zMoving = true ;
                //}
                //else if(_zMoving)
                //{
                //    Comms.com.WriteByte(GrblConstants.CMD_JOG_CANCEL);
                //    _zMoving = false;
                //}
               
                if (input.Buttons == GamepadButtons.None && _previousDown != GamepadButtons.None)
                {
                    Comms.com.WriteByte(GrblConstants.CMD_JOG_CANCEL);
                    command = null;
                }
                if (!string.IsNullOrEmpty(command))
                {
                    Comms.com.WriteCommand(command);
                }

                
                _previousDown = _currentDown;
            }
        }

        private void SetJogRate()
        {
            _grblViewModel.JogRate = FeedRate;
        }

        public double FeedRate { get { return _feedRate[(int)JobFeedRate]; } }

        public JogFeed JobFeedRate { get; set; }

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


    }
}
