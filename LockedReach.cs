using System;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Network;

namespace PluginLockedReach {
    public sealed class LockedReach : Plugin {
        public override string creator { get { return "Not UnknownShadow200"; } }
        public override string name { get { return "LockedReach"; } }
        public override string MCGalaxy_Version { get { return "1.9.2.4"; } }
        
        public override void Load(bool startup) {
            OnSendingMotdEvent.Register(HandleGettingMOTD, Priority.Low);
            OnPlayerCommandEvent.Register(OnPlayerCommand, Priority.Low);
        }
        
        public override void Unload(bool shutdown) {
            OnSendingMotdEvent.Unregister(HandleGettingMOTD);
            OnPlayerCommandEvent.Unregister(OnPlayerCommand);
        }
        
        static void SetReach(Player p, float reach) {
        	if (!p.Supports(CpeExt.ClickDistance)) return;        	
        	p.ReachDistance = reach;
        	p.Send(Packet.ClickDistance((short)(reach * 32)));
        }
        
        void HandleGettingMOTD(Player p, ref string motd) {
        	float? reach = GetLockedReach(motd);
            const string key = "US200.LockedReach.Reach";
            // Reach user had before joining a level with locked reach
            object origReach; p.Extras.TryGet(key, out origReach);
            
            if (reach == null) {
                // Restore the reach back to user's original reach
                if (origReach == null) return;
                p.Extras.Remove(key);
                SetReach(p, (float)origReach);
                return;
            }
            
            float curReach = p.ReachDistance;
            SetReach(p, reach.Value);            
            // Don't overwrite reach user had before joining a level with locked reach
            if (origReach != null) return;
            p.Extras[key] = curReach;
        }
        
        void OnPlayerCommand(Player p, string cmd, string args, CommandData data) {
            if (!cmd.CaselessEq("reachdistance")) return;
            float? max = GetLockedReach(p.GetMotd());
            if (max == null) return;
            
            float reach;
            if (!Utils.TryParseSingle(args, out reach)) return;
            if (reach <= max) return;
            
            p.Message("%WReach distance must be %S{0} %Wblocks or less in this level", max);
            p.cancelcommand = true;
        }

        static float? GetLockedReach(string motd) {
            // Does the motd have 'reach=' in it?
            int index = motd.IndexOf("reach=");
            if (index == -1) return null;
            motd = motd.Substring(index + "reach=".Length);
            
            // Get the single word after 'reach='
            if (motd.IndexOf(' ') >= 0)
                motd = motd.Substring(0, motd.IndexOf(' '));
            
            // Is there an actual word after 'reach='?
            if (motd.Length == 0) return null;
            
            float reach;
            if (!Utils.TryParseSingle(motd, out reach)) return null;
            return reach;
        }
    }
}