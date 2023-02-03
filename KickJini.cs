using System;
using MCGalaxy;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

public class KickJini : Plugin 
{
	public override string MCGalaxy_Version { get { return "1.9.4.3"; } }
	public override string name { get { return "KickJini"; } }
	
	public override void Load(bool startup) {
		OnPlayerFinishConnectingEvent.Register(KickClient, Priority.High);
	}
	
	public override void Unload(bool shutdown) {
		OnPlayerFinishConnectingEvent.Unregister(KickClient);
	}
	
	void KickClient(Player p) {
		string clientName = p.Session.ClientName();
		if (!clientName.CaselessContains("jini")) return;
		
		p.Leave("Do not use hack clients.", true);
		p.cancelconnecting = true;
	}
}