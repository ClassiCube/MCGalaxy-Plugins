using System;
using MCGalaxy;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

public class KickNoCC : Plugin 
{
	public override string creator { get { return "aleksb385"; } } //made from KickJini.cs
	public override string MCGalaxy_Version { get { return "1.9.4.3"; } }
	public override string name { get { return "KickNoCC"; } }
	
	public override void Load(bool startup) {
		OnPlayerFinishConnectingEvent.Register(KickClient, Priority.High);
	}
	
	public override void Unload(bool shutdown) {
		OnPlayerFinishConnectingEvent.Unregister(KickClient);
	}
	
	void KickClient(Player p) {
		string clientName = p.Session.ClientName();
		if (clientName.CaselessContains("ClassiCube ")) return;
		
		p.Leave("Please connect using the ClassiCube client with Enhanced mode.", true);
		p.cancelconnecting = true; 
	}
}