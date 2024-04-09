using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CNC.Controls.Probing
{
    public class ProbingMacroCommands
    {
        public ObservableCollection<ProbingMacroCommand> ProbingMacroCommandsCollection { get;  set; } = new ObservableCollection<ProbingMacroCommand>();
        public void Save()
        {
            XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<ProbingMacroCommand>));
            try
            {
                FileStream fsout = new FileStream(Core.Resources.Path + "ProbingMacroCommand.xml", FileMode.Create, FileAccess.Write, FileShare.None);
                using (fsout)
                {
                    xs.Serialize(fsout, ProbingMacroCommandsCollection);
                }
            }
            catch
            {
            }
        }

        public void Load()
        {
            XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<ProbingMacroCommand>));

            try
            {
                StreamReader reader = new StreamReader(Core.Resources.Path + "ProbingMacroCommand.xml");
                ProbingMacroCommandsCollection = (ObservableCollection<ProbingMacroCommand>)xs.Deserialize(reader);
                reader.Close();
            }
            catch
            {
            }
        }
        public bool Delete(string name)
        {
            bool deleted = false;
            var command = ProbingMacroCommandsCollection.FirstOrDefault(x => x.CommandName.Equals(name));

            if (command != null)
                return deleted = ProbingMacroCommandsCollection.Remove(command);
            return false;
        }


    }
}
[Serializable]
public class ProbingMacroCommand
{
    public ProbingMacroCommand()
    {

    }
    public ProbingMacroCommand(string name, string preCommand, string postCommand, bool isChecked)
    {
        CommandName = name;
        PreCommand = preCommand;
        PostCommand = postCommand;
        IsSingleUse = isChecked;
    }
    public string CommandName { get; set; }

    public string PreCommand { get; set; }

    public string PostCommand { get; set; }
    public bool IsSingleUse { get; set; }
}
