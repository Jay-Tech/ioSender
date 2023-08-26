﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CNC.Controls.Views;

namespace CNC.Controls
{
    /// <summary>
    /// Interaction logic for ConfigTextBox.xaml
    /// </summary>
    public partial class ConfigTextBox: TextBox
    {
        private VirtualKeyBoard _keyBoard;
        public ConfigTextBox()
        {
            InitializeComponent();
            Loaded += ConfigTextBox_Loaded;
            LostFocus += ConfigTextBox_LostFocus;
            IsVisibleChanged += ConfigTextBox_IsVisibleChanged;

        }

        private void ConfigTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool b)
            {
                if (!b)
                {
                    _keyBoard.Close();
                }

            }
        }

        private void ConfigTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _keyBoard.Close();
        }

        private void ConfigTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_keyBoard == null)
            {
                _keyBoard = new VirtualKeyBoard
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = 875,
                    Top = 500,
                    Topmost = true
                };
            }
        }
        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender is TextBox uiElement))
            {
                void Close(object senders, EventArgs es)
                {
                    _keyBoard.VBClosing -= Close;
                    _keyBoard.TextChanged -= TextChanged;
                }

                void TextChanged(object senders, string t)
                {
                    
                        uiElement.Text = t;
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
}
