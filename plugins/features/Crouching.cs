//reference System.Core.dll

/*
    IMPORTANT:
    - This plugin does not fully replicate the crouching system from Minecraft.
    - You may press RSHIFT to toggle crouching instead of holding it on.
    - You can still fall off blocks but due to the slower speed when shifting, it is slightly harder to fall off.

    This plugin requires the HoldBlocks and CustomModels plugins to function properly!

    HoldBlocks: https://github.com/ddinan/ClassiCube-Stuff/blob/master/MCGalaxy/Plugins/HoldBlocks.cs
    CustomModels: https://github.com/NotAwesome2/MCGalaxy-CustomModels/releases/tag/v1.4.2

    Recommended setup (optional, but better if you do):
    1. /cm upload crouch https://www.dropbox.com/s/doq1g2q0fjmep4v/crouch.bbmodel
    2. /cm config crouch eyeY 24
    3. Press RSHIFT to toggle between crouching/not crouching
*/

using System;
using System.Linq;

using MCGalaxy.Commands;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

namespace MCGalaxy
{
    public class Crouching : Plugin
    {
        public override string name { get { return "Crouching"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.0"; } }
        public override string creator { get { return "Venk"; } }

        public override void Load(bool startup)
        {
            OnGettingMotdEvent.Register(HandleGettingMOTD, Priority.Low);

            Command.Register(new CmdCrouch());
        }


        public override void Unload(bool shutdown)
        {
            OnGettingMotdEvent.Unregister(HandleGettingMOTD);

            Command.Unregister(Command.Find("Crouch"));
        }

        static void HandleGettingMOTD(Player p, ref string motd)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (!p.Supports(CpeExt.TextHotkey)) continue;
                pl.Send(Packet.TextHotKey("Crouch", "/Crouch◙", 54, 0, true));
            }

            // Check if player has actually toggled crouch, since defaults to false
            if (!p.Extras.GetBoolean("IS_CROUCHING")) return;

            // Remove current horspeed rule because client does MOTD checking lamely
            motd = motd
                   .SplitSpaces()
                   .Where(word => !word.CaselessStarts("horspeed="))
                   .Join(" ");

            motd += " horspeed=0.52";
        }

        public override void Help(Player p)
        {
        }
    }

    public sealed class CmdCrouch : Command2
    {
        public override string name { get { return "Crouch"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (p.Extras.GetBoolean("IS_CROUCHING"))
            {
                p.Extras["IS_CROUCHING"] = false;
                p.Extras["HAS_CROUCHED"] = true;
                p.SendMapMotd();
                Command.Find("SilentModel").Use(p, "humanoid|1");
            }
            else
            {
                p.Extras["IS_CROUCHING"] = true;
                p.Extras["HAS_CROUCHED"] = true;
                p.SendMapMotd();
                Command.Find("SilentModel").Use(p, "crouch");
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Crouch &H- Toggles crouching.");
        }
    }
}
