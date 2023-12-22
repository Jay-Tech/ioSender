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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CNC.Controls;
using CNC.Core;

namespace ioSenderTouch.Controls
{
    /// <summary>
    /// Interaction logic for JogControl.xaml
    /// </summary>
    public partial class JogControlT : UserControl 
    {
        private string mode = "G21"; // Metric
        private bool softLimits = false;
        private int jogAxis = -1;
        private double limitSwitchesClearance = .5d, position = 0d;
        private KeypressHandler keyboard;
        private static bool keyboardMappingsOk = false;
        private GrblViewModel _grblViewModel;
        private double[] _distance = new double[4];
        private double[] _feedRate = new double[4];

        private JogDistanceConverter distanceConvertor = new JogDistanceConverter();
        public double Distance { get { return _distance[(int)JogStep]; } }
        public double FeedRate { get { return _feedRate[(int)JobFeed]; } }

        private const Key xplus = Key.J, xminus = Key.H, yplus = Key.K, yminus = Key.L, zplus = Key.I,
            zminus = Key.M, aplus = Key.U, aminus = Key.N;

        public JogFeed JobFeed { get; set; }
        public JogStep JogStep { get; set; }
        public JogControlT()
        {
            InitializeComponent();
            Focusable = true;
            Loaded += JogControlT_Loaded;
        }

        private void JogControlT_Loaded(object sender, RoutedEventArgs e)
        {
            _grblViewModel = Grbl.GrblViewModel;
            DataContext = _grblViewModel;
            JogStep = JogStep.Step2;
            JobFeed = JogFeed.Feed2;
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

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GrblViewModel.MachinePosition) ||
                e.PropertyName == nameof(GrblViewModel.GrblState))
            {
                if (_grblViewModel.GrblState.State != GrblStates.Jog)
                    jogAxis = -1;
            }

        }

        private void SetUpControl()
        {
            if (DataContext is GrblViewModel viewModel)
            {
                mode = GrblSettings.GetInteger(GrblSetting.ReportInches) == 0 ? "G21" : "G20";
                softLimits = !(GrblInfo.IsGrblHAL && GrblSettings.GetInteger(grblHALSetting.SoftLimitJogging) == 1) && GrblSettings.GetInteger(GrblSetting.SoftLimitsEnable) == 1;
                limitSwitchesClearance = GrblSettings.GetDouble(GrblSetting.HomingPulloff);


                SetMetric(mode == "G21");
                viewModel.JogRate = FeedRate;
                viewModel.JogStep = Distance;
                if (!keyboardMappingsOk)
                {
                    if (!GrblInfo.HasFirmwareJog || AppConfig.Settings.Jog.LinkStepJogToUI)
                        if (softLimits)
                            _grblViewModel.PropertyChanged += Model_PropertyChanged;

                    keyboard = _grblViewModel.Keyboard;

                    keyboardMappingsOk = true;

                    if (AppConfig.Settings.Jog.Mode == JogConfig.JogMode.UI)
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

                    if (AppConfig.Settings.Jog.Mode != JogConfig.JogMode.Keypad)
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
                JobFeed = feed;
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
            this.JobFeed = JogFeed.Feed0;
            SetJogRate();
            return true;
        }

        private bool JogFeed1(Key key)
        {
            this.JobFeed = JogFeed.Feed1;
            SetJogRate();
            return true;
        }
        private bool JogFeed2(Key key)
        {
            this.JobFeed = JogFeed.Feed2;
            SetJogRate();
            return true;
        }
        private bool JogFeed3(Key key)
        {
            this.JobFeed = JogFeed.Feed3;
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
            GrblViewModel model = DataContext as GrblViewModel;

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
                        if (model != null)
                            position = jogDataDistance + model.MachinePosition.Values[axis];
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
        public void SetMetric(bool on)
        {
            var convertorJogDistance = new JogDistanceConverter();
            var convertorJogRate = new JogRateConverter();
            for (int i = 0; i < _feedRate.Length; i++)
            {
                _distance[i] = on ? AppConfig.Settings.JogUiMetric.Distance[i] : AppConfig.Settings.JogUiImperial.Distance[i];
                var buttonD = new Button
                {
                    Name = "Button" + i,
                    Content = _distance[i],
                    Tag = i,
                    Width = 68,
                    Height = 40,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    BorderThickness = new Thickness(2),
                    
                };
                var buttonBinding = new Binding
                {
                    Source = _grblViewModel,
                    Mode = BindingMode.TwoWay,
                    Path = new PropertyPath("JogStep"),
                    Converter = convertorJogDistance,
                    ConverterParameter = _distance[i],
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                   
                };
                BindingOperations.SetBinding(buttonD, Button.BorderBrushProperty, buttonBinding);
                buttonD.Click += distance_Click;
                PanelDistance.Children.Insert(1,buttonD);
              
                _feedRate[i] = on ? AppConfig.Settings.JogUiMetric.Feedrate[i] : AppConfig.Settings.JogUiImperial.Feedrate[i];
                var buttonFr = new Button
                {
                    Name = "Button" + i,
                    Content = _feedRate[i],
                    Tag = i,
                    Height = 40,
                    Width = 105,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 3, 0, 0),

                };
                var buttonBindingFr = new Binding
                {
                    Source = _grblViewModel,
                    Mode = BindingMode.TwoWay,
                    Path = new PropertyPath("JogRate"),
                    Converter =  convertorJogRate,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    ConverterParameter = _feedRate[i],
                };
                BindingOperations.SetBinding(buttonFr, Button.BorderBrushProperty, buttonBindingFr);
                buttonFr.Click += feedrate_Click;
                PanelFeedRate.Children.Insert(1,buttonFr);
            }

            var labelD = new Label
            {
                Content = new Binding(nameof(FeedRate)),
                Height = 20,
                FontSize = 12,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center

            };
            Binding labelDBinding = new Binding
            {
                Source = _grblViewModel.Unit,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(labelD, Label.ContentProperty, labelDBinding);
            PanelDistance.Children.Add(labelD);

            var labelFR = new Label
            {
                Content = new Binding(nameof(FeedRate)),
                Height = 20,
                FontSize = 12,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center

            };
            //Binding myBinding = new Binding
            //{
            //    Source = _grblViewModel.FeedrateUnit,
            //    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            //};
            //BindingOperations.SetBinding(labelFR, Label.ContentProperty, myBinding);
            //PanelFeedRate.Children.Add(labelFR);
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
            if (JobFeed != JogFeed.Feed3)
            {
                JobFeed += 1;
                SetJogRate();
            }
           
        }

        public void FeedDec()
        {
            if (JobFeed != JogFeed.Feed0)
            {
                JobFeed -= 1;
                SetJogRate();
            }
           
        }


    }
    public enum JogStep
    {
        Step0 = 0,
        Step1,
        Step2,
        Step3
    }
    public enum JogFeed
    {
        Feed0 = 0,
        Feed1,
        Feed2,
        Feed3
    }

}
