using System;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy {
	
	public class PluginKickNoCC : Plugin {
		public override string creator { get { return "aleksb385"; } } //made from KickJini.cs
		public override string MCGalaxy_Version { get { return "1.9.0.0"; } } //this could be anything
		public override string name { get { return "KickNoCC"; } }
        public override bool LoadAtStartup { get { return true; } }

		public override void Load(bool startup) {
			OnPlayerConnectEvent.Register(KickNoCC, Priority.High);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerConnectEvent.Unregister(KickNoCC);
		}
		
                void KickPlayer(Player p) {
                        p.Leave("Please connect using the ClassiCube client with Enhanced mode.", true);
                        p.cancellogin = true; 
                        return;
                }

                void KickNoCC(Player p) {
                        string app = p.appName;			
                        if(app == null) {
                                KickPlayer(p);
                        } else if(!app.CaselessContains("ClassiCube ")) {
                                KickPlayer(p);
                        }
                        KickPlayer(p);
                }
    }
}
