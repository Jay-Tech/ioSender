
using System.Windows.Controls;
using ioSenderTouch.GrblCore;
using ioSenderTouch.ViewModels;

namespace ioSenderTouch.Controls
{
    /// <summary>
    /// Interaction logic for SDCardView.xaml
    /// </summary>
    public partial class SDCardView : UserControl
    {
        private readonly ContentManager _contentManager;

        public SDCardView(GrblViewModel grblViewModel, ContentManager contentManager)
        {
            _contentManager = contentManager;
            InitializeComponent();
            var vm = new SdCardViewModel(grblViewModel);
            DataContext = vm;
            _contentManager.RegisterViewAndModel(nameof(SDCardView), vm);
        }

        //private void GrblViewModel_GrblInitialized(object sender, EventArgs e)
        //{
        //    SetupView();
        //}

        //public void SetupView()
        //{
        //    CanUpload = GrblInfo.UploadProtocol != string.Empty;
        //    CanDelete = GrblInfo.Build >= 20210421;
        //    CanViewAll = GrblInfo.Build >= 20230312;
        //    CanRewind = GrblInfo.IsGrblHAL;
        //    Task.Run((() =>
        //    {
        //        _grblSdCard.Load(_viewModel, ViewAll);
                
        //    }));

        //}

        //public void CloseFile()
        //{
        //}



        //public static readonly DependencyProperty RewindProperty = DependencyProperty.Register(nameof(Rewind), typeof(bool), typeof(SDCardView), new PropertyMetadata(false));
        //public bool Rewind
        //{
        //    get { return (bool)GetValue(RewindProperty); }
        //    set { SetValue(RewindProperty, value); }
        //}

        //public static readonly DependencyProperty CanRewindProperty = DependencyProperty.Register(nameof(CanRewind), typeof(bool), typeof(SDCardView), new PropertyMetadata(false));
        //public bool CanRewind
        //{
        //    get { return (bool)GetValue(CanRewindProperty); }
        //    set { SetValue(CanRewindProperty, value); }
        //}

        //public static readonly DependencyProperty ViewAllProperty = DependencyProperty.Register(nameof(ViewAll), typeof(bool), typeof(SDCardView), new PropertyMetadata(false));
        //public bool ViewAll
        //{
        //    get { return (bool)GetValue(ViewAllProperty); }
        //    set { SetValue(ViewAllProperty, value); }
        //}

        //public static readonly DependencyProperty CanViewAllProperty = DependencyProperty.Register(nameof(CanViewAll), typeof(bool), typeof(SDCardView), new PropertyMetadata(false));
        //public bool CanViewAll
        //{
        //    get { return (bool)GetValue(CanViewAllProperty); }
        //    set { SetValue(CanViewAllProperty, value); }
        //}

        //public static readonly DependencyProperty CanUploadProperty = DependencyProperty.Register(nameof(CanUpload), typeof(bool), typeof(SDCardView), new PropertyMetadata(false));
        //public bool CanUpload
        //{
        //    get { return (bool)GetValue(CanUploadProperty); }
        //    set { SetValue(CanUploadProperty, value); }
        //}

        //public static readonly DependencyProperty CanDeleteProperty = DependencyProperty.Register(nameof(CanDelete), typeof(bool), typeof(SDCardView), new PropertyMetadata(false));


        //public bool CanDelete
        //{
        //    get { return (bool)GetValue(CanDeleteProperty); }
        //    set { SetValue(CanDeleteProperty, value); }
        //}

      
        //private void SDCardView_Loaded(object sender, RoutedEventArgs e)
        //{
        //   // dgrSDCard.DataContext = _grblSdCard.Files;
        //}

        //void dgrSDCard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    currentFile = e.AddedItems.Count == 1 ? ((DataRowView)e.AddedItems[0]).Row : null;
        //}

        //private void dgrSDCard_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
            
        //}

        //private void AddBlock(string data)
        //{
        //    GCode.File.AddBlock(data);
        //}

    //    private void DownloadRun_Click(object sender, RoutedEventArgs e)
    //    {
    //        if (currentFile != null && MessageBox.Show(string.Format((string)FindResource("DownloandRun"), (string)currentFile["Name"]), "ioSender", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
    //        {


    //            using (new UIUtils.WaitCursor())
    //            {
    //                bool? res = null;
    //                CancellationToken cancellationToken = new CancellationToken();

    //                Comms.com.PurgeQueue();

    //                _viewModel.SuspendProcessing = true;
    //                _viewModel.Message = string.Format((string)FindResource("Downloading"), (string)currentFile["Name"]);

    //                GCode.File.AddBlock((string)currentFile["Name"], Action.New);

    //                new Thread(() =>
    //                {
    //                    res = WaitFor.AckResponse<string>(
    //                        cancellationToken,
    //                        response => AddBlock(response),
    //                        a => _viewModel.OnResponseReceived += a,
    //                        a => _viewModel.OnResponseReceived -= a,
    //                        400, () => Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_DUMP + (string)currentFile["Name"]));
    //                }).Start();

    //                while (res == null)
    //                    EventUtils.DoEvents();

    //                _viewModel.SuspendProcessing = false;

    //                GCode.File.AddBlock(string.Empty, Action.End);
    //            }

    //            _viewModel.Message = string.Empty;

    //            if (Rewind)
    //                Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_REWIND);

    //            FileSelected?.Invoke("SDCard:" + (string)currentFile["Name"], Rewind);
    //            Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_RUN + (string)currentFile["Name"]);

    //            Rewind = false;
    //        }
    //    }

    //    private void Upload_Click(object sender, RoutedEventArgs e)
    //    {
    //        bool ok = false;
    //        string filename = string.Empty;
    //        OpenFileDialog file = new OpenFileDialog();

    //        file.Filter = string.Format("GCode files ({0})|{0}|GCode macros (*.macro)|*.macro|Text files (*.txt)|*.txt|All files (*.*)|*.*", FileUtils.ExtensionsToFilter(GCode.FileTypes));

    //        if (file.ShowDialog() == true)
    //        {
    //            filename = file.FileName;
    //        }

    //        if (filename != string.Empty)
    //        {


    //            _viewModel.Message = (string)FindResource("Uploading");

    //            if (GrblInfo.UploadProtocol == "FTP")
    //            {
    //                if (GrblInfo.IpAddress == string.Empty)
    //                    _viewModel.Message = (string)FindResource("NoConnection");
    //                else using (new UIUtils.WaitCursor())
    //                    {
    //                        _viewModel.Message = (string)FindResource("Uploading");
    //                        try
    //                        {
    //                            using (WebClient client = new WebClient())
    //                            {
    //                                client.Credentials = new NetworkCredential("grblHAL", "grblHAL");
    //                                client.UploadFile(string.Format("ftp://{0}/{1}", GrblInfo.IpAddress, filename.Substring(filename.LastIndexOf('\\') + 1)), WebRequestMethods.Ftp.UploadFile, filename);
    //                                ok = true;
    //                            }
    //                        }
    //                        catch (WebException ex)
    //                        {
    //                            _viewModel.Message = ex.Message.ToString() + " " + ((FtpWebResponse)ex.Response).StatusDescription;
    //                        }
    //                        catch (System.Exception ex)
    //                        {
    //                            _viewModel.Message = ex.Message.ToString();
    //                        }
    //                    }
    //            }
    //            else
    //            {
    //                _viewModel.Message = (string)FindResource("Uploading");
    //                YModem ymodem = new YModem();
    //                ymodem.DataTransferred += Ymodem_DataTransferred;
    //                ok = ymodem.Upload(filename);
    //            }

    //            if (!(GrblInfo.UploadProtocol == "FTP" && !ok))
    //                _viewModel.Message = (string)FindResource(ok ? "TransferDone" : "TransferAborted");

    //            _grblSdCard.Load(_viewModel, ViewAll);
    //        }
    //    }

    //    private void Ymodem_DataTransferred(long size, long transferred)
    //    {
    //        _viewModel.Message = string.Format((string)FindResource("Transferring"), transferred, size);
    //    }

    //    private void Run_Click(object sender, RoutedEventArgs e)
    //    {
    //        RunFile();
    //    }
    //    private void ViewAll_Click(object sender, RoutedEventArgs e)
    //    {
    //        _grblSdCard.Load(_viewModel, ViewAll);
    //    }

    //    private void Delete_Click(object sender, RoutedEventArgs e)
    //    {
    //        if (dgrSDCard.SelectedItem == null) return;
    //        var selectedFile = (string)currentFile["Name"];
    //        if (string.IsNullOrEmpty(selectedFile)) return;
    //        if (MessageBox.Show(string.Format((string)FindResource("DeleteFile"), (string)currentFile["Name"]), "ioSender", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
    //        {
    //            Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_UNLINK + (string)currentFile["Name"]);
    //            _grblSdCard.Load(_viewModel, ViewAll);
    //        }
    //    }

    //    private void RunFile()
    //    {
    //        if (currentFile != null)
    //        {
    //            if ((bool)currentFile["Invalid"])
    //            {
    //                MessageBox.Show(string.Format(((string)FindResource("IllegalName")).Replace("\\n", "\r\r"), (string)currentFile["Name"]), "ioSender",
    //                                 MessageBoxButton.OK, MessageBoxImage.Error);
    //            }
    //            else
    //            {
    //                if (Rewind)
    //                {
    //                    Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_REWIND);
    //                }
    //                FileSelected?.Invoke("SDCard:" + (string)currentFile["Name"], Rewind);
    //                Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_RUN + (string)currentFile["Name"]);
    //                Rewind = false;
    //            }
    //        }
    //    }
    //}

    //public class GrblSDCard
    //{
    //    private DataTable dataTable;
    //    private bool? mounted = null;
    //    private int id = 0;

    //    public GrblSDCard()
    //    {
    //        dataTable = new DataTable("Filelist");

    //        dataTable.Columns.Add("Id", typeof(int));
    //        dataTable.Columns.Add("Dir", typeof(string));
    //        dataTable.Columns.Add("Name", typeof(string));
    //        dataTable.Columns.Add("Size", typeof(int));
    //        dataTable.Columns.Add("Invalid", typeof(bool));
    //        dataTable.PrimaryKey = new DataColumn[] { dataTable.Columns["Id"] };
    //    }

    //    public DataView Files { get { return dataTable.DefaultView; } }
    //    public bool Loaded { get { return dataTable.Rows.Count > 0; } }

    //    public void Load(GrblViewModel model, bool viewAll)
    //    {
    //        bool? res = null;
    //        CancellationToken cancellationToken = new CancellationToken();

    //        dataTable.Clear();
    //        //SendSettings(model, GrblConstants.CMD_SDCARD_MOUNT, "ok");
    //        if (mounted == null)
    //        {
    //            Comms.com.PurgeQueue();

    //            new Thread(() =>
    //            {
    //                mounted = WaitFor.AckResponse<string>(
    //                    cancellationToken,
    //                    null,
    //                    a => model.OnResponseReceived += a,
    //                    a => model.OnResponseReceived -= a,
    //                    500, () => Comms.com.WriteCommand(GrblConstants.CMD_SDCARD_MOUNT));
    //            }).Start();

    //            while (mounted == null)
    //                EventUtils.DoEvents();
    //        }

    //        if (mounted == true)
    //        {
    //            Comms.com.PurgeQueue();

    //            id = 0;
    //            model.Silent = true;

    //            new Thread(() =>
    //            {
    //                res = WaitFor.AckResponse<string>(
    //                    cancellationToken,
    //                    response => Process(response),
    //                    a => model.OnResponseReceived += a,
    //                    a => model.OnResponseReceived -= a,
    //                    2000, () => Comms.com.WriteCommand(viewAll ? GrblConstants.CMD_SDCARD_DIR_ALL : GrblConstants.CMD_SDCARD_DIR));
    //            }).Start();

    //            while (res == null)
    //                EventUtils.DoEvents();

    //            model.Silent = false;

    //            dataTable.AcceptChanges();
    //        }
    //    }
    //    private void SendSettings(GrblViewModel model, string command, string key)
    //    {
    //        try
    //        {

    //            bool res = false;
    //            var cancellationToken = new CancellationToken();
    //            model.Poller.SetState(0);

    //            void ProcessSettings(string response)
    //            {
    //                if (response.StartsWith(key))
    //                {   
    //                    Process(response);
    //                    res = true;
    //                }
    //            }
    //            void Send()
    //            {
    //                Comms.com.DataReceived -= ProcessSettings;
    //                Comms.com.DataReceived += ProcessSettings;
    //                Comms.com.WriteCommand(command);
    //                while (!res)
    //                {
    //                    Thread.Sleep(50);
    //                }
    //                Comms.com.DataReceived -= ProcessSettings;
    //                model.Poller.SetState(model.PollingInterval);
    //            }
                
    //            Task.Factory.StartNew(Send, cancellationToken);

    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex);
    //            model.Poller.SetState(200);
    //        }
    //    }
    //    private void Process(string data)
    //    {
    //        if (!Application.Current.Dispatcher.CheckAccess())
    //        {
    //            Application.Current.Dispatcher.Invoke(() => Process(data));
    //            return;
    //        }


    //        string filename = "";
    //        int filesize = 0;
    //        bool invalid = false;

    //        if (data.StartsWith("[FILE:"))
    //        {
    //            string[] parameters = data.TrimEnd(']').Split('|');
    //            foreach (string parameter in parameters)
    //            {
    //                string[] valuepair = parameter.Split(':');
    //                switch (valuepair[0])
    //                {
    //                    case "[FILE":
    //                        filename = valuepair[1];
    //                        break;

    //                    case "SIZE":
    //                        filesize = int.Parse(valuepair[1]);
    //                        break;

    //                    case "INVALID":
    //                        invalid = true;
    //                        break;
    //                }
    //            }

    //            dataTable.Rows.Add(new object[] { id++, "", filename, filesize, invalid });
    //        }
    //    }
    }
}
