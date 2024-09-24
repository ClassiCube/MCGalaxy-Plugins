// NOTE: If you have other plugins that add commands, you should probably rename this file to something along the lines of 'zzDisableCommands' so those are disabled as well.

using System;
using System.Collections.Generic;

namespace MCGalaxy
{
    public class DisableCommands : Plugin
    {
        public override string name { get { return "DisableCommands"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string creator { get { return "Venk"; } }

        public override void Load(bool startup)
        {
            List<string> commandsToKeep = new List<string> { "help", "commands", "compile", "plugin", "restart", }; // Add all commands you wish to keep
            List<Command> commandsToRemove = new List<Command>();

            foreach (Command cmd in Command.allCmds)
            {
                if (commandsToKeep.Contains(cmd.name.ToLower())) continue;
                commandsToRemove.Add(cmd);
            }

            foreach (Command cmd in commandsToRemove) Command.Unregister(cmd);
        }

        public override void Unload(bool shutdown)
        {
        }

        public override void Help(Player p)
        {
        }
    }
}
