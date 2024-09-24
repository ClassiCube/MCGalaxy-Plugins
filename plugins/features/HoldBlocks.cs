// This plugin requires my VenkLib plugin. Install it from here: https://github.com/ddinan/ClassiCube-Stuff/blob/master/MCGalaxy/Plugins/VenkLib.cs.
// To add: Set map MOTD to include +hold then every block you change to will update your model.
// E.g, /map motd +hold
// NOTE: Does not work if you are not a human or hold model

using System;
using System.Collections.Generic;
using System.IO;

using MCGalaxy;
using MCGalaxy.Bots;
using MCGalaxy.Commands;
using MCGalaxy.Commands.CPE;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events;
using MCGalaxy.Tasks;

using BlockID = System.UInt16;

namespace Core
{
    public class HoldBlocks : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.4"; } }
        public override string name { get { return "HoldBlocks"; } }

        public static SchedulerTask Task;

        public override void Load(bool startup)
        {
            OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.High);
            Server.MainScheduler.QueueRepeat(DoBlockLoop, null, TimeSpan.FromMilliseconds(100));
        }

        public override void Unload(bool shutdown)
        {
            OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
            Server.MainScheduler.Cancel(Task);
        }

        void DoBlockLoop(SchedulerTask task)
        {
            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                // Get MOTD of map
                if (!pl.level.Config.MOTD.ToLower().Contains("+hold") && !Server.Config.MOTD.Contains("+hold")) continue;
                if (!pl.Model.Contains("human") && !pl.Model.Contains("hold") && !pl.Model.Contains("-own")) continue;
                BlockID block = pl.GetHeldBlock();
                string holding = Block.GetName(pl, block);

                if (pl.Extras.GetString("HOLDING_BLOCK") != holding)
                {
                    int scale = block;
                    if (scale >= 66) scale = block - 256; // Need to convert block if ID is over 66
                    if (scale >= 100) Command.Find("SilentModel").Use(pl, "-own hold|1." + scale);
                    else if (scale >= 10) Command.Find("SilentModel").Use(pl, "-own hold|1.0" + scale);
                    else if (scale > 0) Command.Find("SilentModel").Use(pl, "-own hold|1.00" + scale);
                    else Command.Find("SilentModel").Use(pl, "-own humanoid|1");
                }
                pl.Extras["HOLDING_BLOCK"] = holding;
            }

            Task = task;
        }

        void HandlePlayerConnect(Player p)
        {
            p.Extras["HOLDING_BLOCK"] = null;
        }
    }
}