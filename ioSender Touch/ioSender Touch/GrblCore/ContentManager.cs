using System.Collections.Generic;

namespace ioSenderTouch.GrblCore;

public class ContentManager
{
    private readonly Dictionary<string, IActiveViewModel> _uiElementCollection = [];


    public void RegisterViewAndModel(string viewName, IActiveViewModel name)
    {
        if (!_uiElementCollection.ContainsKey(name.Name))
        {
            _uiElementCollection.Add(name.Name, name);
        }
    }
    public bool SetActiveUiElement(string name)
    {
        if (_uiElementCollection.ContainsKey(name))
        {
            foreach (var element in _uiElementCollection)
            {
                if (element.Key == name)
                {
                    element.Value.Active = true;
                    element.Value.Activated();
                    
                }
                else
                {
                    if (!element.Value.Active || element.Key == name) continue;
                    element.Value.Active = false;
                    element.Value.Deactivated();
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