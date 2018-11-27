using System;
using MCGalaxy.Commands;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy {
	public class CmdMaphack : Command2 {
		public override string name { get { return "MapHack"; } }
		public override string type { get { return "other"; } }
		 public override CommandPerm[] ExtraPerms {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can bypass hack restrictions on all maps") }; }
        }
		
		static bool hooked;
		const string ext_allowed_key = "__MAPHACK_ALLOWED";
		
		public override void Use(Player p, string message, CommandData data) {
			if (!hooked) { // not thread-safe but meh
				OnSentMapEvent.Register(HandleOnSentMap, Priority.High);
				OnSendingMotdEvent.Register(HandleSendingMotd, Priority.High);
				hooked = true;
			}

			if (CheckExtraPerm(p, data, 1) || LevelInfo.IsRealmOwner(p.name, p.level.MapName)) {
				p.Extras.PutBoolean(ext_allowed_key, true);
				p.SendMapMotd();
				Player.Message(p, "&aYou are now bypassing hacks restrictions on this map");
			} else {
				Player.SendMessage(p, "&cYou can only bypass hacks on your own realms.");
			}
		}
		
		void HandleOnSentMap(Player p, Level prevLevel, Level level) {
			if (!p.Extras.GetBoolean(ext_allowed_key)) return;
			// disable /maphack when you reload or change maps
			p.Extras.PutBoolean(ext_allowed_key, false);
			p.SendMapMotd();
			Player.Message(p, "%HHacks bypassing reset, use %T/MapHack %Hto turn on again");
		}
		
		void HandleSendingMotd(Player p, ref string motd) {
			if (!p.Extras.GetBoolean(ext_allowed_key)) return;
			motd = "+hax";
		}
		
		public override void Help(Player p) {
			Player.Message(p, "%T/MapHack %H");
			Player.Message(p, "%HLets you bypass hacks restrictions on your own map");
			Player.Message(p, "%H  (e.g. for when making a parkour map with -hax on)");
		}
	}
}
