using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ioSenderTouch.Controls;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Comands;
using Action = ioSenderTouch.GrblCore.Action;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;


namespace ioSenderTouch.ViewModels
{
    public class SdCardViewModel : ViewModelBase, IActiveViewModel
    {
        public delegate void FileSelectedHandler(string filename, bool rewind);
        public event FileSelectedHandler FileSelected;
        private bool _mounted;
        private int _id = 0;
        private readonly GrblViewModel _model;
        private bool _active;
        private bool _initalized = false;
        private bool _canUpload;
        private GCodeFile _selectedFile;
        private bool _rewind;
        private bool _viewAll;
        private bool _rowSelected;

        public ObservableCollection<GCodeFile>GCodeFiles { get; set; } = [] ;
        public bool RowSelected
        {
            get => _rowSelected;
            set
            {
                if (value == _rowSelected) return;
                _rowSelected = value;
                OnPropertyChanged();
            }
        }
        public GCodeFile SelectedFile
        {
            get => _selectedFile;
            set
            {
                _selectedFile = value;
                RowSelected = _selectedFile != null;
                OnPropertyChanged();
            }
        }
        public bool Active
        {
            get => _active;
            set
            {
                if (value == _active) return;
                _active = value;
                OnPropertyChanged();
            }
        }
        public bool ViewAll
        {
            get => _viewAll;
            set
            {
                if (value == _viewAll) return;
                _viewAll = value;
                OnPropertyChanged();
            }
        }
        public bool CanUpload
        {
            get => _canUpload;
            set
            {
                if (value == _canUpload) return;
                _canUpload = value;
                OnPropertyChanged();
            }
        }
        public bool Rewind { get; set; } = true;
        public ICommand DeleteCommand { get; }
        public ICommand ViewAllCommand { get; }
        public ICommand UploadCommand { get; }
        public ICommand DownLoadRunCommand { get; }
        public ICommand RunCommand { get; }
        public ICommand RunNowCommand { get; }
        public string Name { get; }
       
        public SdCardViewModel(GrblViewModel model)
        {
            _model = model;
            Name = nameof(SdCardViewModel);
            ViewAllCommand = new Command(SetViewAll);
            UploadCommand = new Command(Upload);
            DownLoadRunCommand = new Command(DownLoadRun);
            RunCommand = new Command(Run);
            DeleteCommand = new Command(Delete);
            RunNowCommand = new Command(RunNow);
        }
        private void RunNow(object obj)
        {
            RunFile();
        }
        private void Run(object obj)
        {
            RunFile();
        }

        public void Activated()
        {
            if (_initalized) return;
            CanUpload = GrblInfo.UploadProtocol != string.Empty;
            ViewAll = true;
            LoadSdCard();
        }

        private void LoadSdCard()
        {
            var results = Load(_model, ViewAll);
            ViewAll = results is { IsFaulted: false };
            _initalized = ViewAll;
        }

        private void AddBlock(string data)
        {
            GCode.File.AddBlock(data);
        }

        private void DownLoadRun(object x)
        {
            if (SelectedFile != null && MessageBox.Show($"Download and run {SelectedFile.FileName}?", "IOT",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                using (new UIUtils.WaitCursor())
                {
                    bool? res = null;
                    CancellationToken cancellationToken = new CancellationToken();

                    Comms.com.PurgeQueue();

                    _model.SuspendProcessing = true;
                    _model.Message = $"Downloading {SelectedFile.FileName}...";

                    GCode.File.AddBlock(SelectedFile.FileName, Action.New);

                    new Thread(() =>
                    {
                        res = WaitFor.AckResponse<string>(
                            cancellationToken,
                            response => AddBlock(response),
                            a => _model.OnResponseReceived += a,
                            a => _model.OnResponseReceived -= a,
                            400, () => Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_DUMP + SelectedFile.FileName));
                    }).Start();

                    while (res == null)
                        EventUtils.DoEvents();

                    _model.SuspendProcessing = false;

                    GCode.File.AddBlock(string.Empty, Action.End);
                }

                _model.Message = string.Empty;

                if (SelectedFile.Rewind)
                    Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_REWIND);

                FileSelected?.Invoke($"SDCard:{SelectedFile.FileName}", SelectedFile.Rewind);
                Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_RUN + SelectedFile.FileName);
                GCode.File.SdCardFileLoaded();
                SelectedFile.Rewind = false;
            }
        }

        private void SetViewAll(object x)
        {
            var t = Load(_model, ViewAll);
        }
        private void Upload(object x)
        {
            bool ok = false;
            string filename = string.Empty;
            OpenFileDialog file = new OpenFileDialog();

            file.Filter =
                string.Format(
                    "GCode files ({0})|{0}|GCode macros (*.macro)|*.macro|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileUtils.ExtensionsToFilter(GCode.FileTypes));

            if (file.ShowDialog() == true)
            {
                filename = file.FileName;
            }

            if (filename != string.Empty)
            {
                _model.Message = "Uploading...";

                if (GrblInfo.UploadProtocol == "FTP")
                {
                    if (GrblInfo.IpAddress == string.Empty)
                        _model.Message = "No connection.";
                    else
                        using (new UIUtils.WaitCursor())
                        {
                            _model.Message = "Uploading...";
                            try
                            {
                                using (WebClient client = new WebClient())
                                {
                                    client.Credentials = new NetworkCredential("grblHAL", "grblHAL");
                                    client.UploadFile(
                                        string.Format("ftp://{0}/{1}", GrblInfo.IpAddress,
                                            filename.Substring(filename.LastIndexOf('\\') + 1)),
                                        WebRequestMethods.Ftp.UploadFile, filename);
                                    ok = true;
                                }
                            }
                            catch (WebException ex)
                            {
                                _model.Message = ex.Message.ToString() + " " +
                                                 ((FtpWebResponse)ex.Response).StatusDescription;
                            }
                            catch (System.Exception ex)
                            {
                                _model.Message = ex.Message.ToString();
                            }
                        }
                }
                else
                {
                    _model.Message = "Uploading...";
                    YModem ymodem = new YModem();
                    ymodem.DataTransferred += Ymodem_DataTransferred;
                    ok = ymodem.Upload(filename);
                }

                if (!(GrblInfo.UploadProtocol == "FTP" && !ok))
                    _model.Message = ok ? "TransferDone" : "TransferAborted";

                var t = Load(_model, ViewAll);
            }
        }
        private void Ymodem_DataTransferred(long size, long transferred)
        {
            _model.Message = $"Transferred {transferred} of {size} bytes...";
        }
        private void Delete(object x)
        {
            if (SelectedFile == null) return;
            var selectedFile = SelectedFile.FileName;
            if (string.IsNullOrEmpty(selectedFile)) return;
            if (MessageBox.Show($"Delete {SelectedFile.FileName}?", "IOT", MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                GCodeFiles.Clear();
                Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_UNLINK + SelectedFile.FileName);
                var t = Load(_model, ViewAll);
            }
        }
        private void RunFile()
        {
            if (SelectedFile != null)
            {
                if (!SelectedFile.Valid)
                {
                    MessageBox.Show(
                        $"File:{SelectedFile.FileName}!,?,~ and SPACE is not supported in filenames, please rename ",
                        "IOT",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (Rewind)
                    {
                        Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_REWIND);
                    }

                    FileSelected?.Invoke("SDCard: { SelectedFile.FileName}", Rewind);
                    Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_RUN + SelectedFile.FileName);
                    Rewind = false;
                }
            }
        }

        public async Task<bool?> Load(GrblViewModel model, bool viewAll)
        {
            bool? res = null;
            CancellationToken cancellationToken = new CancellationToken();
            //SendSettings(model, GrblConstants.CMD_SDCARD_MOUNT, "ok");
            if (!_mounted)
            {
                Comms.com.PurgeQueue();

                new Thread(() =>
                {
                    _mounted = WaitFor.AckResponse<string>(
                        cancellationToken,
                        null,
                        a => model.OnResponseReceived += a,
                        a => model.OnResponseReceived -= a,
                        500, () => Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_MOUNT));
                }).Start();

                while (!_mounted)
                    EventUtils.DoEvents();
            }

            if (_mounted == true)
            {
                Comms.com.PurgeQueue();
                _id = 0;
                model.Silent = true;

                new Thread(() =>
                {
                    res = WaitFor.AckResponse<string>(
                        cancellationToken,
                        response => Process(response),
                        a => model.OnResponseReceived += a,
                        a => model.OnResponseReceived -= a,
                        2000,
                        () => Comms.com.WriteCommand(viewAll
                            ? GrblConstants.CMD_SDCARD_DIR_ALL
                            : GrblConstants.CMD_SDCARD_DIR));
                }).Start();

                while (res == null)
                    EventUtils.DoEvents();
                model.Silent = false;

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            return _mounted;
        }
        private void Process(string data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => Process(data));
                return;
            }
            string filename = "";
            int filesize = 0;
            bool invalid = false;

            if (data.StartsWith("[FILE:"))
            {
                string[] parameters = data.TrimEnd(']').Split('|');
                foreach (string parameter in parameters)
                {
                    string[] valuepair = parameter.Split(':');
                    switch (valuepair[0])
                    {
                        case "[FILE":
                            filename = valuepair[1];
                            break;

                        case "SIZE":
                            filesize = int.Parse(valuepair[1]);
                            break;

                        case "INVALID":
                            invalid = true;
                            break;
                    }
                }

                GCodeFiles.Add(new GCodeFile(_id++, filename, filesize, !invalid));

            }
        }
        public void Deactivated()
        {

        }
    }
}
public class GCodeFile(int id, string fileName, int fileSize, bool valid)
{
    public int Id { get; set; } = id;
    public string FileName { get; set; } = fileName;
    public int FileSize { get; set; } = fileSize;
    public bool Valid { get; set; } = valid;
    public bool Rewind { get; set; }
}





