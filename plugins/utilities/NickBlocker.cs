using System;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events;

namespace Core {
	public class NickBlocker : Plugin {
		public override string creator { get { return "Venk"; } }
		public override string name { get { return "NickBlocker"; } }
		public override string MCGalaxy_Version { get { return "1.9.2.2"; } }
		public override void Load(bool startup) {
			OnPlayerCommandEvent.Register(HandlePlayerCommand, Priority.High);
		}

		public override void Unload(bool shutdown) {
			OnPlayerCommandEvent.Unregister(HandlePlayerCommand);
		}

        void HandlePlayerCommand(Player p, string cmd, string args, CommandData data) {
            cmd = cmd.ToLower();
            if (!(cmd == "whonick" || cmd == "realname")) return;
            
            // Get MOTD of map
			LevelConfig cfg = p.level.Config;

        	if (cfg.MOTD.ToLower().Contains("-nicks")) {
                p.Message("You cannot use that command in this map.");
                p.cancelcommand = true;
                return;
            }
		}
	}
}
