using System;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy {

	public class TeamChatPlugin : Plugin_Simple {
		public override string creator { get { return "Not UnknownShadow200"; } }
		public override string MCGalaxy_Version { get { return "1.9.0.1"; } }
		public override string name { get { return "TeamChatPlugin"; } }
		
		public override void Load(bool startup) {
			OnPlayerChatEvent.Register(DoTeamChat, Priority.Low);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerChatEvent.Unregister(DoTeamChat);
		}
		
		void DoTeamChat(Player p, string message) {
			if (p.cancelchat || message.Length <= 1 || message[0] != '=') return;
			
			if (p.Game.Team == null) {
				p.Message("You are not on a team, so cannot send a team message.");
			} else {
				p.Game.Team.Message(p, message.Substring(1));
			}
			p.cancelchat = true;
		}
	}
}
