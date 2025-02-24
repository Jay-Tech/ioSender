using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ioSenderTouch.GrblCore;
using ioSenderTouch.ViewModels;


namespace ioSenderTouch.Controls
{
    /// <summary>
    /// Interaction logic for UtilityMacroControl.xaml
    /// </summary>
    public partial class UtilityMacroControl : UserControl
    {
        private GrblViewModel _model;
       
        public UtilityMacroControl()
        {
            InitializeComponent();
            Loaded += UtilityMacroControl_Loaded;
        }
        public ObservableCollection<Macro> Macros
        {
            get => (ObservableCollection<Macro>)GetValue(MacrosProp);
            set => SetValue(MacrosProp, value);
        }
        public static readonly DependencyProperty MacrosProp = DependencyProperty.Register(nameof(UtilityMacroControl.Macros), typeof(ObservableCollection<Macro>), typeof(UtilityMacroControl));
        private void UtilityMacroControl_Loaded(object sender, RoutedEventArgs e)
        {
            _model = DataContext as GrblViewModel;
            if (_model != null)
            {
                Macros = _model.UtilityMacros;
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var macro = Macros.FirstOrDefault(o =>
            {
                var tag = (sender as Button)?.Tag;
                return tag != null && o.Name == (string)tag;
            });
            if(macro == null)return;
            if (macro.isJob)
            {
                if(!macro.ConfirmOnExecute || MessageBox.Show($"Load Job {macro.Name}", "ioSender",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    GCode.File.Load(macro.Path);
                     
              
            }
            else
            {
                if (!macro.ConfirmOnExecute || MessageBox.Show($"Run Macro {macro.Name}", "ioSender",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    _model?.ExecuteMacro(macro.Code);
            }
           
           
            

        }
    }
}
