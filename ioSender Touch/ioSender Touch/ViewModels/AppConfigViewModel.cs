using System.Windows.Input;
using ioSenderTouch.GrblCore;
using ioSenderTouch.GrblCore.Comands;
using ioSenderTouch.GrblCore.Config;


namespace ioSenderTouch.ViewModels;

public class AppConfigViewModel : ViewModelBase, IActiveViewModel
{
    public bool Active { get; set; }
    public string Name { get; }

    public Config ConfigBase => AppConfig.Settings.Base;

    public ICommand SaveCommand { get; }

    public ICommand SaveKeyMapCommand { get; }
    public AppConfigViewModel()
    {
        Name = nameof(AppConfigViewModel);
        SaveCommand = new Command(Save);
        SaveKeyMapCommand = new Command(SaveKeyMap);
    }

    private void SaveKeyMap(object obj)
    {
        if (AppConfig.Settings.Save())
            Grbl.GrblViewModel.Message = "SettingsSaved";
    }

    private void Save(object obj)
    {
        string filename = Resources.Path + $"KeyMap{(int)AppConfig.Settings.JogMetric.Mode}.xml";
        if (Grbl.GrblViewModel.Keyboard.SaveMappings(filename))
            Grbl.GrblViewModel.Message = $"Keymappings saved to {filename}";
    }

    public void Activated(){}
    
    public void Deactivated(){}
    
}