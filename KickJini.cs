using System;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy {
	
	public class PluginKickJini : Plugin_Simple {
		public override string creator { get { return "aaaa"; } }
		public override string MCGalaxy_Version { get { return "1.9.0.0"; } }
		public override string name { get { return "KickJini"; } }
		
		public override void Load(bool startup) {
			OnPlayerConnectEvent.Register(DoKickJini, Priority.High);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerConnectEvent.Unregister(DoKickJini);
		}
		
		void DoKickJini(Player p) {
			string app = p.appName;			
			if (app != null && app.CaselessContains("jini")) {
				p.Leave("Do not use hack clients.", true);
				p.cancellogin = true;
			}
		}
	}
}
