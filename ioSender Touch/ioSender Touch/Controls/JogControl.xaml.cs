/*
 * JogBaseControl.xaml.cs - part of CNC Controls library
 *
 * v0.37 / 2022-02-27 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2020-2022, Io Engineering (Terje Io)
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CNC.Core;
using CNC.Core.Config;
using ioSenderTouch.Utility;

namespace ioSenderTouch.Controls
{
    /// <summary>
    /// Interaction logic for JogControl.xaml
    /// </summary>
    public partial class JogControl : UserControl, INotifyPropertyChanged
    {

        private const Key xplus = Key.J, xminus = Key.H, yplus = Key.K, yminus = Key.L, zplus = Key.I,
            zminus = Key.M, aplus = Key.U, aminus = Key.N;

        private string mode = "G21"; // Metric
        private bool softLimits = false;
        private int jogAxis = -1;
        private double limitSwitchesClearance = .5d, position = 0d;
        private KeypressHandler keyboard;
        private static bool keyboardMappingsOk = false;
        private GrblViewModel _grblViewModel;
        private double[] _distance = new double[4];
        private int[] _feedRate = new int[4];

        private int _feedrate3;
        private int _feedrate1;
        private int _feedrate0;
        private int _feedrate2;
        private double _distance0;
        private double _distance1;
        private double _distance2;
        private double _distance3;
        private JogFeed _jogFeed;
        private JogStep _jogStep;
        public double Distance { get { return _distance[(int)JogStep]; } }
        public double FeedRate
        {
            get
            {
                return _feedRate[(int)JogFeed];
            }
        }
        public JogFeed JogFeed
        {
            get => _jogFeed;
            set
            {
                if (value == _jogFeed) return;
                _jogFeed = value;
                OnPropertyChanged();
            }
        }
        public JogStep JogStep
        {
            get => _jogStep;
            set
            {
                if (value == _jogStep) return;
                _jogStep = value;
                OnPropertyChanged();
            }
        }
        public int Feedrate3
        {
            get => _feedrate3;
            set
            {
                if (value == _feedrate3) return;
                _feedrate3 = value;
                OnPropertyChanged();
            }
        }
        public int Feedrate2
        {
            get => _feedrate2;
            set
            {
                if (value == _feedrate2) return;
                _feedrate2 = value;
                OnPropertyChanged();
            }
        }
        public int Feedrate1
        {
            get => _feedrate1;
            set
            {
                if (value == _feedrate1) return;
                _feedrate1 = value;
                OnPropertyChanged();
            }
        }
        public int Feedrate0
        {
            get => _feedrate0;
            set
            {
                if (value == _feedrate0) return;
                _feedrate0 = value;
                OnPropertyChanged();
            }
        }
        public double Distance0
        {
            get => _distance0;
            set
            {
                if (value == _distance0) return;
                _distance0 = value;
                OnPropertyChanged();
            }
        }
        public double Distance1
        {
            get => _distance1;
            set
            {
                if (value == _distance1) return;
                _distance1 = value;
                OnPropertyChanged();
            }
        }
        public double Distance2
        {
            get => _distance2;
            set
            {
                if (value == _distance2) return;
                _distance2 = value;
                OnPropertyChanged();
            }
        }
        public double Distance3
        {
            get => _distance3;
            set
            {
                if (value == _distance3) return;
                _distance3 = value;
                OnPropertyChanged();
            }
        }
        public JogControl()
        {
            InitializeComponent();
            AppConfig.Settings.OnConfigFileLoaded += Settings_OnConfigFileLoaded;
        }

        private void Settings_OnConfigFileLoaded(object sender, EventArgs e)
        {
            _grblViewModel = Grbl.GrblViewModel;
            _grblViewModel.GrblUnitChanged += GrblViewModelGrblUnitChanged;
            JogStep = JogStep.Step3;
            JogFeed = JogFeed.Feed3;
            SetUpControl();
        }

        private void GrblViewModelGrblUnitChanged(object sender, Measurement e)
        {
            SetUpControl();
        }

        private void SetJogRate()
        {
            _grblViewModel.JogRate = FeedRate;
        }
        private void SetJogDistance()
        {
            _grblViewModel.JogStep = Distance;
        }

        private void SetUpControl()
        {
            mode = _grblViewModel.IsMetric ? "G21" : "G20";
            _feedRate = _grblViewModel.IsMetric ? AppConfig.Settings.JogUiMetric.Feedrate : AppConfig.Settings.JogUiImperial.Feedrate;
            _distance = _grblViewModel.IsMetric ? AppConfig.Settings.JogUiMetric.Distance : AppConfig.Settings.JogUiImperial.Distance;
            Feedrate3 = _feedRate[3];
            Feedrate2 = _feedRate[2];
            Feedrate1 = _feedRate[1];
            Feedrate0 = _feedRate[0];
            Distance3 = _distance[3];
            Distance2 = _distance[2];
            Distance1 = _distance[1];
            Distance0 = _distance[0];
            _grblViewModel.JogRate = FeedRate;
            _grblViewModel.JogStep = Distance;
            if (!keyboardMappingsOk)
            {
                if (!GrblInfo.HasFirmwareJog || AppConfig.Settings.JogMetric.LinkStepJogToUI)
                    keyboard = _grblViewModel.Keyboard;
                if (keyboard == null) return;

                keyboardMappingsOk = true;

                if (AppConfig.Settings.JogMetric.Mode == JogConfig.JogMode.UI)
                {
                    keyboard.AddHandler(Key.PageUp, ModifierKeys.None, CursorJogZplus, false);
                    keyboard.AddHandler(Key.PageDown, ModifierKeys.None, CursorJogZminus, false);
                    keyboard.AddHandler(Key.Left, ModifierKeys.None, CursorJogXminus, false);
                    keyboard.AddHandler(Key.Up, ModifierKeys.None, CursorJogYplus, false);
                    keyboard.AddHandler(Key.Right, ModifierKeys.None, CursorJogXplus, false);
                    keyboard.AddHandler(Key.Down, ModifierKeys.None, CursorJogYminus, false);
                }

                keyboard.AddHandler(xplus, ModifierKeys.Control | ModifierKeys.Shift, KeyJogXplus, false);
                keyboard.AddHandler(xminus, ModifierKeys.Control | ModifierKeys.Shift, KeyJogXminus, false);
                keyboard.AddHandler(yplus, ModifierKeys.Control | ModifierKeys.Shift, KeyJogYplus, false);
                keyboard.AddHandler(yminus, ModifierKeys.Control | ModifierKeys.Shift, KeyJogYminus, false);
                keyboard.AddHandler(zplus, ModifierKeys.Control | ModifierKeys.Shift, KeyJogZplus, false);
                keyboard.AddHandler(zminus, ModifierKeys.Control | ModifierKeys.Shift, KeyJogZminus, false);
                if (GrblInfo.AxisFlags.HasFlag(AxisFlags.A))
                {
                    keyboard.AddHandler(aplus, ModifierKeys.Control | ModifierKeys.Shift, KeyJogAplus, false);
                    keyboard.AddHandler(aminus, ModifierKeys.Control | ModifierKeys.Shift, KeyJogAminus, false);
                }

                if (AppConfig.Settings.JogMetric.Mode != JogConfig.JogMode.Keypad)
                {
                    keyboard.AddHandler(Key.End, ModifierKeys.None, EndJog, false);

                    keyboard.AddHandler(Key.NumPad0, ModifierKeys.Control, JogStep0);
                    keyboard.AddHandler(Key.NumPad1, ModifierKeys.Control, JogStep1);
                    keyboard.AddHandler(Key.NumPad2, ModifierKeys.Control, JogStep2);
                    keyboard.AddHandler(Key.NumPad3, ModifierKeys.Control, JogStep3);
                    keyboard.AddHandler(Key.NumPad4, ModifierKeys.Control, JogFeed0);
                    keyboard.AddHandler(Key.NumPad5, ModifierKeys.Control, JogFeed1);
                    keyboard.AddHandler(Key.NumPad6, ModifierKeys.Control, JogFeed2);
                    keyboard.AddHandler(Key.NumPad7, ModifierKeys.Control, JogFeed3);

                    keyboard.AddHandler(Key.NumPad2, ModifierKeys.None, FeedDec);
                    keyboard.AddHandler(Key.NumPad4, ModifierKeys.None, StepDec);
                    keyboard.AddHandler(Key.NumPad6, ModifierKeys.None, StepInc);
                    keyboard.AddHandler(Key.NumPad8, ModifierKeys.None, FeedInc);
                }
            }
        }

        private bool KeyJogXplus(Key key)
        {
            if (keyboard.CanJog2 && !keyboard.IsRepeating)
                JogCommand(GrblInfo.LatheModeEnabled ? "Z+" : "X+");

            return true;
        }

        private bool KeyJogXminus(Key key)
        {
            if (keyboard.CanJog2 && !keyboard.IsRepeating)
                JogCommand(GrblInfo.LatheModeEnabled ? "Z-" : "X-");

            return true;
        }

        private bool KeyJogYplus(Key key)
        {
            if (keyboard.CanJog2 && !keyboard.IsRepeating)
                JogCommand(GrblInfo.LatheModeEnabled ? "X-" : "Y+");

            return true;
        }

        private bool KeyJogYminus(Key key)
        {
            if (keyboard.CanJog2 && !keyboard.IsRepeating)
                JogCommand(GrblInfo.LatheModeEnabled ? "X+" : "Y-");

            return true;
        }

        private bool KeyJogZplus(Key key)
        {
            if (keyboard.CanJog2 && !keyboard.IsRepeating && !GrblInfo.LatheModeEnabled)
                JogCommand("Z+");

            return true;
        }

        private bool KeyJogZminus(Key key)
        {
            if (keyboard.CanJog2 && !keyboard.IsRepeating && !GrblInfo.LatheModeEnabled)
                JogCommand("Z-");

            return true;
        }

        private bool KeyJogAplus(Key key)
        {
            if (keyboard.CanJog2 && !keyboard.IsRepeating)
                JogCommand("A+");

            return true;
        }

        private bool KeyJogAminus(Key key)
        {
            if (keyboard.CanJog2 && !keyboard.IsRepeating)
                JogCommand("A-");

            return true;
        }

        private bool CursorJogXplus(Key key)
        {
            if (keyboard.CanJog && !keyboard.IsRepeating)
                JogCommand(GrblInfo.LatheModeEnabled ? "Z+" : "X+");

            return true;
        }

        private bool CursorJogXminus(Key key)
        {
            if (keyboard.CanJog && !keyboard.IsRepeating)
                JogCommand(GrblInfo.LatheModeEnabled ? "Z-" : "X-");

            return true;
        }

        private bool CursorJogYplus(Key key)
        {
            if (keyboard.CanJog && !keyboard.IsRepeating)
                JogCommand(GrblInfo.LatheModeEnabled ? "X-" : "Y+");

            return true;
        }

        private bool CursorJogYminus(Key key)
        {
            if (keyboard.CanJog && !keyboard.IsRepeating)
                JogCommand(GrblInfo.LatheModeEnabled ? "X+" : "Y-");

            return true;
        }

        private bool CursorJogZplus(Key key)
        {
            if (keyboard.CanJog && !keyboard.IsRepeating && !GrblInfo.LatheModeEnabled)
                JogCommand("Z+");

            return true;
        }

        private bool CursorJogZminus(Key key)
        {
            if (keyboard.CanJog && !keyboard.IsRepeating && !GrblInfo.LatheModeEnabled)
                JogCommand("Z-");

            return true;
        }

        private void distance_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            if (Enum.TryParse(button.Tag.ToString(), true, out JogStep step))
            {
                JogStep = step;
                SetJogDistance();
            }

        }
        private void feedrate_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            if (Enum.TryParse(button.Tag.ToString(), true, out JogFeed feed))
            {
                JogFeed = feed;
                SetJogRate();
            }
        }
        private bool EndJog(Key key)
        {
            if (!keyboard.IsRepeating && keyboard.IsJogging)
                JogCommand("stop");

            return keyboard.IsJogging;
        }

        private bool JogStep0(Key key)
        {
            JogStep = JogStep.Step0;
            SetJogDistance();
            return true;
        }

        private bool JogStep1(Key key)
        {
            JogStep = JogStep.Step1;
            SetJogDistance();
            return true;
        }

        private bool JogStep2(Key key)
        {
            JogStep = JogStep.Step2;
            SetJogDistance();
            return true;
        }

        private bool JogStep3(Key key)
        {
            JogStep = JogStep.Step3;
            SetJogDistance();
            return true;
        }
        private bool JogFeed0(Key key)
        {
            this.JogFeed = JogFeed.Feed0;
            SetJogRate();
            return true;
        }
        private bool JogFeed1(Key key)
        {
            this.JogFeed = JogFeed.Feed1;
            SetJogRate();
            return true;
        }
        private bool JogFeed2(Key key)
        {
            this.JogFeed = JogFeed.Feed2;
            SetJogRate();
            return true;
        }
        private bool JogFeed3(Key key)
        {
            this.JogFeed = JogFeed.Feed3;
            SetJogRate();
            return true;
        }
        private bool FeedDec(Key key)
        {
            FeedDec();
            return true;
        }
        private bool FeedInc(Key key)
        {
            FeedInc();
            return true;
        }
        private bool StepDec(Key key)
        {
            StepDec();
            return true;
        }
        private bool StepInc(Key key)
        {
            StepInc();
            return true;
        }
        private void JogCommand(string cmd)
        {

            if (cmd == "stop")
                cmd = ((char)GrblConstants.CMD_JOG_CANCEL).ToString();
            else
            {
                var jogDataDistance = cmd[1] == '-' ? -Distance : Distance;
                if (softLimits)
                {
                    int axis = GrblInfo.AxisLetterToIndex(cmd[0]);

                    if (jogAxis != -1 && axis != jogAxis)
                        return;

                    if (axis != jogAxis)
                    {
                        if (_grblViewModel != null)
                            position = jogDataDistance + _grblViewModel.MachinePosition.Values[axis];
                    }
                    else
                        position += jogDataDistance;

                    if (GrblInfo.ForceSetOrigin)
                    {
                        if (!GrblInfo.HomingDirection.HasFlag(GrblInfo.AxisIndexToFlag(axis)))
                        {
                            if (position > 0d)
                                position = 0d;
                            else if (position < (-GrblInfo.MaxTravel.Values[axis] + limitSwitchesClearance))
                                position = (-GrblInfo.MaxTravel.Values[axis] + limitSwitchesClearance);
                        }
                        else
                        {
                            if (position < 0d)
                                position = 0d;
                            else if (position > (GrblInfo.MaxTravel.Values[axis] - limitSwitchesClearance))
                                position = GrblInfo.MaxTravel.Values[axis] - limitSwitchesClearance;
                        }
                    }
                    else
                    {
                        if (position > -limitSwitchesClearance)
                            position = -limitSwitchesClearance;
                        else if (position < -(GrblInfo.MaxTravel.Values[axis] - limitSwitchesClearance))
                            position = -(GrblInfo.MaxTravel.Values[axis] - limitSwitchesClearance);
                    }

                    if (position == 0d)
                        return;

                    jogAxis = axis;

                    cmd =
                        $"$J=G53{mode}{cmd.Substring(0, 1)}{position.ToInvariantString()}F{Math.Ceiling(FeedRate).ToInvariantString()}";
                }
                else
                    cmd =
                        $"$J=G91{mode}{cmd.Substring(0, 1)}{jogDataDistance.ToInvariantString()}F{Math.Ceiling(FeedRate).ToInvariantString()}";
            }

            _grblViewModel.ExecuteCommand(cmd);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            JogCommand((string)(sender as Button)?.Tag == "stop" ? "stop" : (string)(sender as Button)?.Content);
        }
        public void StepInc()
        {
            if (JogStep != JogStep.Step3)
            {
                JogStep += 1;
                SetJogDistance();
            }
        }
        public void StepDec()
        {
            if (JogStep != JogStep.Step0)
            {
                JogStep -= 1;
                SetJogDistance();
            }
        }

        public void FeedInc()
        {
            if (JogFeed != JogFeed.Feed3)
            {
                JogFeed += 1;
                SetJogRate();
            }
        }

        public void FeedDec()
        {
            if (JogFeed != JogFeed.Feed0)
            {
                JogFeed -= 1;
                SetJogRate();
            }
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
