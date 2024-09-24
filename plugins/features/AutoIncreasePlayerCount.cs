using System;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy
{
    public class PlayerCount : Plugin
    {
        public override string name { get { return "AutoIncreasePlayerCount"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.1"; } }
        public override string creator { get { return "Venk"; } }

        public override void Load(bool startup)
        {
            OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.Low);
            OnPlayerDisconnectEvent.Register(HandlePlayerDisconnect, Priority.Low);
        }

        public override void Unload(bool shutdown)
        {
            OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
            OnPlayerDisconnectEvent.Unregister(HandlePlayerDisconnect);
        }

        void HandlePlayerConnect(Player p)
        {
            Server.Config.MaxPlayers += 1;
        }

        void HandlePlayerDisconnect(Player p, string reason)
        {
            Server.Config.MaxPlayers -= 1;
        }

        public override void Help(Player p)
        {
        }
    }
}
