﻿/*
 * WorkParametersControl.xaml.cs - part of CNC Controls library
 *
 * v0.27 / 2020-09-11 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2018-2020, Io Engineering (Terje Io)
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

using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CNC.Core;
using System.Windows;

namespace CNC.Controls
{
    public partial class WorkParametersControlTouch : UserControl
    {
        public new bool IsFocused => cbxTool.IsFocused || cbxOffset.IsFocused;

        public WorkParametersControlTouch()
        {
            InitializeComponent();
        }


        void cbxTool_KeyPress(object sender, KeyEventArgs e)
        {
            // UIUtils.ProcessMask((Control)sender, e);

            //if (e.KeyChar == '\r' && cbxTool.Text != "")
            //{
            //    if (!cbxTool.Items.Contains(cbxTool.Text))
            //        cbxTool.Items.Add(cbxTool.Text);
            //    cbxTool.SelectedItem = cbxTool.Text;
            //    cbxTool_TextChanged(cbxTool, null);
            //}
        }

        private void cbxOffset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && ((ComboBox)sender).IsDropDownOpen)
                (DataContext as GrblViewModel)?.ExecuteCommand(((CoordinateSystem)e.AddedItems[0]).Code);
        }

        private void cbxTool_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && ((ComboBox)sender).IsDropDownOpen)
                (DataContext as GrblViewModel)?.ExecuteCommand(string.Format(GrblCommand.ToolChange, ((Tool)e.AddedItems[0]).Code));
        }
    }
}