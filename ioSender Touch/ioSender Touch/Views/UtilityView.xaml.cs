using System.Windows.Controls;
using ioSenderTouch.GrblCore;
using ioSenderTouch.ViewModels;

namespace ioSenderTouch.Views
{
    /// <summary>
    /// Interaction logic for Utility.xaml
    /// </summary>
    public partial class UtilityView : UserControl
    {
        private UtilityViewModel _model;
        public UtilityView(GrblViewModel grblViewModel)
        {
            _model = new UtilityViewModel(grblViewModel);
            DataContext = _model;
            
            InitializeComponent();
        }
    }
}
