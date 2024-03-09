/*
 * OutlineBaseControl.xaml.cs - part of CNC Controls library
 *
 * v0.42 / 2023-03-21 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2021-2023, Io Engineering (Terje Io)
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

using System.Windows;
using System.Windows.Controls;
using CNC.Core;

namespace CNC.Controls
{
    public partial class OutlineControl : UserControl
    {
        public OutlineControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty FeedRateProperty = DependencyProperty.Register(nameof(FeedRate), typeof(int), typeof(OutlineControl), new PropertyMetadata(500));
        public int FeedRate
        {
            get => (int)GetValue(FeedRateProperty);
            set => SetValue(FeedRateProperty, value);
        }

        private void button_Go(object sender, RoutedEventArgs e)
        {
            GrblViewModel model = DataContext as GrblViewModel;

            if (AppConfig.Settings.Base.OutlineFeedRate != FeedRate)
            {
                AppConfig.Settings.Base.OutlineFeedRate = FeedRate;
                AppConfig.Settings.Save();
            }

            if (model == null || !model.IsFileLoaded) return;
            if (!model.IsParserStateLive)
                GrblParserState.Get();

            var wasMetric = GrblParserState.IsMetric;
            var gcode = $"G90G{(model.IsMetric ? 21 : 20)}G1F{FeedRate}\r";

            gcode += $"X{model.ProgramLimits.MinX.ToInvariantString()}Y{model.ProgramLimits.MinY.ToInvariantString(model.Format)}\r";
            gcode += $"Y{model.ProgramLimits.MaxY.ToInvariantString(model.Format)}\r";
            gcode += $"X{model.ProgramLimits.MaxX.ToInvariantString(model.Format)}\r";
            gcode += $"Y{model.ProgramLimits.MinY.ToInvariantString(model.Format)}\r";
            gcode += $"X{model.ProgramLimits.MinX.ToInvariantString(model.Format)}\r";
            if(model.IsMetric != wasMetric)
                gcode += $"G{(wasMetric ? 21 : 20)}\r";

            model.ExecuteCommand(gcode);
        }

        private void OutlineControl_Loaded(object sender, RoutedEventArgs e)
        {
            FeedRate = AppConfig.Settings.Base.OutlineFeedRate;
        }
    }
}
