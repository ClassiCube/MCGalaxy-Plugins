using System;
using System.Collections.Generic;

using MCGalaxy;
using MCGalaxy.Blocks.Extended;
using MCGalaxy.Bots;
using MCGalaxy.Commands;
using MCGalaxy.Commands.CPE;
using MCGalaxy.Maths;
using MCGalaxy.Network;

using BlockID = System.UInt16;

namespace Core
{
    public class VenkLib : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.1"; } }
        public override string name { get { return "VenkLib"; } }

        public override void Load(bool startup)
        {
            Command.Register(new CmdAdventure());
            Command.Register(new CmdAnnounce());
            Command.Register(new CmdBoost());
            Command.Register(new CmdListLevels());
            Command.Register(new CmdMoveEverything());
            Command.Register(new CmdSilentHold());
            Command.Register(new CmdSilentModel());
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Adventure"));
            Command.Unregister(Command.Find("Announce"));
            Command.Unregister(Command.Find("Boost"));
            Command.Unregister(Command.Find("ListLevels"));
            Command.Unregister(Command.Find("MoveEverything"));
            Command.Unregister(Command.Find("SilentHold"));
            Command.Unregister(Command.Find("SilentModel"));
        }
    }

    public sealed class CmdAdventure : Command2
    {
        public override string name { get { return "Adventure"; } }
        public override string shortcut { get { return "ad"; } }
        public override string type { get { return "World"; } }

        public override void Use(Player p, string message, CommandData data)
        {
            Command.Find("Map").Use(p, "buildable");
            Command.Find("Map").Use(p, "deletable");
        }

        public override void Help(Player p)
        {
            p.Message("&T/Adventure %H- Toggles adventure mode for a map.");
        }
    }
    public sealed class CmdAnnounce : Command2
    {
        public override string name { get { return "Announce"; } }
        public override string shortcut { get { return "ann"; } }
        public override string type { get { return "other"; } }

        public override CommandPerm[] ExtraPerms
        {
            get
            {
                return new[] { new CommandPerm(LevelPermission.Operator, "can announce messages to the level they are in."),
                    new CommandPerm(LevelPermission.Admin, "can announce messages to the whole server.") };
            }
        }

        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces(2);

            if (args[0].Length > 0)
            {
                if (args[0].CaselessEq("level") || args[0].CaselessEq("global"))
                {
                    if (args[0].CaselessEq("level"))
                    {
                        if (args[1].Length > 0)
                        {
                            if (!HasExtraPerm(p, data.Rank, 1)) return;
                            foreach (Player pl in PlayerInfo.Online.Items)
                            {
                                if (pl.level != p.level) continue;
                                pl.SendCpeMessage(CpeMessageType.Announcement, args[1]);
                            }
                        }
                        else { Help(p); }
                    }

                    else if (args[0].CaselessEq("global"))
                    {
                        if (args[1].Length > 0)
                        {
                            if (!HasExtraPerm(p, data.Rank, 2)) return;
                            foreach (Player pl in PlayerInfo.Online.Items)
                            {
                                pl.SendCpeMessage(CpeMessageType.Announcement, args[1]);
                            }
                        }

                        else { Help(p); }
                    }
                }

                else
                {
                    p.SendCpeMessage(CpeMessageType.Announcement, message);
                }
            }

            else { Help(p); }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Announce [message] %H- Displays a message on your screen.");
            p.Message("&T/Announce level [message] %H- Displays a message on players' screens in your level.");
            p.Message("&T/Announce global [message] %H- Displays a message on players' screens globally.");
        }
    }
    public sealed class CmdBoost : Command2
    {
        public override string name { get { return "Boost"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message == "") { Help(p); return; }
            if (!p.Supports("VelocityControl", 1))
            {
                p.Message("&cYour client does not support velocity changes! Please update to the latest ClassiCube version"); return;
            }
            string[] args = message.SplitSpaces(6);
            if (args.Length < 6) { Help(p); return; }

            float x, y, z;
            byte xmode, ymode, zmode;

            // Input validation
            if (!float.TryParse(args[0], out x) | !float.TryParse(args[1], out y) |
                !float.TryParse(args[2], out z) | !byte.TryParse(args[3], out xmode) |
                !byte.TryParse(args[4], out ymode) | !byte.TryParse(args[5], out zmode))
            {
                Help(p); return;
            }
            if (xmode < 0 | xmode > 1)
            {
                p.Message("&cxmode must be 0 or 1!"); return;
            }
            if (ymode < 0 | ymode > 1)
            {
                p.Message("&cymode must be 0 or 1!"); return;
            }
            if (zmode < 0 | zmode > 1)
            {
                p.Message("&czmode must be 0 or 1!"); return;
            }
            if (x < -2048f | x > 2048f)
            {
                p.Message("&cX must be between -2048 and 2048"); return;
            }
            if (y < -2048f | y > 2048f)
            {
                p.Message("&cY must be between -2048 and 2048"); return;
            }
            if (z < -2048f | z > 2048f)
            {
                p.Message("&cZ must be between -2048 and 2048"); return;
            }

            p.Send(Packet.VelocityControl(x, y, z, xmode, ymode, zmode));

            // Allow air message block to repeat every time
            p.prevMsg = "";
        }

        public override void Help(Player p)
        {
            p.Message("&T/Boost [x] [y] [z] [xmode] [ymode] [zmode]");
            p.Message("&HChanges the player's velocity");
            p.Message("&Hmode 0 means that current velocity is added to");
            p.Message("&Hmode 1 means that current velocity is replaced");
            p.Message("&HThe command will always be executed, even in air MBs");
        }
    }
    public sealed class CmdListLevels : Command2
    {
        public override string name { get { return "ListLevels"; } }
        public override string type { get { return CommandTypes.Information; } }
        public override bool UseableWhenFrozen { get { return true; } }

        public override void Use(Player p, string message, CommandData data)
        {
            string[] maps = LevelInfo.AllMapNames();

            List<string> levels = new List<string>();

            string[] args = message.SplitSpaces();

            Group grp = null;

            if (args.Length > 0)
            {
                grp = Matcher.FindRanks(p, args[0]);
                if (grp == null) return;


                foreach (string map in maps)
                {
                    LevelConfig cfg = LevelInfo.GetConfig(map);
                    if (cfg == null) continue;

                    if (cfg.BuildMin == grp.Permission || cfg.VisitMin == grp.Permission)
                    {
                        levels.Add(map);
                    }
                }

                maps = levels.ToArray();
            }

            else Help(p);

            if (maps.Length == 0)
            {
                p.Message("There are no levels with this permission.");
                return;
            }

            p.Message(string.Join("%S, " + grp.Color, maps));
        }

        public override void Help(Player p)
        {
            p.Message("&T/ListLevels [rank]");
            p.Message("&HLists loaded levels and their physics levels.");
        }
    }
    public sealed class CmdMoveEverything : Command2
    {
        public override string name { get { return "MoveEverything"; } }
        public override string shortcut { get { return "shift"; } }
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
            // Convert from block positions into precise positions

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
            p.Message("&SSuccessfully moved %b" + count + " %Sbots.");
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
                    p.Message("&cMB at %b" + pos.X + " " + pos.Y + " " + pos.Z + " %cwas outside of the map bounds, deleting.");
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

            p.Message("&SSuccessfully moved %b" + count + " %SMBs.");
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
                    p.Message("&cPortal at %b" + pos.X + " " + pos.Y + " " + pos.Z + " %cwas outside of the map bounds, deleting.");
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

            p.Message("&SSuccessfully moved %b" + count + " %Sportals.");
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
    public sealed class CmdSilentHold : Command2
    {
        public override string name { get { return "SilentHold"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }
        public override bool MessageBlockRestricted { get { return false; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0)
            {
                Help(p);
                return;
            }
            string[] args = message.SplitSpaces(2);

            BlockID block;
            if (!CommandParser.GetBlock(p, args[0], out block)) return;
            bool locked = false;
            if (args.Length > 1 && !CommandParser.GetBool(p, args[1], ref locked)) return;

            if (Block.IsPhysicsType(block))
            {
                p.Message("Cannot hold physics blocks");
                return;
            }

            if (!p.Session.SendHoldThis(block, locked)) 
            {
                p.Message("Your client doesn't support changing your held block.");
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/SilentHold [block] <locked>");
            p.Message("&HMakes you hold the given block in your hand");
            p.Message("&H  <locked> optionally prevents you from changing it");
            p.Message("&HLiterally the same as /hold but it doesn't send a msg to the player.");
        }
    }
    public sealed class CmdSilentModel : EntityPropertyCmd
    {
        public override string name { get { return "SilentModel"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        public override CommandPerm[] ExtraPerms
        {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can change the model of others") }; }
        }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message.IndexOf(' ') == -1)
            {
                message = "-own " + message;
                message = message.TrimEnd();
            }
            UseBotOrOnline(p, data, message, "model");
        }

        protected override void SetBotData(Player p, PlayerBot bot, string model)
        {
            model = ParseModel(p, bot, model);
            if (model == null) return;
            bot.UpdateModel(model);

            BotsFile.Save(p.level);
        }

        protected override void SetOnlineData(Player p, Player who, string model)
        {
            string orig = model;
            model = ParseModel(p, who, model);
            if (model == null) return;
            who.UpdateModel(model);

            if (!model.CaselessEq("humanoid"))
            {
                Server.models.Update(who.name, model);
            }
            else
            {
                Server.models.Remove(who.name);
            }
            Server.models.Save();

            // Remove model scale too when resetting model
            //if (orig.Length == 0) CmdModelScale.UpdateSavedScale(who);
        }

        static string ParseModel(Player dst, Entity e, string model)
        {
            // Reset entity's model
            if (model.Length == 0)
            {
                e.ScaleX = 0; e.ScaleY = 0; e.ScaleZ = 0;
                return "humanoid";
            }

            model = model.ToLower();
            model = model.Replace(':', '|'); // since users assume : is for scale instead of |.

            float max = ModelInfo.MaxScale(e, model);
            // restrict player model scale, but bots can have unlimited model scale
            if (ModelInfo.GetRawScale(model) > max)
            {
                dst.Message("%WScale must be {0} or less for {1} model",
                            max, ModelInfo.GetRawModel(model));
                return null;
            }
            return model;
        }

        public override void Help(Player p) { }
    }
}