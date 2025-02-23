using System;
using System.Windows;
using System.Windows.Controls;
using ioSenderTouch.Views;

namespace ioSenderTouch.Controls
{
    /// <summary>
    /// Interaction logic for CalibrationControl.xaml
    /// </summary>
    public partial class CalibrationControl : UserControl
    {
        private VirtualKeyBoard _keyBoard;
        public CalibrationControl()
        {
            InitializeComponent();
            IsVisibleChanged += Control_IsVisibleChanged;
            this.Loaded += Control_Loaded;
            this.LostFocus += Control_LostFocus;
        }

        
        private void Control_LostFocus(object sender, RoutedEventArgs e)
        {
            _keyBoard.Close();
        }

        private void Control_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool b)
            {
                if (!b)
                {
                    _keyBoard.Close();
                }

            }
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
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
