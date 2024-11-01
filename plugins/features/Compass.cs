using System;
using System.Threading;

using MCGalaxy;
using MCGalaxy.Tasks;

namespace MCGalaxy
{
    public class Compass : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.8"; } }
        public override string name { get { return "Compass"; } }

        public static SchedulerTask task;

        public override void Load(bool startup)
        {
            task = Server.MainScheduler.QueueRepeat(CheckDirection, null, TimeSpan.FromMilliseconds(100));
        }

        void CheckDirection(SchedulerTask task)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players)
            {
                if (!p.Supports(CpeExt.MessageTypes)) continue;

                int yaw = Orientation.PackedToDegrees(p.Rot.RotY);

                // If value is the same, don't bother sending status packets to the client
                if (p.Extras.GetInt("COMPASS_VALUE") == yaw) continue;

                // Store yaw in extras values so we can retrieve it above
                p.Extras["COMPASS_VALUE"] = yaw;

                p.SendCpeMessage(CpeMessageType.Status1, "&SFacing:");

                if (yaw >= 337 || yaw < 22)
                    p.SendCpeMessage(CpeMessageType.Status2, "&bNorth");

                if (yaw >= 22 && yaw < 67)
                    p.SendCpeMessage(CpeMessageType.Status2, "&bNortheast");

                if (yaw >= 67 && yaw < 112)
                    p.SendCpeMessage(CpeMessageType.Status2, "&bEast");

                if (yaw >= 112 && yaw < 157)
                    p.SendCpeMessage(CpeMessageType.Status2, "&bSoutheast");

                if (yaw >= 157 && yaw < 202)
                    p.SendCpeMessage(CpeMessageType.Status2, "&bSouth");

                if (yaw >= 202 && yaw < 247)
                    p.SendCpeMessage(CpeMessageType.Status2, "&bSouthwest");

                if (yaw >= 247 && yaw < 292)
                    p.SendCpeMessage(CpeMessageType.Status2, "&bWest");

                if (yaw >= 292 && yaw < 337)
                    p.SendCpeMessage(CpeMessageType.Status2, "&bNorthwest");
            }
        }

        public override void Unload(bool shutdown)
        {
            Server.MainScheduler.Cancel(task);
        }
    }
}
