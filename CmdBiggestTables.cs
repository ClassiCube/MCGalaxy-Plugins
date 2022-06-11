using System;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.SQL;

public sealed class CmdBiggestTables : Command 
{
	public override string name { get { return "BiggestTables"; } }
	public override string type { get { return CommandTypes.Information; } }
	public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
	
	struct TableEntry { public string Name; public int Count; }
	
	public override void Use(Player p, string message) {
		List<string> tables = Database.Backend.AllTables();
		TableEntry[] entries = new TableEntry[tables.Count];
		
		for (int i = 0; i < tables.Count; i++) {
			try {
				string maxID = Database.ReadString(tables[i], "MAX(_ROWID_)", "LIMIT 1");
				if (maxID == null || maxID == "") continue;
				
				int count;
				if (!int.TryParse(maxID, out count) || count == 0) continue;
				
				entries[i] = new TableEntry() { Name = tables[i], Count = count };
			} catch {
			}
		}
		Array.Sort<TableEntry>(entries, (a, b) => b.Count.CompareTo(a.Count));
		
		for (int i = 0; i < Math.Min(20, entries.Length); i++) {
			p.Message("Table {0} has {1} entries", entries[i].Name, entries[i].Count);
		}
	}
	
	public override void Help(Player p) { }
}
