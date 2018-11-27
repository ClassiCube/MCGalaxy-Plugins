using System;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;

namespace PluginNoTP {
	public sealed class Core : Plugin_Simple {
		public override string creator { get { return "Not UnknownShadow200"; } }
		public override string name { get { return "NoTp"; } }
		public override string MCGalaxy_Version { get { return "1.8.9.7"; } }
		
		public override void Load(bool startup) {
			OnPlayerCommandEvent.Register(OnPlayerCommand, Priority.Low);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerCommandEvent.Unregister(OnPlayerCommand);
		}
		void OnPlayerCommand(Player p, string cmd, string args) {
			cmd = cmd.ToLower();
			if (!(cmd == "tp" || cmd == "teleport" || cmd == "tpa" || cmd == "spawn")) return;
			
			if (p.level.name.CaselessStarts("zs")) {
				Player.Message(p, "You cannot use that command in this gamemode.");
				p.cancelcommand = true;
				return;
			}
			
			if (cmd == "spawn") return; // don't do player check for spawn
      
			string[] bits = args.SplitSpaces();
			if (bits.Length > 1) return; // Don't want to do /tp x y z
			
			Player who = PlayerInfo.FindMatches(p, bits[0]);
			if (who == null) { 
				p.cancelcommand = true; return; // Don't want double 'Player not found' message
			}
			
			if (who.level.name.CaselessStarts("zs")) {
				Player.Message(p, "You cannot use that command in this gamemode.");
				p.cancelcommand = true;
				return;
			}
		}
	}
}
