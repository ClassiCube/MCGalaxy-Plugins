using System;
using System.Collections.Generic;

using MCGalaxy.DB;
using MCGalaxy.SQL;

namespace MCGalaxy {
	public class FavouriteMap : Plugin {
		public override string name { get { return "FavouriteMap"; } }
		public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
		public override string creator { get { return "Venk"; } }
		
		static OnlineStatPrinter onlineLine;
		static OfflineStatPrinter offlineLine;

		public override void Load(bool startup) {
            Command.Register(new CmdFavouriteMap());
            onlineLine  = (p, who) => DisplayFavouriteMap(p, who.name);
			offlineLine = (p, who) => DisplayFavouriteMap(p, who.Name);
			OnlineStat.Stats.Add(onlineLine);
			OfflineStat.Stats.Add(offlineLine);
			InitDB();
        }
		
        public override void Unload(bool shutdown) {
            Command.Unregister(Command.Find("FavouriteMap"));
            OnlineStat.Stats.Remove(onlineLine);
			OfflineStat.Stats.Remove(offlineLine);
        }
		
		ColumnDesc[] CreateDatabase = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16), 
            new ColumnDesc("Map", ColumnType.VarChar, 32),
        };

        void InitDB() {
            Database.CreateTable("FavouriteMaps", CreateDatabase);
        }
		
		static void DisplayFavouriteMap(Player p, string who) {
			List<string[]> mRows = Database.GetRows("FavouriteMaps", "Name, Map", "WHERE Name=@0", who);
			if (mRows.Count == 0) return;
			string FavouriteMap = mRows[0][1];
			//string gold = Awards.HasAllAwards(who) ? "&6" : "&S";
			p.Message("  Favourite map is &b{0}", FavouriteMap);
		}
                        
		public override void Help(Player p) {}
	}
	
	public sealed class CmdFavouriteMap : Command2 {
        public override string name { get { return "FavouriteMap"; } }
        public override bool SuperUseable { get { return false; } }
        public override string type { get { return "fun"; } }
        public override string shortcut { get { return "fm"; } }

        public override void Use(Player p, string message, CommandData data) {
        	string[] args = message.SplitSpaces(1);
        	if (args.Length == 0) { Help(p); }
        	else {
        		string map = Matcher.FindMaps(p, args[0]);
            	if (map == null) return;
        		List<string[]> mRows = Database.GetRows("FavouriteMaps", "Name, Map", "WHERE Name=@0", p.truename);
	                    
	            if (mRows.Count == 0) {
	            	Database.AddRow("FavouriteMaps", "Name, Map", p.truename, map);
		            p.Message("&SYou set your favourite map to &b" + map + "&S.");
	                return;
	            }
	                    
	            else {
        			Database.UpdateRows("FavouriteMaps", "Map=@1", "WHERE NAME=@0", p.truename, map);
        			p.Message("&SYou changed your favourite map to &b" + map + "&S.");
        			return;
        		}
        	}
        }

        public override void Help(Player p) {
        	p.Message("&T/FavouriteMap [map] &H- Sets your favourite map to [map].");
        }
    }
}
