/*
 * Renderer.xaml.cs - part of CNC Controls library
 *
 * v0.36 / 2021-12-25 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2019-2021, Io Engineering (Terje Io)
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

using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Config;
using ioSenderTouch.ViewModels;

namespace ioSenderTouch.Controls.Render
{
    public partial class RenderView : UserControl
    {
        private readonly GrblViewModel _grblViewModel;
        private static bool keyboardMappingsOk = false;
        private readonly RenderViewModel _model;

        public RenderView()
        {
            InitializeComponent();
        }
        public RenderView(GrblViewModel grblViewModel, ContentManager manager)
        {
            _grblViewModel = grblViewModel;
            DataContext = _grblViewModel;
            _grblViewModel.RenderVM = _model = new RenderViewModel(grblViewModel);
            InitializeComponent();
            manager.RegisterViewAndModel(nameof(RenderView), _model);
            grblViewModel.GrblInitialized += GrblViewModel_GrblInitialized;
        }

        private void GrblViewModel_GrblInitialized(object sender, System.EventArgs e)
        {
            GCode.File.Model = _grblViewModel;
            GCode.File.FileLoaded += File_FileLoaded;
        }

        private void File_FileLoaded(object sender, bool fileLoaded)
        {
            if (fileLoaded)
            {
                Open(GCode.File.Tokens);
            }
            else
            {
                Close();
            }
        }

        public Machine MachineView
        {
            get { return gcodeView.Machine; }
        }

        public void Close()
        {
            gcodeView.ClearViewport();
        }

        public void Open(List<GCodeToken> tokens)
        {
            gcodeView.Render(tokens);
            //gcodeView.ShowPosition();
        }

        private bool ToggleGrid(Key key)
        {
            MachineView.ShowGrid = !MachineView.ShowGrid;
            return true;
        }
        private bool ToggleJobEnvelope(Key key)
        {
            MachineView.ShowJobEnvelope = !MachineView.ShowJobEnvelope;
            return true;
        }
        private bool ToggleWorkEnvelope(Key key)
        {
            MachineView.ShowWorkEnvelope = !MachineView.ShowWorkEnvelope;
            return true;
        }
        private bool RestoreView(Key key)
        {
            gcodeView.RestoreView();
            return true;
        }
        private bool ResetView(Key key)
        {
            gcodeView.ResetView();
            return true;
        }

        private void ResetView_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            gcodeView.ResetView();
        }

        private void SaveView_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            gcodeView.SaveView();
        }

        private void RestoreView_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            gcodeView.RestoreView();
        }

        private void RenderControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!keyboardMappingsOk && DataContext is GrblViewModel)
            {
                KeypressHandler keyboard = _grblViewModel.Keyboard;

                keyboardMappingsOk = true;

                keyboard.AddHandler(Key.V, ModifierKeys.Control, ResetView);
                keyboard.AddHandler(Key.R, ModifierKeys.Control, RestoreView);
                keyboard.AddHandler(Key.G, ModifierKeys.Control, ToggleGrid);
                keyboard.AddHandler(Key.J, ModifierKeys.Control, ToggleJobEnvelope);
                keyboard.AddHandler(Key.W, ModifierKeys.Control, ToggleWorkEnvelope);
            }
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(_grblViewModel.GrblState.State == GrblStates.Tool)
                Comms.com.WriteByte(GrblConstants.CMD_CYCLE_START);
        }
    }
}
