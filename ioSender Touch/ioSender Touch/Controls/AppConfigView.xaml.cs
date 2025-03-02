/*
 * AppConfigView.xaml.cs - part of CNC Controls library for Grbl
 *
 * v0.37 / 2021-02-27 / Io Engineering (Terje Io)
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

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Config;
using ioSenderTouch.ViewModels;
using LibStrings = ioSenderTouch.GrblCore.Config.LibStrings;

namespace ioSenderTouch.Controls
{
    public partial class AppConfigView : UserControl
    {
       
        private GrblViewModel _grblModel;
        private readonly AppConfigViewModel _vModel;

        public AppConfigView(GrblViewModel grblViewModel, ContentManager contentManager)
        {

            InitializeComponent();
            _grblModel = grblViewModel;
             _vModel = new AppConfigViewModel();
            contentManager.RegisterViewAndModel("appSettingsView", _vModel);
        }

        public AppConfigView()
        {
            InitializeComponent();
        }

        public void Setup(ObservableCollection<UserControl> controls)
        {
            DataContext = _vModel.ConfigBase;
            xx.ItemsSource = controls;
        }


    }
}