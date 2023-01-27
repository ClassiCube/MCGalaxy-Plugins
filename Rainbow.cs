using System;
using MCGalaxy;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

namespace PluginRainbowColors
{
    public sealed class RainbowPlugin : Plugin 
    {
        public override string creator { get { return "Not UnknownShadow200"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.4"; } }
        public override string name { get { return "Rainbow"; } }

        SchedulerTask task;
        public override void Load(bool startup) {
           task = Server.MainScheduler.QueueRepeat(RainbowCallback, null,
        	                                        TimeSpan.FromMilliseconds(100));
        }
        
        public override void Unload(bool shutdown) {
        	Server.MainScheduler.Cancel(task);
        }
        
        static string[] colors = { "9400D3", "4B0082", "0000FF", "00FF00", "FFFF00", "FF7F00", "FF0000" };
        static int index;
        static void RainbowCallback(SchedulerTask task) {
        	index = (index + 1) % colors.Length;
        	
        	ColorDesc desc = Colors.ParseHex(colors[index]);
            desc.Code = 'r';
        	Player[] players = PlayerInfo.Online.Items;
            
        	foreach (Player p in players) 
			{
        		p.Session.SendSetTextColor(desc);
        	}
        }
    }
}