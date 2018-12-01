using System;
using MCGalaxy;

namespace MyCommandThingy {
    public sealed class CmdSetSoftwareName : Command {
        public override string name { get { return "SetSoftwareName"; } }
        public override string type { get { return "other"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Nobody; } }

        public override void Use(Player p, string message) {
        	Server.SoftwareName = Colors.Escape(message);
        }

        public override void Help(Player p) {
        	Player.Message(p, "%T/SetSoftwareName [software name]");
        	Player.Message(p, "%HSets software name to the given name.");
        }
    }
}
