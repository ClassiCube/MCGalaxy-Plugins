using System;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.SQL;

public sealed class CmdRemoveTable : Command
{
    public override string name { get { return "RemoveTable"; } }
    public override string type { get { return CommandTypes.Information; } }
    public override LevelPermission defaultRank { get { return LevelPermission.Nobody; } }

    public override void Use(Player p, string message)
    {
        string[] args = message.SplitSpaces();

        if (message.Length == 0)
        {
            Help(p);
            return;
        }

        if (!Database.TableExists(args[0]))
        {
            p.Message("%STable %b" + args[0] + " %Sdoes not exist.");
            return;
        }

        Database.DeleteTable(args[0]);
        p.Message("%aTable %b" + args[0] + " %adeleted successfully.");
    }

    public override void Help(Player p)
    {
        p.Message("&T/RemoveTable [table]");
        p.Message("&HDeletes the [table] from the database.");
        p.Message("&cWarning: &HOnly use this if you know what you are doing.");
    }
}
