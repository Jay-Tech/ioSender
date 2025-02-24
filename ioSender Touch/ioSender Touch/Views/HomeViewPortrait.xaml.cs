using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ioSenderTouch.Controls;
using ioSenderTouch.Controls.Probing;
using ioSenderTouch.Controls.Render;
using ioSenderTouch.GrblCore;
using ioSenderTouch.Utility;
using ioSenderTouch.ViewModels;

namespace ioSenderTouch.Views
{
    public partial class HomeViewPortrait : UserControl
    {
        private bool? initOK = null;
        private bool isBooted = false;
        private IInputElement focusedControl = null;
        private Controller _controller = null;
        private readonly GrblViewModel _model;
        private readonly GrblConfigView _grblSettingView;
        private readonly AppConfigView _grblAppSettings;
        private ProbingView _probeView;
        private readonly RenderControl _renderView;
        private readonly OffsetView _offsetView;
        private SDCardView _sdView;
        private ToolView _toolView;
        private readonly UtilityView _utilityView;

        public HomeViewPortrait(GrblViewModel model)
        {
            _model = model;
            DataContext = _model;
            InitializeComponent();
            Grbl.GrblViewModel = _model;
            _model.HomeViewModel = new HomeViewModel(_model);

        }
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!(e.Handled = ProcessKeyPreview(e)))
            {
                if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                    Focus();
                base.OnPreviewKeyDown(e);
            }
        }
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (!(e.Handled = ProcessKeyPreview(e)))
                base.OnPreviewKeyDown(e);
        }
        protected bool ProcessKeyPreview(KeyEventArgs e)
        {
            return _model.Keyboard.ProcessKeypress(e, !(MdiControl.IsFocused || DRO.IsFocused || spindleControl.IsFocused || workParametersControl.IsFocused));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            LeftBorder.Visibility = Visibility.Visible;
            RightMenuBorder.Visibility = Visibility.Visible;
            FillBorder.Child = _renderView;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            LeftBorder.Visibility = Visibility.Collapsed;
            RightMenuBorder.Visibility = Visibility.Collapsed;
            FillBorder.Child = _grblAppSettings;
        }

    }
}
