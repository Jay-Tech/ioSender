using System;
using System.Windows;
using System.Windows.Controls;
using ioSenderTouch.ViewModels;

namespace ioSenderTouch.Views
{
    /// <summary>
    /// Interaction logic for VirtualKeyBoard.xaml
    /// </summary>
    public partial class VirtualKeyBoard : Window, IDisposable
    {
        public event EventHandler<string> TextChanged;
        public event EventHandler VBClosing;
        private  UIElement _uiElement;
        private readonly VirtualKeyboardViewModel _viewModel;
        private bool _exitApp;

        public string Text => _viewModel.TextFromKeyBoard;

        public VirtualKeyBoard() 
        {
           
            InitializeComponent();
            _viewModel = new VirtualKeyboardViewModel();
            _viewModel.PropertyChanged += _viewModel_PropertyChanged;
            DataContext = _viewModel;
            this.Closing += VirtualKeyBoard_Closing;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        }

        public void Shutdown(bool shutdown)
        {
            _exitApp = shutdown;
           this.Close();
        }
        private void VirtualKeyBoard_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_exitApp)
            {
                e.Cancel = true;
                VBClosing?.Invoke(this, null);
                 _viewModel.TextFromKeyBoard = string.Empty;
                 this.Hide();
            }
            
        }

        private void _viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TextChanged?.Invoke(this,Text);
        }

        public VirtualKeyBoard(UIElement uiElement)
        {
            _uiElement = uiElement;
            _viewModel = new VirtualKeyboardViewModel();
            InitializeComponent();
            DataContext = _viewModel;
            switch (uiElement)
            {
                case TextBox tb:
                    tb.Text = _viewModel.TextFromKeyBoard;
                    break;
                case ComboBox cB:
                    cB.Text = _viewModel.TextFromKeyBoard;
                    break;
            }
        }

        public void Dispose()
        {
            Shutdown(true);
        }
    }
}
