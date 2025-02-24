/*
 * ToolView.xaml.cs - part of CNC Controls library
 *
 * v0.42 / 2023-03-21 / Io Engineering (Terje Io)
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ioSenderTouch.GrblCore;
using ioSenderTouch.ViewModels;

namespace ioSenderTouch.Controls
{
    public partial class ToolView : UserControl
    {
        private readonly GrblViewModel _model;
        Tool selectedTool = null;
        private volatile bool awaitCoord = false;
        private Action<string> GotPosition;

        public ToolView(GrblViewModel model)
        {
            _model = model;
            InitializeComponent();
            GrblWorkParameters.Get(_model);
            dgrTools.ItemsSource = new ObservableCollection<Tool>(from tool in GrblWorkParameters.Tools
                                                                  where tool.Code != GrblConstants.NO_TOOL
                                                                  orderby tool.Code
                                                                  select tool);
            dgrTools.SelectedIndex = 0;
        }

        public Position offset { get; private set; } = new Position();

        private void dgrTools_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                selectedTool = (Tool)e.AddedItems[0];

                for (var i = 0; i < offset.Values.Length; i++)
                {
                    if (double.IsNaN(offset.Values[i])) offset.Values[i] = 120; // workaround for binding not propagating

                    offset.Values[i] = selectedTool.Values[i];
                }
                txtTool.Text = selectedTool.Code;
            }
            else
                selectedTool = null;
        }

        // G10 L1 P- axes <R- I- J- Q-> Set Tool Table
        // L10 - ref G5x + G92 - useful for probe (G38)
        // L11 - ref g59.3 only
        // Q: 1 - 8: 1: 135, 2: 45, 3: 315, 4: 225, 5: 180, 6: 90, 7: 0, 8: 270

        void SaveOffset(string axis)
        {
            string axes;
            Position newpos = new Position(offset);

            newpos.X = GrblWorkParameters.ConvertX(GrblWorkParameters.LatheMode, GrblParserState.LatheMode, selectedTool.X);

            switch (axis)
            {
                case "R":
                    axes = "R{4}";
                    break;

                case "All":
                    axes = newpos.ToString(GrblInfo.AxisFlags);
                    break;

                default:
                    axes = newpos.ToString(GrblInfo.AxisLetterToFlag(axis));
                    break;
            }

            Comms.com.WriteCommand(string.Format("G10L1P{0}{1}", selectedTool.Code, axes));
        }

        void cvOffset_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTool != null)
            {
                string axisletter = (string)((CoordValueSetControl)sender).Tag;
                int axis = GrblInfo.AxisLetterToIndex(axisletter);

                selectedTool.Values[axis] = offset.Values[axis];
                SaveOffset(axisletter);
            }
        }

        private void btnSetAll_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTool != null)
            {
                for (var i = 0; i < offset.Values.Length; i++)
                    selectedTool.Values[i] = offset.Values[i];
            }

            SaveOffset("All");
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTool != null)
            {
                for (var i = 0; i < offset.Values.Length; i++)
                    offset.Values[i] = selectedTool.Values[i] = 0d;

                SaveOffset("All");
            }
        }

        private void RequestStatus()
        {
            if (double.IsNaN(_model.WorkPosition.X))
            {
                Comms.com.WriteByte(GrblConstants.CMD_STATUS_REPORT_ALL);
            }
            else
            {
                awaitCoord = double.IsNaN(_model.MachinePosition.Values[0]);
            }

        }

        void btnCurrPos_Click(object sender, RoutedEventArgs e)
        {
            bool? res = null;
            CancellationToken cancellationToken = new CancellationToken();
            awaitCoord = true;
            _model.OnRealtimeStatusProcessed += DataReceived;
            _model.Clear();
            _model.MachinePosition.Clear();
            new Thread(() =>
            {
                res = WaitFor.AckResponse<string>(
                    cancellationToken,
                    null,
                    a => GotPosition += a,
                    a => GotPosition -= a,
                    1000, RequestStatus);
            }).Start();

            while (res == null)
                EventUtils.DoEvents();
            _model.OnRealtimeStatusProcessed -= DataReceived;
        }

        private void DataReceived(string data)
        {
            if (awaitCoord)
            {
                Thread.Sleep(50);
                Comms.com.WriteByte(GrblConstants.CMD_STATUS_REPORT);
                awaitCoord = double.IsNaN(_model.MachinePosition.Values[0]);
            }
            else
            {
                GotPosition?.Invoke("ok");
                offset.Set(_model.MachinePosition);
            }
        }
    }
}
