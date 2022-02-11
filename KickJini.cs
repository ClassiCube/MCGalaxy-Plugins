using System;
using MCGalaxy;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace PluginKickJini 
{
	public class KickJini : Plugin 
	{
		public override string creator { get { return "aaaa"; } }
		public override string MCGalaxy_Version { get { return "1.9.1.4"; } }
		public override string name { get { return "KickJini"; } }
		
		public override void Load(bool startup) {
			OnPlayerConnectEvent.Register(KickClient, Priority.High);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerConnectEvent.Unregister(KickClient);
		}
		
		void KickClient(Player p) {
			string app = p.appName;
			if (app != null && app.CaselessContains("jini")) {
				p.Leave("Do not use hack clients.", true);
				p.cancellogin = true;
			}
		}
	}
}
