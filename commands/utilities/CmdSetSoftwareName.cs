using System;
using MCGalaxy;

public sealed class CmdSetSoftwareName : Command 
{
    public override string name { get { return "SetSoftwareName"; } }
    public override string type { get { return "other"; } }
    public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }

    public override void Use(Player p, string message) {
    	Server.SoftwareName = Colors.Escape(message);
    }

    public override void Help(Player p) {
    	p.Message("&T/SetSoftwareName [software name]");
    	p.Message("&HSets software name to the given name.");
    }
}
