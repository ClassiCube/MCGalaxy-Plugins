using System;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;

public class LowLatency : Plugin
{
	public override string name { get { return "LowLatency"; } }
	public override string MCGalaxy_Version { get { return "1.9.3.8"; } }
	public override string creator { get { return "Not UnknownShadow200"; } }
	
	public override void Load(bool startup) {
		OnPlayerFinishConnectingEvent.Register(OnPlayerFinishConnecting, Priority.Low);
		SetAllNoDelay(true);
	}
	
	public override void Unload(bool shutdown) {
		OnPlayerFinishConnectingEvent.Unregister(OnPlayerFinishConnecting);
		SetAllNoDelay(false);
	}
	
	static void OnPlayerFinishConnecting(Player p) {
		SetNoDelay(p, true);
	}
	
	
	static void SetNoDelay(Player p, bool noDelay) {
		try {
			p.Socket.LowLatency = noDelay;
		} catch {
			// exception can be rarely thrown when socket is disconnected
		}
	}
	
	static void SetAllNoDelay(bool noDelay) {
		Player[] players = PlayerInfo.Online.Items;
		foreach (Player pl in players) { SetNoDelay(pl, noDelay); }
	}
}
