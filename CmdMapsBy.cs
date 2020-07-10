using System;
using System.Collections.Generic;

namespace MCGalaxy.Commands {

	public sealed class CmdMapsBy : Command2 {
		public override string name { get { return "MapsBy"; } }
		public override string shortcut { get { return "MadeBy"; } }
		public override string type { get { return CommandTypes.Information; } }

		public override void Use(Player p, string message, CommandData data) {
			if (message == "") { Help(p); return; }
			string author = PlayerInfo.FindMatchesPreferOnline(p, message);
			if (author == null) return;

			string[] maps = LevelInfo.AllMapNames();
			List<string> madeBy = new List<string>();
			foreach (string map in maps) {
				if (!LevelInfo.IsRealmOwner(author, map)) continue;
				madeBy.Add(map);
			}

			author = PlayerInfo.GetColoredName(p, author);
			if (madeBy.Count == 0) {
				p.Message("{0} %Shas not made any maps", author);
			} else {
				p.Message("{0} %Sauthored these maps: {1}", author, madeBy.Join());
			}
		}

		public override void Help(Player p) {
			p.Message("%T/mapsby %H[player]");
			p.Message("%HLists all maps authored by the given player.");
		}
	}
}

