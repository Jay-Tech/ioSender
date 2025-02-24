using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ioSenderTouch.ViewModels;
using ioSenderTouch.Views;

namespace ioSenderTouch.Controls
{
    /// <summary>
    /// Interaction logic for FacingView.xaml
    /// </summary>
    public partial class SurfacingControl : UserControl
    {
        private VirtualKeyBoard _keyBoard;
        public SurfacingControl()
        {
            InitializeComponent();
            IsVisibleChanged += SurfacingControl_IsVisibleChanged;
            this.Loaded += SurfacingControl_Loaded;
            this.LostFocus += SurfacingControl_LostFocus1;
            //TODO temp until passes is support 
            PassesTextBox.IsReadOnly = true;
            PassesTextBox.IsEnabled = false;
        }

        private void SurfacingControl_LostFocus1(object sender, RoutedEventArgs e)
        {
            _keyBoard.Close();
        }

        private void SurfacingControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool b )
            {
                if (!b)
                {
                    _keyBoard.Close();
                }

            }
        }

        private void SurfacingControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_keyBoard == null)
            {
                _keyBoard = new VirtualKeyBoard
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = 750,
                    Top = 400,
                    Topmost = true
                };

            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox textBox)) return;

            void Close(object senders, EventArgs es)
            {
                _keyBoard.VBClosing -= Close;
                _keyBoard.TextChanged -= TextChanged;
            }

            void TextChanged(object senders, string t)
            {
                textBox.Text = t;
            }
            if (_keyBoard.Visibility == Visibility.Visible) return;
            _keyBoard.Show();
            _keyBoard.TextChanged -= TextChanged;
            _keyBoard.TextChanged += TextChanged;
            _keyBoard.VBClosing -= Close;
            _keyBoard.VBClosing += Close;

        }

    }
}
