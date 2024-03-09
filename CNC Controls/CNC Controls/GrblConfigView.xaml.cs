/*
 * GrblConfigView.xaml.cs - part of CNC Controls library for Grbl
 *
 * v0.36 / 2021-11-01 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2018-2021, Io Engineering (Terje Io)
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
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace CNC.Controls
{
    public partial class GrblConfigView : UserControl
    {
        private Widget curSetting = null;
        private GrblViewModel _model;

        private string retval;

        public GrblConfigView(GrblViewModel model)
        {
            InitializeComponent();
            _model = model;
        }

        private void ConfigView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new WidgetViewModel();
            dgrSettings.Visibility = GrblInfo.HasEnums ? Visibility.Collapsed : Visibility.Visible;
            treeView.Visibility = !GrblInfo.HasEnums ? Visibility.Collapsed : Visibility.Visible;
            details.Visibility = GrblInfo.HasEnums && curSetting == null ? Visibility.Hidden : Visibility.Visible;

            if (GrblInfo.HasEnums)
            {
                treeView.ItemsSource = GrblSettingGroups.Groups;
            }
            else
            {
                dgrSettings.DataContext = GrblSettings.Settings;
                dgrSettings.SelectedIndex = 0;
            }
        }

        void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (curSetting != null)
                curSetting.Assign();

            _model.Message = string.Empty;

            GrblSettings.Save();
            _model.SettingsLoaded();
        }

        void btnReload_Click(object sender, RoutedEventArgs e)
        {
            using (new UIUtils.WaitCursor())
            {
                GrblSettings.Load();
            }
        }

        void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            GrblSettings.Backup(string.Format("{0}settings.txt", Core.Resources.Path));
            _model.Message = string.Format((string)FindResource("SettingsWritten"), "settings.txt");
        }

        private void ShowSetting(GrblSettingDetails setting, bool assign)
        {
            details.Visibility = Visibility.Visible;

            if (curSetting != null)
            {
                if (assign)
                    curSetting.Assign();
                canvas.Children.Clear();
                curSetting.Dispose();
            }
            txtDescription.Text = setting.Description;
            curSetting = new Widget(this, new WidgetProperties(setting), canvas);
            curSetting.IsEnabled = true;
        }

        public bool LoadFile(string filename)
        {
            int pos, id;
            var lines = new List<string>();
            var dep = new List<int>();
            var settings = new Dictionary<int, string>();
            var file = new FileInfo(filename);
            var sr = file.OpenText();
            var block = sr.ReadLine();

            while (block != null)
            {
                block = block.Trim();
                try
                {
                    if (lines.Count == 0 && _model.IsGrblHAL && block == "%")
                        lines.Add(block);
                    else if (block.StartsWith("$") && (pos = block.IndexOf('=')) > 1)
                    {
                        if (int.TryParse(block.Substring(1, pos - 1), out id))
                            settings.Add(id, block.Substring(pos + 1));
                        else
                            lines.Add(block);
                    }

                    block = sr.ReadLine();
                }
                catch (Exception e)
                {
                    if (MessageBox.Show(((string)FindResource("SettingsFail")).Replace("\\n", "\r\r"), 
                            e.Message, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        block = sr.ReadLine();
                    else
                    {
                        block = null;
                        settings.Clear();
                        lines.Clear();
                    }
                }
            }

            sr.Close();

            if (settings.Count == 0)
                MessageBox.Show((string)FindResource("SettingsInvalid"));
            else
            {
                bool? res = null;
                CancellationToken cancellationToken = new CancellationToken();
                dep.Add((int)GrblSetting.HomingEnable);

                foreach (var cmd in lines)
                {
                    res = null;
                    if (cmd.Equals("%"))
                    {
                        Comms.com.WriteCommand(cmd);

                    }
                }

                foreach (var d in dep)
                {
                    if (settings.ContainsKey(d))
                    {
                        if (!SetSetting(new KeyValuePair<int, string>(d, settings[d])))
                        {
                            settings.Clear();
                            break;
                        }
                    }
                }
                LoadSettings(settings);
            }
            return settings.Count > 0;
        }

        private List<string> LoadSettings(Dictionary<int, string> grblSettings)
        {
            try
            {
                var retVal = new List<string>();
                bool? res = null;
                var cancellationToken = new CancellationToken();
                _model.Poller.SetState(0);

                void ProcessSettings(string response)
                {
                    if (response != "ok")
                    {
                        retVal.Add(response);
                    }
                }

                void SendSettings()
                {
                    Comms.com.DataReceived -= ProcessSettings;
                    Comms.com.DataReceived += ProcessSettings;
                    foreach (var setting in grblSettings)
                    {

                        var cmdr = $"${setting.Key}={setting.Value}";
                        Comms.com.WriteCommand(cmdr);
                        Thread.Sleep(50);
                    }
                    Comms.com.DataReceived -= ProcessSettings;
                    _model.Poller.SetState(_model.PollingInterval);
                    SetLoaded(retVal);
                }

                Task.Factory.StartNew(SendSettings, cancellationToken);

                return retVal;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _model.Poller.SetState(200);
                return null;
            }
        }
        private void SetLoaded(List<string> reList)
        {

            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                Comms.com.WriteCommand("%");
                using (new UIUtils.WaitCursor())
                {
                    GrblSettings.Load();
                }

                var errors = reList;
                _model.Message = string.Empty;
                var messageString = errors.Count > 1 ? "Settings Loaded with Errors" : "Settings Loaded";
                MessageBox.Show( messageString, "Settings Restore Completed", MessageBoxButton.OK);

            }));
        }

        private bool SetSetting(KeyValuePair<int, string> setting)
        {
            bool? res = null;
            CancellationToken cancellationToken = new CancellationToken();
            var scmd = $"${setting.Key}={setting.Value}";

            retval = string.Empty;

            new Thread(() =>
            {
                res = cancellationToken.AckResponse<string>(response => Process(response),
                    a => _model.OnResponseReceived += a,
                    a => _model.OnResponseReceived -= a,
                    400, () => Comms.com.WriteCommand(scmd));
            }).Start();

            while (res == null)
            {
                EventUtils.DoEvents();
            }

            if (retval != string.Empty)
            {
                if (retval.StartsWith("error:"))
                {
                    var msg = GrblErrors.GetMessage(retval.Substring(6));
                    if (msg != retval)
                        retval += " - \"" + msg + "\"";
                }
                var settingDetails = GrblSettings.Get((GrblSetting)setting.Key);

                var results = MessageBox.Show(string.Format((string)FindResource("SettingsError"), scmd, retval),
                    "ioSender" + (settingDetails == null ? "" : " - " + settingDetails.Name),
                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No;
                if (results)
                {
                    return false;
                }

            }
            else if (res == false &&
                     MessageBox.Show(string.Format((string)FindResource("SettingsTimeout"), scmd),
                         "ioSender", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
            {
                return false;
            }

            return true;
        }

        private void Process(string data)
        {
            if (data != "ok")
            {
                //filtering out homing error and machine state from the setting responses.
                //TODO FIX FOR LATTER.  Better fix would be to stop state polling while sending setting 
                if (data.StartsWith("error:9") || data.StartsWith("<Alarm|MPos:") || data.StartsWith("<Idle|MPos:")) return;
                {
                    retval = data;
                }
            }
        }
        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();

            file.InitialDirectory = Core.Resources.Path;
            file.Title = (string)FindResource("SettingsRestore");

            file.Filter = string.Format("Text files (*.txt)|*.txt");

            if (file.ShowDialog() == true)
            {
                using (new UIUtils.WaitCursor())
                {
                    LoadFile(file.FileName);
                }
            }
        }

        private void dgrSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
                ShowSetting(e.AddedItems[0] as GrblSettingDetails, true);
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e != null && e.NewValue is GrblSettingDetails && (e.NewValue as GrblSettingDetails).Value != null)
                ShowSetting(e.NewValue as GrblSettingDetails, true);
            else
                details.Visibility = Visibility.Hidden;
        }


        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is TreeViewItem tvi) || e.Handled) return;

            tvi.IsExpanded = !tvi.IsExpanded;
            e.Handled = true;
        }
    }
}
