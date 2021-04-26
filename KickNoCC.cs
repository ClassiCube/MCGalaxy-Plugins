using System;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy {
	
	public class PluginKickNoCC : Plugin {
		public override string creator { get { return "aleksb385"; } } //made from KickJini.cs
		public override string MCGalaxy_Version { get { return "1.9.0.0"; } }
		public override string name { get { return "KickNoCC"; } }

		public override void Load(bool startup) {
			OnPlayerConnectEvent.Register(KickNoCC, Priority.High);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerConnectEvent.Unregister(KickNoCC);
		}

		void KickNoCC(Player p) {
			string app = p.appName;            
			if (app == null || !app.CaselessContains("ClassiCube ")) {
				p.Leave("Please connect using the ClassiCube client with Enhanced mode.", true);
				p.cancellogin = true; 
		    }
		}
    }
}
