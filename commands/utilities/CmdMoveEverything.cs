// Credit to Goodlyay for the source code for moving bots

using System.Collections.Generic;

using MCGalaxy;
using MCGalaxy.Blocks.Extended;
using MCGalaxy.Bots;
using MCGalaxy.Commands;
using MCGalaxy.Maths;

using BlockID = System.UInt16;

public class CmdMoveEverything : Command2
{
    public override string name { get { return "MoveEverything"; } }
    public override string shortcut { get { return "mve"; } }
    public override bool MessageBlockRestricted { get { return true; } }
    public override string type { get { return "other"; } }
    public override bool museumUsable { get { return false; } }
    public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

    static bool OwnsMap(Player p, Level lvl)
    {
        if (lvl.name.CaselessStarts(p.name)) return true;
        string[] owners = lvl.Config.RealmOwner.Replace(" ", "").Split(',');

        foreach (string owner in owners)
        {
            if (owner.CaselessEq(p.name)) return true;
        }
        return false;
    }

    #region Bots

    public static void MoveBots(Player p, int x, int y, int z)
    {
        // Convert block positions into precise positions

        x *= 32;
        y *= 32;
        z *= 32;

        Position pos;
        byte yaw, pitch;
        PlayerBot[] bots = p.level.Bots.Items;

        int count = 0;

        for (int i = 0; i < bots.Length; i++)
        {
            pos.X = bots[i].Pos.X + x;
            pos.Y = bots[i].Pos.Y + y;
            pos.Z = bots[i].Pos.Z + z;
            yaw = bots[i].Rot.RotY; pitch = bots[i].Rot.HeadX;
            bots[i].Pos = pos;
            bots[i].SetYawPitch(yaw, pitch);

            count++;
        }

        BotsFile.Save(p.level);
        p.Message("&SSuccessfully moved &b" + count + " &Sbots.");
    }

    #endregion

    #region Message blocks

    void MoveMessageBlocks(Player p, int x, int y, int z)
    {
        List<Vec3U16> coords = MessageBlock.GetAllCoords(p.level.MapName);

        int count = 0;

        foreach (Vec3U16 pos in coords)
        {
            string message = MessageBlock.Get(p.level.MapName, pos.X, pos.Y, pos.Z);

            if (message == null) continue;
            BlockID block = p.level.FastGetBlock(pos.X, pos.Y, pos.Z);

            int x2 = pos.X + x;
            int y2 = pos.Y + y;
            int z2 = pos.Z + z;

            if (!p.level.IsValidPos(x2, y2, z2))
            {
                p.Message("&cMB at &b" + pos.X + " " + pos.Y + " " + pos.Z + " &cwas outside of the map bounds, deleting.");
                MessageBlock.Delete(p.level.name, pos.X, pos.Y, pos.Z);
                p.level.UpdateBlock(p, pos.X, pos.Y, pos.Z, Block.Air);
                continue;
            }

            // Create new MBs
            MessageBlock.Set(p.level.name, (ushort)x2, (ushort)y2, (ushort)z2, message);
            p.level.UpdateBlock(p, (ushort)x2, (ushort)y2, (ushort)z2, block);

            // Delete old MBs
            MessageBlock.Delete(p.level.name, pos.X, pos.Y, pos.Z);
            p.level.UpdateBlock(p, pos.X, pos.Y, pos.Z, Block.Air);

            count++;
        }

        p.Message("&SSuccessfully moved &b" + count + " &SMBs.");
    }

    #endregion

    #region Portals

    void MovePortals(Player p, int x, int y, int z)
    {
        List<Vec3U16> coords = Portal.GetAllCoords(p.level.MapName);

        int count = 0;

        foreach (Vec3U16 pos in coords)
        {
            if (!Portal.ExistsInDB(p.level.MapName)) continue;
            PortalExit exit = Portal.Get(p.level.MapName, pos.X, pos.Y, pos.Z);

            BlockID block = p.level.FastGetBlock(pos.X, pos.Y, pos.Z);

            int x2 = pos.X + x;
            int y2 = pos.Y + y;
            int z2 = pos.Z + z;

            int dx = exit.X + x;
            int dy = exit.Y + y;
            int dz = exit.Z + z;

            if (!p.level.IsValidPos(x2, y2, z2))
            {
                p.Message("&cPortal at &b" + pos.X + " " + pos.Y + " " + pos.Z + " &cwas outside of the map bounds, deleting.");
                Portal.Delete(p.level.name, pos.X, pos.Y, pos.Z);
                p.level.UpdateBlock(p, pos.X, pos.Y, pos.Z, Block.Air);
                continue;
            }

            // Create new portals
            Portal.Set(p.level.name, (ushort)x2, (ushort)y2, (ushort)z2, (ushort)dx, (ushort)dy, (ushort)dz, exit.Map);
            p.level.UpdateBlock(p, (ushort)x2, (ushort)y2, (ushort)z2, block);

            // Delete old portals
            Portal.Delete(p.level.name, pos.X, pos.Y, pos.Z);
            p.level.UpdateBlock(p, pos.X, pos.Y, pos.Z, Block.Air);

            count++;
        }

        p.Message("&SSuccessfully moved &b" + count + " &Sportals.");
    }

    #endregion

    public override void Use(Player p, string message, CommandData data)
    {
        if (message.Length == 0) { Help(p); return; }
        bool canUse = false; // = p.group.Permission >= p.level.BuildAccess.Min;

        if (OwnsMap(p, p.level) || p.group.Permission >= LevelPermission.Operator) canUse = true;
        if (!canUse)
        {
            p.Message("&cYou can only use this command on your own maps."); return;
        }

        string[] bits = message.SplitSpaces(5);
        if (bits.Length < 3) { Help(p); return; }
        // x y z 3		    
        int x = -1, y = -1, z = -1;

        if (!CommandParser.GetInt(p, bits[0], "X delta", ref x)) { return; }
        if (!CommandParser.GetInt(p, bits[1], "Y delta", ref y)) { return; }
        if (!CommandParser.GetInt(p, bits[2], "Z delta", ref z)) { return; }

        MoveBots(p, x, y, z);
        MoveMessageBlocks(p, x, y, z);
        MovePortals(p, x, y, z);
    }

    public override void Help(Player p)
    {
        p.Message("&T/MoveEverything [x y z]");
        p.Message("&HMoves all bots/MBs/portals in the map you're in by [x y z].");
        p.Message("&HFor example, 0 1 0 would move everything up by 1 block.");
    }
}
