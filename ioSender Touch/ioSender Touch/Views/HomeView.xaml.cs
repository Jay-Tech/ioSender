using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ioSenderTouch.GrblCore;
using ioSenderTouch.ViewModels;


namespace ioSenderTouch.Views
{

    public partial class HomeView : UserControl
    {
        private readonly GrblViewModel _model;
        
        public UIElement Content { get; set; }
        public HomeView(GrblViewModel model)
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

    }
}
