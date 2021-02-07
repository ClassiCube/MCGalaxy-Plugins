using System;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;

namespace PluginNoTP {
	public sealed class Core : Plugin {
		public override string creator { get { return "Not UnknownShadow200"; } }
		public override string name { get { return "NoTp"; } }
		public override string MCGalaxy_Version { get { return "1.9.1.4"; } }
		
		public override void Load(bool startup) {
			OnPlayerCommandEvent.Register(OnPlayerCommand, Priority.Low);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerCommandEvent.Unregister(OnPlayerCommand);
		}

		static bool OnGameMap(string map) {
			return map.CaselessStarts("zs");
		}

		void OnPlayerCommand(Player p, string cmd, string args, CommandData data) {
			cmd = cmd.ToLower();
			if (!(cmd == "tp" || cmd == "teleport" || cmd == "tpa" || cmd == "spawn")) return;
			
			if (OnGameMap(p.level.name)) {
				p.Message("You cannot use that command in this gamemode.");
				p.cancelcommand = true;
				return;
			}
			
			if (cmd == "spawn") return; // don't do player check for spawn
      
			string[] bits = args.SplitSpaces();
			if (bits.Length > 1) return; // Don't want to do /tp x y z
			
			// don't want to intercept /tpaccept and /tpdeny
			if (cmd == "tpa" && (bits[0].CaselessEq("accept") || bits[0].CaselessEq("deny"))) return;
			
			Player who = PlayerInfo.FindMatches(p, bits[0]);
			if (who == null) { 
				p.cancelcommand = true; return; // Don't want double 'Player not found' message
			}
			
			if (OnGameMap(who.level.name)) {
				p.Message("You cannot teleport to someone in that gamemode.");
				p.cancelcommand = true;
				return;
			}
		}
	}
}
