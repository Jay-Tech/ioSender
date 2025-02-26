using System.Collections.Generic;
using System.Linq;

namespace ioSenderTouch.GrblCore;

public class ContentManager
{
    private readonly Dictionary<string, IActiveViewModel> _activeViewModelCollection = [];


    public void RegisterViewAndModel(string viewName, IActiveViewModel vm)
    {
        if (!_activeViewModelCollection.ContainsKey(viewName))
        {
            _activeViewModelCollection.Add(viewName, vm);
        }
    }
    public bool SetActiveUiElement(string name)
    {
        if (_activeViewModelCollection.ContainsKey(name))
        {
            if (_activeViewModelCollection.Any(item => item.Key.Equals(name) && item.Value.Active))
            {
                return true;
            }
            foreach (var viewModel in _activeViewModelCollection)
            {
                if (viewModel.Key == name)
                {
                    viewModel.Value.Active = true;
                    viewModel.Value.Activated();
                    
                }
                else
                {
                    if (!viewModel.Value.Active || viewModel.Key == name) continue;
                    viewModel.Value.Active = false;
                    viewModel.Value.Deactivated();
                }
            }
            return true;
        }

        return false;
    }
}

public interface IActiveViewModel
{
    public bool Active { get; set; }
    public string Name { get; }
    public void Activated();
    public void Deactivated();
}