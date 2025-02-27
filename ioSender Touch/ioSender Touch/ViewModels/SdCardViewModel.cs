using System;
using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ioSenderTouch.Controls;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Comands;
using Action = ioSenderTouch.GrblCore.Action;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;


namespace ioSenderTouch.ViewModels
{
    public class SdCardViewModel : ViewModelBase, IActiveViewModel
    {
        public delegate void FileSelectedHandler(string filename, bool rewind);
        public event FileSelectedHandler FileSelected;

        private readonly GrblViewModel _model;
        private bool _active;
        private bool _initalized = false;
        private bool _canRewind;
        private bool _canViewAll;
        private bool _canDelete;
        private bool _canUpload;
        private GrblSDCard _grblSdCard;
        private DataRow _currentFile;
        private DataRow _selectedFile;
        private bool _rewind;
        private bool _viewAll;

        public DataRow CurrentFile
        {
            get => _currentFile;
            set
            {
                if (Equals(value, _currentFile)) return;
                _currentFile = value;
                OnPropertyChanged();
            }
        }
        public DataRow SelectedFile
        {
            get => _currentFile;
            set
            {
                if (Equals(value, _selectedFile)) return;
                _selectedFile = value;
                OnPropertyChanged();
            }
        }

        public GrblSDCard GrblSdCard
        {
            get => _grblSdCard;
            set
            {
                if (Equals(value, _grblSdCard)) return;
                _grblSdCard = value;
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

        public bool CanViewAll
        {
            get => _canViewAll;
            set
            {
                if (value == _canViewAll) return;
                _canViewAll = value;
                OnPropertyChanged();
            }
        }

        public bool CanDelete
        {
            get => _canDelete;
            set
            {
                if (value == _canDelete) return;
                _canDelete = value;
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
        public bool Rewind
        {
            get => _rewind;
            set
            {
                if (value == _rewind) return;
                _rewind = value;
                OnPropertyChanged();
            }
        }

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
            GrblSdCard = new GrblSDCard();
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
            CanDelete = GrblInfo.Build >= 20210421;
            CanViewAll = GrblInfo.Build >= 20230312;
            CanRewind = GrblInfo.IsGrblHAL;
            ViewAll = true;
            //var t =  Task.Run((() =>
            //{
            //    GrblSdCard.Load(_model, ViewAll);

            //}));
            LoadSdCard();
           
        }

        private async void  LoadSdCard()
        {
            var results = await GrblSdCard.Load(_model, ViewAll);
            if (results != null && results.Value)
            {
                ViewAll = true;
            }
            else
            {
                ViewAll = false;
            }

            _initalized = ViewAll;
        }
        public bool CanRewind
        {
            get => _canRewind;
            set
            {
                if (value == _canRewind) return;
                _canRewind = value;
                OnPropertyChanged();
            }
        }
        private void AddBlock(string data)
        {
            GCode.File.AddBlock(data);
        }

        private void DownLoadRun(object x)
        {
            if (CurrentFile != null && MessageBox.Show($"Download and run {CurrentFile["Name"]}?", "IOT", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                using (new UIUtils.WaitCursor())
                {
                    bool? res = null;
                    CancellationToken cancellationToken = new CancellationToken();

                    Comms.com.PurgeQueue();

                    _model.SuspendProcessing = true;
                    _model.Message = $"Downloading {CurrentFile["Name"]}...";

                    GCode.File.AddBlock((string)CurrentFile["Name"], Action.New);

                    new Thread(() =>
                    {
                        res = WaitFor.AckResponse<string>(
                            cancellationToken,
                            response => AddBlock(response),
                            a => _model.OnResponseReceived += a,
                            a => _model.OnResponseReceived -= a,
                            400, () => Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_DUMP + (string)CurrentFile["Name"]));
                    }).Start();

                    while (res == null)
                        EventUtils.DoEvents();

                    _model.SuspendProcessing = false;

                    GCode.File.AddBlock(string.Empty, Action.End);
                }

                _model.Message = string.Empty;

                if (Rewind)
                    Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_REWIND);

                FileSelected?.Invoke("SDCard:" + (string)CurrentFile["Name"], Rewind);
                Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_RUN + (string)CurrentFile["Name"]);

                Rewind = false;
            }
        }

        
        private void SetViewAll(object x)
        {
           var t = _grblSdCard.Load(_model, ViewAll);
        }

        private void Upload(object x)
        {

            bool ok = false;
            string filename = string.Empty;
            OpenFileDialog file = new OpenFileDialog();

            file.Filter = string.Format("GCode files ({0})|{0}|GCode macros (*.macro)|*.macro|Text files (*.txt)|*.txt|All files (*.*)|*.*", FileUtils.ExtensionsToFilter(GCode.FileTypes));

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
                    else using (new UIUtils.WaitCursor())
                    {
                            _model.Message = "Uploading...";
                            try
                            {
                                using (WebClient client = new WebClient())
                                {
                                    client.Credentials = new NetworkCredential("grblHAL", "grblHAL");
                                    client.UploadFile(string.Format("ftp://{0}/{1}", GrblInfo.IpAddress, filename.Substring(filename.LastIndexOf('\\') + 1)), WebRequestMethods.Ftp.UploadFile, filename);
                                    ok = true;
                                }
                            }
                            catch (WebException ex)
                            {
                                _model.Message = ex.Message.ToString() + " " + ((FtpWebResponse)ex.Response).StatusDescription;
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

                _grblSdCard.Load(_model, ViewAll);
            }
        }

        private void Ymodem_DataTransferred(long size, long transferred)
        {
            _model.Message = $"Transferred {transferred} of {size} bytes...";
        }

        private void Delete(object x)
        {
            if (SelectedFile == null) return;
            var selectedFile = (string)CurrentFile["Name"];
            if (string.IsNullOrEmpty(selectedFile)) return;
            if (MessageBox.Show($"Delete {CurrentFile["Name"]}?", "IOT", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_UNLINK + (string)CurrentFile["Name"]);
               var t= _grblSdCard.Load(_model, ViewAll);
            }
        }

        private void RunFile()
        {
            if (CurrentFile != null)
            {
                if ((bool)CurrentFile["Invalid"])
                {
                    MessageBox.Show($"File:{CurrentFile["Name"]}!,?,~ and SPACE is not supported in filenames, please rename ", "IOT",
                                     MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (Rewind)
                    {
                        Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_REWIND);
                    }
                    FileSelected?.Invoke("SDCard:" + (string)CurrentFile["Name"], Rewind);
                    Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_RUN + (string)CurrentFile["Name"]);
                    Rewind = false;
                }
            }
        }

        public void Deactivated()
        {

        }
    }
    public class GrblSDCard
    {
        private DataTable dataTable;
        private bool? mounted = null;
        private int id = 0;

        public GrblSDCard()
        {
            dataTable = new DataTable("Filelist");

            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Dir", typeof(string));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Size", typeof(int));
            dataTable.Columns.Add("Invalid", typeof(bool));
            dataTable.PrimaryKey = new DataColumn[] { dataTable.Columns["Id"] };
        }

        public DataView Files => dataTable.DefaultView;
        public bool Loaded => dataTable.Rows.Count > 0;

        public async Task<bool?> Load(GrblViewModel model, bool viewAll)
        {
            bool? res = null;
            CancellationToken cancellationToken = new CancellationToken();

            dataTable.Clear();
            //SendSettings(model, GrblConstants.CMD_SDCARD_MOUNT, "ok");
            if (mounted == null)
            {
                Comms.com.PurgeQueue();

                new Thread(() =>
                {
                    mounted = WaitFor.AckResponse<string>(
                        cancellationToken,
                        null,
                        a => model.OnResponseReceived += a,
                        a => model.OnResponseReceived -= a,
                        500, () => Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_MOUNT));
                }).Start();

                while (mounted == null)
                    EventUtils.DoEvents();
            }

            if (mounted == true)
            {
                Comms.com.PurgeQueue();

                id = 0;
                model.Silent = true;

              new Thread(() =>
                {
                    res = WaitFor.AckResponse<string>(
                        cancellationToken,
                        response => Process(response),
                        a => model.OnResponseReceived += a,
                        a => model.OnResponseReceived -= a,
                        2000, () => Comms.com.WriteCommand(viewAll ? GrblConstants.CMD_SDCARD_DIR_ALL : GrblConstants.CMD_SDCARD_DIR));
                }).Start();

                while (res == null)
                    EventUtils.DoEvents();
                model.Silent = false;

                dataTable.AcceptChanges();
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            return mounted;
        }
        private async Task SendSettings(GrblViewModel model, string command, string key)
        {
            try
            {

                bool res = false;
                var cancellationToken = new CancellationToken();
                model.Poller.SetState(0);

                void ProcessSettings(string response)
                {
                    if (response.StartsWith(key))
                    {
                        Process(response);
                        res = true;
                    }
                }
                void Send()
                {
                    Comms.com.DataReceived -= ProcessSettings;
                    Comms.com.DataReceived += ProcessSettings;
                    Comms.com.WriteCommand(command);
                    while (!res)
                    {
                        Task.Delay(50, cancellationToken);
                    }
                    Comms.com.DataReceived -= ProcessSettings;
                    model.Poller.SetState(model.PollingInterval);
                }

                await Task.Factory.StartNew(Send, cancellationToken);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                model.Poller.SetState(200);
            }
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

                dataTable.Rows.Add(new object[] { id++, "", filename, filesize, invalid });
            }
        }
    }
}

