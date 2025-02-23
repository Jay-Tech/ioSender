using System.Windows;
using System.Windows.Controls;
using CNC.Core;

namespace ioSenderTouch.Controls
{
    /// <summary>
    /// Interaction logic for GCodeListControl.xaml
    /// </summary>
    public partial class GCodeListControl : UserControl
    {
        public ScrollViewer scroll = null;

        public static readonly DependencyProperty ShowScrollBarProperty = DependencyProperty.Register(nameof(ShowScrollBar), typeof(bool), typeof(SDCardView), new PropertyMetadata(false));
        public bool ShowScrollBar
        {
            get { return (bool)GetValue(ShowScrollBarProperty); }
            set { SetValue(ShowScrollBarProperty, value); }
        }
        public GCodeListControl()
        {
            InitializeComponent();
        }
        private void grdGCode_Drag(object sender, DragEventArgs e)
        {
            GCode.File.Drag(sender, e);
        }

        private void grdGCode_Drop(object sender, DragEventArgs e)
        {
            GCode.File.Drop(sender, e);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            scroll = UIUtils.GetScrollViewer(grdGCode);
            grdGCode.DataContext = GCode.File.Data.DefaultView;
            if (DataContext is GrblViewModel)
                (DataContext as GrblViewModel).PropertyChanged += GCodeListControl_PropertyChanged;
        }

        private void GCodeListControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is GrblViewModel) switch (e.PropertyName)
            {
                case nameof(GrblViewModel.ScrollPosition):
                    int sp = ((GrblViewModel)sender).ScrollPosition;
                    grdGCode.SelectedIndex = sp;
                    grdGCode.UpdateLayout();
                    grdGCode.ScrollIntoView(grdGCode.SelectedItem);
                        break;
                    //if (sp == 0)
                    //    scroll.ScrollToTop();
                    //else
                    //    scroll.ScrollToVerticalOffset(sp);
                    //break;
            }
        }
    }
}
