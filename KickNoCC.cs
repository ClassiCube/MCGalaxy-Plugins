using System;
using MCGalaxy;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace PluginKickNoCC
{
	public class KickNoCC : Plugin 
	{
		public override string creator { get { return "aleksb385"; } } //made from KickJini.cs
		public override string MCGalaxy_Version { get { return "1.9.0.0"; } }
		public override string name { get { return "KickNoCC"; } }

		public override void Load(bool startup) {
			OnPlayerConnectEvent.Register(KickClient, Priority.High);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerConnectEvent.Unregister(KickClient);
		}

		void KickClient(Player p) {
			string app = p.appName;
			if (app == null || !app.CaselessContains("ClassiCube ")) {
				p.Leave("Please connect using the ClassiCube client with Enhanced mode.", true);
				p.cancellogin = true; 
		    }
		}
    }
}
