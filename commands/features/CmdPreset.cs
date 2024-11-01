// Checks if player has build perm as well as realm ownership
// Can be used in message blocks and bots

using System;
using System.Collections.Generic;
using MCGalaxy.Commands;

namespace MCGalaxy.Commands.Eco {
    public sealed class CmdPreset : Command2 {
        public override string name { get { return "Preset"; } }
        public override string type { get { return CommandTypes.Other; } }

        public override void Use(Player p, string message, CommandData data) {
            string[] args = message.SplitSpaces();
            if (args.Length < 1) { Help(p); return; }
            
            bool canBuild = p.level.BuildAccess.CheckAllowed(p);
            bool isOwner = LevelInfo.IsRealmOwner(p.name, p.level.name);
            if (!canBuild && !isOwner) {
            	p.Message("You need permission from the map owner to use this command.");
            	return;
            }
            
            Command.Find("Environment").Use(p, "preset " + args[0]);
            
        }

        public override void Help(Player p) {
            p.Message("&T/Preset [preset] &H- Adds an env preset to your map.");
            p.Message("&eUsable in message blocks.");
        }
    }
}
