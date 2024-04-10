using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace CNC.Controls.Probing
{
    public class ProbingMacroCommands
    {
        public List<ProbingMacroCommand> ProbingMacroCommandsCollection { get;  set; } = new List<ProbingMacroCommand>();
        public void Save()
        {
            XmlSerializer xs = new XmlSerializer(typeof(List<ProbingMacroCommand>));
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
            XmlSerializer xs = new XmlSerializer(typeof(List<ProbingMacroCommand>));

            try
            {
                StreamReader reader = new StreamReader(Core.Resources.Path + "ProbingMacroCommand.xml");
                ProbingMacroCommandsCollection = (List<ProbingMacroCommand>)xs.Deserialize(reader);
                reader.Close();
            }
            catch
            {
            }
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