using System;
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
using System.Windows.Shapes;

namespace CNC.Controls.Probing
{
    /// <summary>
    /// Interaction logic for ProbingDialog.xaml
    /// </summary>
    public partial class ProbingDialog : Window
    {
        public ProbingDialog(ProbingViewModel probingViewModel)
        {
            DataContext = probingViewModel;
            InitializeComponent();
            
        }

        private void AddButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
