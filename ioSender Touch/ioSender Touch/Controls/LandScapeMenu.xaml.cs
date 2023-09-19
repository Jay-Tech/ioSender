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
using CNC.Core;

namespace ioSenderTouch.Controls
{
    /// <summary>
    /// Interaction logic for LandScapeMenu.xaml
    /// </summary>
    public partial class LandScapeMenu : UserControl
    {
        public LandScapeMenu()
        {
            InitializeComponent();
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GrblViewModel model)
            {
                model.SetShutDown(this);
            }
            else
            {
                if (Application.Current.MainWindow != null)
                    Application.Current.MainWindow.Close();
            }
            
        }

        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow != null)
            {
                AdjustWindowSize();
            }
        }
        private void AdjustWindowSize()
        {
            if (App.Current.MainWindow.WindowState == WindowState.Maximized)
            {
                App.Current.MainWindow.WindowState = WindowState.Normal;
                MaximizeButton.Content = "";
            }
            else if (App.Current.MainWindow.WindowState == WindowState.Normal)
            {
                App.Current.MainWindow.WindowState = WindowState.Maximized;
                MaximizeButton.Content = "";
            }
        }
    }
}
