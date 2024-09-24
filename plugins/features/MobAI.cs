//reference System.Core.dll
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using MCGalaxy.Bots;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Maths;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

using BlockID = System.UInt16;

namespace MCGalaxy
{
    public sealed class MobAI : Plugin
    {
        public override string name { get { return "MobAI"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.1"; } }
        public override string creator { get { return "Venk"; } }

        BotInstruction drop;
        BotInstruction hostile;
        BotInstruction roam;
        BotInstruction run;
        BotInstruction smart;
        BotInstruction smarthunt;
        BotInstruction spleef;
        BotInstruction tnt;

        public override void Load(bool startup)
        {
            // Initialize bot AIs
            drop = new DropInstruction();
            hostile = new HostileInstruction();
            roam = new RoamInstruction();
            run = new RunInstruction();
            smart = new SmartInstruction();
            smarthunt = new SmartHuntInstruction();
            spleef = new SpleefInstruction();
            tnt = new TNTInstruction();

            // Create bot AIs
            BotInstruction.Instructions.Add(drop);
            BotInstruction.Instructions.Add(hostile);
            BotInstruction.Instructions.Add(roam);
            BotInstruction.Instructions.Add(run);
            BotInstruction.Instructions.Add(smart);
            BotInstruction.Instructions.Add(smarthunt);
            BotInstruction.Instructions.Add(spleef);
            BotInstruction.Instructions.Add(tnt);
        }

        public override void Unload(bool shutdown)
        {
            // Delete bot AIs
            BotInstruction.Instructions.Remove(drop);
            BotInstruction.Instructions.Remove(hostile);
            BotInstruction.Instructions.Remove(roam);
            BotInstruction.Instructions.Remove(run);
            BotInstruction.Instructions.Remove(smart);
            BotInstruction.Instructions.Remove(smarthunt);
            BotInstruction.Instructions.Remove(spleef);
            BotInstruction.Instructions.Remove(tnt);
        }

        /// <summary>
        /// Determines the axis to check for blocks around the bot with.
        /// 
        /// North = Z -> 0
        /// East = 0 -> X
        /// South = 0 -> Z
        /// West = X -> 0
        /// </summary>
        public static string axis = "none";

        /// <summary>
        /// Calculates cardinal direction based on bot's yaw value.
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>

        public static string CalculateCardinal(PlayerBot bot)
        {
            if (Orientation.PackedToDegrees(bot.Rot.RotY) >= 0 && Orientation.PackedToDegrees(bot.Rot.RotY) < 45) return "North";
            if (Orientation.PackedToDegrees(bot.Rot.RotY) >= 45 && Orientation.PackedToDegrees(bot.Rot.RotY) < 90) return "Northeast";
            if (Orientation.PackedToDegrees(bot.Rot.RotY) >= 90 && Orientation.PackedToDegrees(bot.Rot.RotY) < 135) return "East";
            if (Orientation.PackedToDegrees(bot.Rot.RotY) >= 135 && Orientation.PackedToDegrees(bot.Rot.RotY) < 180) return "Southeast";
            if (Orientation.PackedToDegrees(bot.Rot.RotY) >= 180 && Orientation.PackedToDegrees(bot.Rot.RotY) < 225) return "South";
            if (Orientation.PackedToDegrees(bot.Rot.RotY) >= 225 && Orientation.PackedToDegrees(bot.Rot.RotY) < 270) return "Southwest";
            if (Orientation.PackedToDegrees(bot.Rot.RotY) >= 270 && Orientation.PackedToDegrees(bot.Rot.RotY) < 315) return "West";
            if (Orientation.PackedToDegrees(bot.Rot.RotY) >= 315 && Orientation.PackedToDegrees(bot.Rot.RotY) < 361) return "Northwest";
            return "";
        }

        /// <summary>
        /// Since bots travel at ~1.4x (40%) faster speeds on diagonals, we need to make the bot's speed slower.
        /// </summary>
        /// <param name="bot"></param>

        public static void SetDirectionalSpeeds(PlayerBot bot)
        {
            if (CalculateCardinal(bot) == "Northeast" || CalculateCardinal(bot) == "Northwest" || CalculateCardinal(bot) == "Southeast" || CalculateCardinal(bot) == "Southwest")
            {
                bot.movementSpeed = (int)Math.Round(3m * 60 / 100m); // 40% slower on diagonals
            }

            else
            {
                if (CalculateCardinal(bot) == "North" || CalculateCardinal(bot) == "South") axis = "Z";
                if (CalculateCardinal(bot) == "East" || CalculateCardinal(bot) == "West") axis = "X";
                bot.movementSpeed = (int)Math.Round(3m * 100 / 100m); // Regular speed on non-diagonals
            }
        }

        /// <summary>
        /// The player closest to the bot.
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="search"></param>
        /// <returns></returns>

        internal static Player ClosestPlayer(PlayerBot bot, int search)
        {
            int maxDist = search * 32;
            Player[] players = PlayerInfo.Online.Items;
            Player closest = null;

            foreach (Player p in players)
            {
                if (p.level != bot.level || p.invincible || p.hidden) continue;

                int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;
                int playerDist = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                if (playerDist >= maxDist) continue;

                closest = p;
                maxDist = playerDist;
            }
            return closest;
        }

        /// <summary>
        /// The bot closest to the bot.
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="search"></param>
        /// <returns></returns>

        internal static PlayerBot ClosestBot(PlayerBot bot, int search)
        {
            int maxDist = search * 32;
            Player[] players = PlayerInfo.Online.Items;
            PlayerBot closest = null;

            PlayerBot[] bots = bot.level.Bots.Items;
            foreach (PlayerBot b in bots)
            {
                if (b == bot) continue;

                int dx = b.Pos.X - bot.Pos.X, dy = b.Pos.Y - bot.Pos.Y, dz = b.Pos.Z - bot.Pos.Z;
                int botDist = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                if (botDist >= maxDist) continue;

                closest = b;
                maxDist = botDist;
            }
            return closest;
        }

        /// <summary>
        /// The zombie closest to the bot.
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="search"></param>
        /// <returns></returns>

        internal static PlayerBot ClosestZombie(PlayerBot bot, int search)
        {
            int maxDist = search * 32;
            Player[] players = PlayerInfo.Online.Items;
            PlayerBot closest = null;

            PlayerBot[] bots = bot.level.Bots.Items;
            foreach (PlayerBot b in bots)
            {
                if (b == bot) continue;
                if (b.Model != "zombie") continue;

                int dx = b.Pos.X - bot.Pos.X, dy = b.Pos.Y - bot.Pos.Y, dz = b.Pos.Z - bot.Pos.Z;
                int botDist = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                if (botDist >= maxDist) continue;

                closest = b;
                maxDist = botDist;
            }
            return closest;
        }

        /// <summary>
        /// Face bot towards target position.
        /// </summary>
        /// <param name="bot"></param>

        public static void FaceTowards(PlayerBot bot)
        {
            int dstHeight = ModelInfo.CalcEyeHeight(bot);

            int dx = (bot.TargetPos.X) - bot.Pos.X, dy = bot.Rot.RotY, dz = (bot.TargetPos.Z) - bot.Pos.Z;
            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            dir = Vec3F32.Normalise(dir);

            Orientation rot = bot.Rot;
            DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);
            bot.Rot = rot;
        }

        /// <summary>
        /// Whether or not the bot is "inside" the player. Note the hitbox is 0.875 so ping will affect how far/close players are from the bot.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="dist"></param>
        /// <returns></returns>

        public static bool InRange(Player a, PlayerBot b, int dist)
        {
            int dx = Math.Abs(a.Pos.X - b.Pos.X);
            int dy = Math.Abs(a.Pos.Y - b.Pos.Y);
            int dz = Math.Abs(a.Pos.Z - b.Pos.Z);
            return dx <= dist && dy <= dist && dz <= dist;
        }
    }

    public sealed class Metadata
    {
        public int waitTime;
        public int walkTime;
        public int lookTime;
        public int bowCooldown;
        public int explodeTime;
        public int jumpHeight;
        public int search;
        public int randomTick;
        public int tickDelay;
        public int velocityX;
        public int velocityY;
        public int velocityZ;
        public bool flanking;
        public Player chasing;
    }

    class Location
    {
        public int X;
        public int Z;
        public int F;
        public int G;
        public int H;
        public Location Parent;
    }

    #region Drop AI

    /* 
        Current AI behaviour:

        -   Spin
        -   Fall until block hits the ground

     */

    sealed class DropInstruction : BotInstruction
    {
        public DropInstruction() { Name = "drop"; }

        public override bool Execute(PlayerBot bot, InstructionData data)
        {
            if (bot.Pos.BlockY < 0) PlayerBot.Remove(bot);

            BlockID block = bot.level.GetBlock((ushort)bot.Pos.BlockX, ((ushort)(bot.Pos.BlockY - 2)), (ushort)bot.Pos.BlockZ);

            //Console.WriteLine(block + " block");
            if (block != Block.Air) return true;
            Metadata meta = (Metadata)data.Metadata;
            //Console.WriteLine(bot.Pos.X + " x " + bot.Pos.Y + " y" + bot.Pos.Z + " z" );
            bot.Pos = new Position(bot.Pos.X, (bot.Pos.Y - 16), bot.Pos.Z);

            return true;
        }

        public override InstructionData Parse(string[] args)
        {
            InstructionData data =
                default(InstructionData);
            data.Metadata = new Metadata();
            return data;
        }

        public void Output(Player p, string[] args, StreamWriter w)
        {
            if (args.Length > 3)
            {
                w.WriteLine(Name + " " + ushort.Parse(args[3]));
            }
            else
            {
                w.WriteLine(Name);
            }
        }

        public override string[] Help { get { return help; } }
        static string[] help = new string[] {
            "%T/BotAI add [name] drop",
            "%HCauses the bot to fall.",
        };
    }

    #endregion

    #region Hostile AI

    /* 
        Current AI behaviour:

        -   Chase player if within 12 block range
        -   Hit player if too close
        -   Assign movement speed based on mob model
        -   Explode if mob is a creeper


        -   50% chance to stand still (moving when 0-2, still when 3-5)
        -   If not moving, wait for waitTime duration before executing next task
        -   Choose random coord within 8x8 block radius of player and try to go to it
        -   Do action for walkTime duration

     */

    sealed class HostileInstruction : BotInstruction
    {
        public HostileInstruction() { Name = "hostile"; }

        static bool MoveTowards(PlayerBot bot, Player p, Metadata meta)
        {
            if (p == null) return false;

            int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;
            bot.TargetPos = p.Pos;
            bot.movement = true;

            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            dir = Vec3F32.Normalise(dir);
            Orientation rot = bot.Rot;
            DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);

            MobAI.SetDirectionalSpeeds(bot);

            dx = Math.Abs(dx); dy = Math.Abs(dy); dz = Math.Abs(dz);

            if (bot.Model == "creeper")
            {
                if (dx < (3 * 32) && dz < (3 * 32))
                {
                    if (meta.explodeTime == 0)
                    {
                        meta.explodeTime = 10;
                    }
                }
                else meta.explodeTime = 0;
            }

            else
            {
                // Check to see if positions collide
                AABB playerBB = p.ModelBB.OffsetPosition(p.Pos);
                AABB botBB = bot.ModelBB.OffsetPosition(bot.Pos);

                int dist = (int)(0.875f * 32);

                bool inRange = ((long)dx * dx + (long)dz * dz <= dist * dist) &&
                    (botBB.Min.Y <= playerBB.Max.Y && playerBB.Min.Y <= botBB.Max.Y);

                if (inRange) HitPlayer(bot, p, rot);
            }

            bot.Rot = rot;

            return dx <= 8 && dy <= 16 && dz <= 8;
        }

        public static void HitPlayer(PlayerBot bot, Player p, Orientation rot)
        {
            // Send player backwards if hit
            // Code "borrowed" from PvP plugin

            // If we are very close to a player, switch from trying to look
            // at them to just facing the opposite direction to them

            rot.RotY = (byte)(p.Rot.RotY + 128);
            bot.Rot = rot;

            int srcHeight = ModelInfo.CalcEyeHeight(bot);
            int dstHeight = ModelInfo.CalcEyeHeight(p);
            int dx2 = bot.Pos.X - p.Pos.X, dy2 = (bot.Pos.Y + srcHeight) - (p.Pos.Y + dstHeight), dz2 = bot.Pos.Z - p.Pos.Z;

            Vec3F32 dir2 = new Vec3F32(dx2, dy2, dz2);

            if (dir2.Length > 0) dir2 = Vec3F32.Normalise(dir2);

            float mult = 1 / ModelInfo.GetRawScale(p.Model);
            float plScale = ModelInfo.GetRawScale(p.Model);

            float VelocityY = 1.0117f * mult;

            if (dir2.Length <= 0) VelocityY = 0;

            if (p.Supports(CpeExt.VelocityControl))
            {
                // Intensity of force is in part determined by model scale
                p.Send(Packet.VelocityControl((-dir2.X * mult) * 0.57f, VelocityY, (-dir2.Z * mult) * 0.57f, 0, 1, 0));
            }

            int damage = 0;

            if (bot.Model == "bee") damage = 2;
            if (bot.Model == "blaze") damage = 4;
            if (bot.Model == "creeper") damage = 23; // 22.5 but either way, still kills the player
            if (bot.Model == "enderman") damage = 5; // 4.5
            if (bot.Model == "panda") damage = 4;
            if (bot.Model == "skeleton") damage = 2; // 3-5 damage when shot with bow
            if (bot.Model == "spider") damage = 2;
            if (bot.Model == "witherskeleton") damage = 5;
            if (bot.Model == "wither") damage = 15; // 34 with explosion
            if (bot.Model == "zombie") damage = 3; // 2.5

            // Update player's health
            p.Extras["SURVIVAL_HEALTH"] = p.Extras.GetInt("SURVIVAL_HEALTH") - damage;

            if (p.Extras.GetInt("SURVIVAL_HEALTH") <= 0) p.HandleDeath(Block.Orange);


        }

        private readonly Random _random = new Random();

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public void DoStuff(PlayerBot bot, Metadata meta)
        {
            int stillChance = RandomNumber(0, 5); // Chance for the NPC to stand still
            int walkTime = RandomNumber(4, 8) * 5; // Time in milliseconds to execute a task
            int waitTime = RandomNumber(2, 5) * 5; // Time in milliseconds to wait before executing the next task

            int dx = RandomNumber(bot.Pos.X - (8 * 32), bot.Pos.X + (8 * 32)); // Random X location on the map within a 8x8 radius of the bot for the it to walk towards.
            int dz = RandomNumber(bot.Pos.Z - (8 * 32), bot.Pos.Z + (8 * 32)); // Random Z location on the map within a 8x8 radius of the bot for the it to walk towards.

            if (stillChance > 2)
            {
                meta.walkTime = walkTime;
            }

            else
            {
                Coords target;
                target.X = dx;
                target.Y = bot.Pos.Y;
                target.Z = dz;
                target.RotX = bot.Rot.RotX;
                target.RotY = bot.Rot.RotY;
                bot.TargetPos = new Position(target.X, target.Y, target.Z);

                bot.movement = true;

                if (bot.Pos.BlockX == bot.TargetPos.BlockX && bot.Pos.BlockZ == bot.TargetPos.BlockZ)
                {
                    bot.SetYawPitch(target.RotX, target.RotY);
                    bot.movement = false;
                }

                bot.AdvanceRotation();

                MobAI.FaceTowards(bot);

                meta.walkTime = walkTime;
                bot.movement = false;
                meta.waitTime = waitTime;
            }
        }

        public override bool Execute(PlayerBot bot, InstructionData data)
        {
            Metadata meta = (Metadata)data.Metadata;

            if (bot.Model == "skeleton" || bot.Model == "creeper") bot.movementSpeed = (int)Math.Round(3m * (short)97 / 100m);
            if (bot.Model == "zombie") bot.movementSpeed = (int)Math.Round(3m * (short)94 / 100m);

            if (bot.movementSpeed == 0) bot.movementSpeed = 1;


            int search = 16;
            // If specified, use user specified search distance instead of default

            if (meta.search != 16 && meta.search > 0) search = meta.search;

            else
            {
                if (bot.Model == "zombie") search = 35;
            }


            Player closest = MobAI.ClosestPlayer(bot, search);

            if (closest == null)
            {
                if (bot.Model == "creeper")
                {
                    meta.explodeTime = 0;
                }

                if (meta.walkTime > 0)
                {
                    meta.walkTime--;
                    bot.movement = true;
                    return true;
                }

                if (meta.waitTime > 0)
                {
                    meta.waitTime--;
                    return true;
                }

                DoStuff(bot, meta);

                bot.movement = false;
                bot.NextInstruction();
            }

            else
            {
                if (bot.Model == "creeper")
                {
                    if (meta.explodeTime > 0)
                    {
                        meta.explodeTime--;

                        if (meta.explodeTime == 1)
                        {
                            if (closest.level.physics > 1 && closest.level.physics != 5) closest.level.MakeExplosion((ushort)(bot.Pos.X / 32), (ushort)(bot.Pos.Y / 32), (ushort)(bot.Pos.Z / 32), 0);
                            Command.Find("Effect").Use(closest, "explosion " + (bot.Pos.X / 32) + " " + (bot.Pos.Y / 32) + " " + (bot.Pos.Z / 32) + " 0 0 0 true");

                            int distanceX = closest.Pos.X - bot.Pos.X, distanceY = closest.Pos.Y - bot.Pos.Y, distanceZ = closest.Pos.Z - bot.Pos.Z;
                            int distance = (distanceX + distanceZ) / 32;

                            // Do damage to the player if player is within a 3 block radius

                            if (distance < 3)
                            {
                                closest.Extras["SURVIVAL_HEALTH"] = closest.Extras.GetInt("SURVIVAL_HEALTH") - 23;

                                if (closest.Extras.GetInt("SURVIVAL_HEALTH") <= 0)
                                {
                                    closest.HandleDeath(Block.Orange);
                                }

                                else
                                {
                                    Orientation rot = bot.Rot;
                                    HitPlayer(bot, closest, rot);
                                }
                            }

                            meta.explodeTime = 0;
                            PlayerBot.Remove(bot);
                            return true;
                        }

                        bot.movement = true;
                        return true;
                    }
                }

                if (bot.Model == "skeleton")
                {
                    if (meta.bowCooldown >= 1)
                    {
                        meta.bowCooldown--;
                    }

                    if (meta.bowCooldown == 0)
                    {
                        meta.bowCooldown = 33;
                        Bow.Enable(bot, closest);
                    }

                    bot.movement = true;
                }
            }

            bool overlapsPlayer = MoveTowards(bot, closest, meta);
            if (overlapsPlayer && closest != null) { bot.NextInstruction(); return false; }

            return true;
        }

        public override InstructionData Parse(string[] args)
        {
            InstructionData data = default(InstructionData);
            data.Metadata = new Metadata();

            Metadata meta = (Metadata)data.Metadata;

            if (args.Length > 1)
            {
                meta.search = int.Parse(args[1]);
            }
            return data;
        }

        public void Output(Player p, string[] args, StreamWriter w)
        {
            if (args.Length > 3)
            {
                w.WriteLine(Name + " " + ushort.Parse(args[3]));
            }
            else
            {
                w.WriteLine(Name);
            }
        }

        struct Coords
        {
            public int X, Y, Z;
            public byte RotX, RotY;
        }

        public override string[] Help { get { return help; } }
        static string[] help = new string[] {
            "%T/BotAI add [name] hostile",
            "%HCauses the bot behave as a hostile mob.",
        };
    }

    #endregion

    #region Roam AI

    /* 
        Current AI behaviour:

        -   50% chance to stand still (moving when 0-2, still when 3-5)
        -   If not moving, wait for waitTime duration before executing next task
        -   Choose random coord within 8x8 block radius of player and try to go to it
        -   Do action for walkTime duration

     */

    sealed class RoamInstruction : BotInstruction
    {
        public RoamInstruction() { Name = "roam"; }

        private readonly Random _random = new Random();

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public void DoStuff(PlayerBot bot, Metadata meta)
        {
            int stillChance = RandomNumber(0, 5); // Chance for the NPC to stand still
            int walkTime = RandomNumber(4, 8) * 5; // Time in milliseconds to execute a task
            int waitTime = RandomNumber(2, 4) * 5; // Time in milliseconds to wait before executing the next task
            int lookChance = RandomNumber(0, 2); // Chance for the NPC to look at the player
            int lookTime = RandomNumber(2, 5) * 5; // Time in milliseconds to look at the player for
            int eatGrassChance = RandomNumber(0, 100); // Chance for the NPC to eat grass

            int dx = RandomNumber(bot.Pos.X - (8 * 32), bot.Pos.X + (8 * 32)); // Random X location on the map within a 8x8 radius of the bot for the it to walk towards.
            int dz = RandomNumber(bot.Pos.Z - (8 * 32), bot.Pos.Z + (8 * 32)); // Random Z location on the map within a 8x8 radius of the bot for the it to walk towards.

            if (eatGrassChance <= 3)
            {
                // Only eat grass if the block underneath the NPC is grass
                BlockID floor = bot.level.GetBlock((ushort)(bot.Pos.X / 32), (ushort)((bot.Pos.Y / 32) - 2), (ushort)(bot.Pos.Z / 32));

                if (floor == Block.Grass)
                {
                    bot.level.UpdateBlock(Player.Console, (ushort)(bot.Pos.X / 32), (ushort)((bot.Pos.Y / 32) - 2), (ushort)(bot.Pos.Z / 32), Block.Dirt);

                    if (bot.Model.CaselessEq("sheep_nofur"))
                    {
                        bot.UpdateModel("sheep");
                        BotsFile.Save(bot.level);
                    }
                }
            }

            if (stillChance > 2)
            {
                meta.walkTime = walkTime;

                Player p = MobAI.ClosestPlayer(bot, 5);

                if (p != null && lookChance == 1)
                {
                    int srcHeight = ModelInfo.CalcEyeHeight(p);
                    int dstHeight = ModelInfo.CalcEyeHeight(bot);

                    // Look at player
                    int lx = p.Pos.X - bot.Pos.X, ly = (p.Pos.Y + srcHeight) - (bot.Pos.Y + dstHeight), lz = p.Pos.Z - bot.Pos.Z;
                    Vec3F32 dir = new Vec3F32(lx, ly, lz);
                    dir = Vec3F32.Normalise(dir);

                    Orientation rot = bot.Rot;
                    DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);
                    bot.Rot = rot;
                    meta.lookTime = lookTime;
                    meta.chasing = p;
                    //p.Message("looking at " + p.truename + " for " + lookTime);
                }

                else
                {
                    Coords target;
                    target.X = dx;
                    target.Y = bot.Pos.Y;
                    target.Z = dz;
                    target.RotX = bot.Rot.RotX;
                    target.RotY = bot.Rot.RotY;
                    bot.TargetPos = new Position(target.X, target.Y, target.Z);

                    bot.movement = true;

                    if (bot.Pos.BlockX == bot.TargetPos.BlockX && bot.Pos.BlockZ == bot.TargetPos.BlockZ)
                    {
                        bot.SetYawPitch(target.RotX, bot.Rot.RotY);
                        bot.movement = false;
                    }

                    bot.AdvanceRotation();

                    MobAI.FaceTowards(bot);

                    meta.walkTime = walkTime;
                    bot.movement = false;
                    meta.waitTime = waitTime;
                }
            }
        }

        public override bool Execute(PlayerBot bot, InstructionData data)
        {
            Metadata meta = (Metadata)data.Metadata;

            if (meta.walkTime > 0)
            {
                meta.walkTime--;
                bot.movement = true;
                return true;
            }

            if (meta.waitTime > 0)
            {
                Player p = MobAI.ClosestPlayer(bot, 5);

                if (p != null)
                {
                    int lookChance = RandomNumber(0, 2); // Chance for the NPC to look at the player
                    int lookTime = RandomNumber(2, 5) * 5; // Time in milliseconds to look at the player for

                    if (lookChance == 1)
                    {
                        int srcHeight = ModelInfo.CalcEyeHeight(p);
                        int dstHeight = ModelInfo.CalcEyeHeight(bot);

                        // Look at player
                        int lx = p.Pos.X - bot.Pos.X, ly = (p.Pos.Y + srcHeight) - (bot.Pos.Y + dstHeight), lz = p.Pos.Z - bot.Pos.Z;
                        Vec3F32 dir = new Vec3F32(lx, ly, lz);
                        dir = Vec3F32.Normalise(dir);

                        Orientation rot = bot.Rot;
                        DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);
                        bot.Rot = rot;
                        meta.lookTime = lookTime;
                        meta.chasing = p;
                        //p.Message("interrupted, looking at " + p.truename + " for " + lookTime);
                    }

                    meta.waitTime = 0;
                    return true;
                }

                meta.waitTime--;
                return true;
            }

            if (meta.lookTime > 0)
            {
                if (meta.chasing != null)
                {
                    Player p = meta.chasing;

                    int srcHeight = ModelInfo.CalcEyeHeight(p);
                    int dstHeight = ModelInfo.CalcEyeHeight(bot);

                    // Look at player
                    int lx = p.Pos.X - bot.Pos.X, ly = (p.Pos.Y + srcHeight) - (bot.Pos.Y + dstHeight), lz = p.Pos.Z - bot.Pos.Z;
                    Vec3F32 dir = new Vec3F32(lx, ly, lz);
                    dir = Vec3F32.Normalise(dir);

                    Orientation rot = bot.Rot;
                    DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);
                    bot.Rot = rot;

                    //p.Message("still looking at " + p.truename + " for " + meta.lookTime);
                }

                meta.lookTime--;

                if (meta.lookTime <= 0)
                {
                    if (meta.chasing != null) //meta.chasing.Message("finished looking");
                        bot.NextInstruction();
                }

                else return true;
            }

            DoStuff(bot, meta);

            bot.movement = false;
            bot.NextInstruction();
            return true;
        }

        public override InstructionData Parse(string[] args)
        {
            InstructionData data =
                default(InstructionData);
            data.Metadata = new Metadata();
            return data;
        }

        public void Output(Player p, string[] args, StreamWriter w)
        {
            if (args.Length > 3)
            {
                w.WriteLine(Name + " " + ushort.Parse(args[3]));
            }
            else
            {
                w.WriteLine(Name);
            }
        }

        struct Coords
        {
            public int X, Y, Z;
            public byte RotX, RotY;
        }

        public override string[] Help { get { return help; } }
        static string[] help = new string[] {
            "%T/BotAI add [name] roam",
            "%HCauses the bot behave freely.",
        };
    }

    #endregion

    #region Run AI

    /* 
        Current AI behaviour:

        -   Chase player if within 10 block range
     */

    sealed class RunInstruction : BotInstruction
    {
        public RunInstruction() { Name = "run"; }

        static int lastY = 0;

        static bool MoveTowards(PlayerBot bot, Player p, Metadata meta)
        {
            if (p == null) return false;
            //int dist = (int)(0.875 * 32);

            int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;

            MobAI.SetDirectionalSpeeds(bot);

            Vec3F32 pDir = DirUtils.GetDirVector(p.Rot.RotY, 0);

            bot.TargetPos.X = bot.Pos.X + (int)(pDir.X * 100);
            bot.TargetPos.Z = bot.Pos.Z + (int)(pDir.Z * 100);

            bot.movement = true;

            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            dir = Vec3F32.Normalise(dir);
            Orientation rot = bot.Rot;
            DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);

            dx = Math.Abs(dx); dy = Math.Abs(dy); dz = Math.Abs(dz);
            //if (InRange(p, bot, dist)) p.Message("%cInfect");

            bot.Rot = rot;

            return dx <= 4 && dy <= 8 && dz <= 4;
        }

        private readonly Random _random = new Random();

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public override bool Execute(PlayerBot bot, InstructionData data)
        {
            Metadata meta = (Metadata)data.Metadata;

            Player closest = MobAI.ClosestPlayer(bot, 20);

            if (closest == null)
            {
                bot.movement = false;
                bot.NextInstruction();
            }

            bool overlapsPlayer = MoveTowards(bot, closest, meta);
            if (overlapsPlayer && closest != null) { bot.NextInstruction(); return false; }


            return true;
        }

        public override InstructionData Parse(string[] args)
        {
            InstructionData data =
                default(InstructionData);
            data.Metadata = new Metadata();
            return data;
        }

        public void Output(Player p, string[] args, StreamWriter w)
        {
            if (args.Length > 3)
            {
                w.WriteLine(Name + " " + ushort.Parse(args[3]));
            }
            else
            {
                w.WriteLine(Name);
            }
        }

        struct Coords
        {
            public int X, Y, Z;
            public byte RotX, RotY;
        }

        public override string[] Help { get { return help; } }
        static string[] help = new string[] {
            "%T/BotAI add [name] run",
            "%HCauses the bot to try and spleef you.",
        };
    }

    #endregion

    #region Smart AI

    /* 
        Current AI behaviour:

        -   Chase player if within 12 block range
        -   Let player know if they are within the hitbox (simulative of New Blood's 0.875 hitbox)
        -   Adjust speed to be ~40% slower when running on diagonals to simulate player speed (bot speed is 1.4 on diagonals since cardinally lacking)
     */

    sealed class SmartInstruction : BotInstruction
    {
        public SmartInstruction() { Name = "smart"; }

        public static bool InRange(Player a, PlayerBot b, int dist)
        {
            int dx = Math.Abs(a.Pos.X - b.Pos.X);
            int dy = Math.Abs(a.Pos.Y - b.Pos.Y);
            int dz = Math.Abs(a.Pos.Z - b.Pos.Z);

            // Actually checking if positions collide
            AABB aliveBB = a.ModelBB.OffsetPosition(a.Pos);
            AABB killerBB = b.ModelBB.OffsetPosition(b.Pos);
            killerBB.Max.Y -= 8; // Adjust because zombie head is a bit lower
            bool inRange = ((long)dx * dx + (long)dz * dz <= dist * dist) &&
                (killerBB.Min.Y <= aliveBB.Max.Y && aliveBB.Min.Y <= killerBB.Max.Y);

            return inRange;
        }

        static bool MoveTowards(PlayerBot bot, Player p, Metadata meta)
        {
            if (p == null) return false;
            int dist = (int)(0.875 * 32);

            int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;

            // Update target pos every x seconds to simulate ping
            // 10 ticks per second (10 : 1000 or 1 : 100)

            meta.tickDelay++;

            if (meta.tickDelay == meta.randomTick)
            {
                p.Message("%dUpdate target");
                bot.TargetPos = p.Pos;
                meta.tickDelay = 0;
            }

            bot.movement = true;

            MobAI.SetDirectionalSpeeds(bot);

            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            dir = Vec3F32.Normalise(dir);
            Orientation rot = bot.Rot;
            DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);

            bot.Rot = rot;

            if (InRange(p, bot, dist)) p.Message("%cInfect");

            return dx <= 8 && dy <= 16 && dz <= 8;
        }

        private readonly Random _random = new Random();

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public override bool Execute(PlayerBot bot, InstructionData data)
        {
            Metadata meta = (Metadata)data.Metadata;

            Player closest = MobAI.ClosestPlayer(bot, 30);

            if (closest == null)
            {
                bot.movement = false;
                bot.NextInstruction();
            }

            bool overlapsPlayer = MoveTowards(bot, closest, meta);
            if (overlapsPlayer && closest != null) { bot.NextInstruction(); return false; }

            return true;
        }

        public override InstructionData Parse(string[] args)
        {
            InstructionData data =
                default(InstructionData);
            data.Metadata = new Metadata();
            return data;
        }

        public void Output(Player p, string[] args, StreamWriter w)
        {
            if (args.Length > 3)
            {
                w.WriteLine(Name + " " + ushort.Parse(args[3]));
            }
            else
            {
                w.WriteLine(Name);
            }
        }

        struct Coords
        {
            public int X, Y, Z;
            public byte RotX, RotY;
        }

        public override string[] Help { get { return help; } }
        static string[] help = new string[] {
            "%T/BotAI add [name] smart",
            "%HCauses the bot behave as a smart mob.",
        };
    }

    #endregion

    #region Hostile AI

    /* 
        Current AI behaviour:

        -   Chase player if within 12 block range
        -   Hit player if too close
        -   Assign movement speed based on mob model
        -   Explode if mob is a creeper


        -   50% chance to stand still (moving when 0-2, still when 3-5)
        -   If not moving, wait for waitTime duration before executing next task
        -   Choose random coord within 8x8 block radius of player and try to go to it
        -   Do action for walkTime duration

     */

    sealed class SmartHuntInstruction : BotInstruction
    {
        public SmartHuntInstruction() { Name = "smarthunt"; }

        static bool MoveTowards(PlayerBot bot, Player p, Metadata meta)
        {
            if (p == null) return false;

            int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;

            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            dir = Vec3F32.Normalise(dir);
            Orientation rot = bot.Rot;
            DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);

            PathFind(bot, p);

            bot.Rot = rot;

            MobAI.SetDirectionalSpeeds(bot);

            dx = Math.Abs(dx); dy = Math.Abs(dy); dz = Math.Abs(dz);

            // Check to see if positions collide
            AABB playerBB = p.ModelBB.OffsetPosition(p.Pos);
            AABB botBB = bot.ModelBB.OffsetPosition(bot.Pos);

            int dist = (int)(0.875f * 32);

            bool inRange = ((long)dx * dx + (long)dz * dz <= dist * dist) &&
                (botBB.Min.Y <= playerBB.Max.Y && playerBB.Min.Y <= botBB.Max.Y);

            //if (inRange) HitPlayer(bot, p, rot);

            return dx <= 8 && dy <= 16 && dz <= 8;
        }

        static void PathFind(PlayerBot bot, Player p)
        {
            int delay = 0;

            var start = new Location { X = p.Pos.X / 32, Z = p.Pos.Z / 32 };
            var target = new Location { X = bot.Pos.X / 32, Z = bot.Pos.Z / 32 };

            // Algorithm  
            Location current = null;
            var openList = new List<Location>();
            var closedList = new List<Location>();
            int g = 0;

            // Start by adding the original position to the open list  
            openList.Add(start);

            while (openList.Count > 0)
            {
                // Get the square with the lowest F score  
                var lowest = openList.Min(l => l.F);
                current = openList.First(l => l.F == lowest);

                // Add the current square to the closed list  
                closedList.Add(current);

                p.SendBlockchange((ushort)current.X, 0, (ushort)current.Z, Block.Yellow);
                Thread.Sleep(10);

                // Remove it from the open list  
                openList.Remove(current);

                // If we added the destination to the closed list, we've found a path  
                if (closedList.FirstOrDefault(l => l.X == target.X && l.Z == target.Z) != null)
                    break;

                var adjacentSquares = GetWalkableAdjacentSquares(p, current.X, current.Z, openList);
                g = current.G + 1;

                foreach (var adjacentSquare in adjacentSquares)
                {
                    // If this adjacent square is already in the closed list, ignore it  
                    if (closedList.FirstOrDefault(l => l.X == adjacentSquare.X
                        && l.Z == adjacentSquare.Z) != null)
                        continue;

                    // If it's not in the open list...  
                    if (openList.FirstOrDefault(l => l.X == adjacentSquare.X
                        && l.Z == adjacentSquare.Z) == null)
                    {
                        // Compute its score, set the parent  
                        adjacentSquare.G = g;
                        adjacentSquare.H = ComputeHScore(adjacentSquare.X, adjacentSquare.Z, target.X, target.Z);
                        adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                        adjacentSquare.Parent = current;

                        // And add it to the open list  
                        openList.Insert(0, adjacentSquare);
                    }
                    else
                    {
                        // Test if using the current G score makes the adjacent square's F score  
                        // Lower, if yes update the parent because it means it's a better path  
                        if (g + adjacentSquare.H < adjacentSquare.F)
                        {
                            adjacentSquare.G = g;
                            adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                            adjacentSquare.Parent = current;
                        }
                    }
                }
            }

            Location end = current;

            // Assume path was found; let's show it  
            while (current != null)
            {
                p.SendBlockchange((ushort)current.X, 0, (ushort)current.Z, Block.Green);

                current = current.Parent;
                Thread.Sleep(100);
                bot.TargetPos = new Position(current.X * 32, p.Pos.Y, current.Z * 32);
                bot.movement = true;
            }

            if (end != null)
            {
                //Console.WriteLine("Path : {0}", end.G);
            }
        }

        static List<Location> GetWalkableAdjacentSquares(Player p, int x, int z, List<Location> openList)
        {
            List<Location> list = new List<Location>();

            BlockID cur = p.level.GetBlock((ushort)x, 0, (ushort)(z - 1));

            if (cur == Block.Air || cur == Block.Water || cur == Block.Lava)
            {
                Location node = openList.Find(l => l.X == x && l.Z == z - 1);
                if (node == null) list.Add(new Location() { X = x, Z = z - 1 });
                else list.Add(node);
            }

            cur = p.level.GetBlock((ushort)x, 0, (ushort)(z + 1));

            if (cur == Block.Air || cur == Block.Water || cur == Block.Lava)
            {
                Location node = openList.Find(l => l.X == x && l.Z == z + 1);
                if (node == null) list.Add(new Location() { X = x, Z = z + 1 });
                else list.Add(node);
            }

            cur = p.level.GetBlock((ushort)(x - 1), 0, (ushort)z);

            if (cur == Block.Air || cur == Block.Water || cur == Block.Lava)
            {
                Location node = openList.Find(l => l.X == x - 1 && l.Z == z);
                if (node == null) list.Add(new Location() { X = x - 1, Z = z });
                else list.Add(node);
            }

            cur = p.level.GetBlock((ushort)(x + 1), 0, (ushort)z);

            if (cur == Block.Air || cur == Block.Water || cur == Block.Lava)
            {
                Location node = openList.Find(l => l.X == x + 1 && l.Z == z);
                if (node == null) list.Add(new Location() { X = x + 1, Z = z });
                else list.Add(node);
            }

            return list;
        }

        static int ComputeHScore(int x, int z, int targetX, int targetZ)
        {
            return Math.Abs(targetX - x) + Math.Abs(targetZ - z);
        }

        public override bool Execute(PlayerBot bot, InstructionData data)
        {
            Metadata meta = (Metadata)data.Metadata;

            if (bot.Model == "skeleton" || bot.Model == "creeper") bot.movementSpeed = (int)Math.Round(3m * (short)97 / 100m);
            if (bot.Model == "zombie") bot.movementSpeed = (int)Math.Round(3m * (short)94 / 100m);

            if (bot.movementSpeed == 0) bot.movementSpeed = 1;

            int search = 16;
            // If user specified a search distance, use that instead of default

            if (meta.search != 16 && meta.search > 0) search = meta.search;

            Player closest = MobAI.ClosestPlayer(bot, search);

            if (closest == null)
            {
                bot.movement = false;
                bot.NextInstruction();
            }

            bool overlapsPlayer = MoveTowards(bot, closest, meta);
            if (overlapsPlayer && closest != null) { bot.NextInstruction(); return false; }

            return true;
        }

        public override InstructionData Parse(string[] args)
        {
            InstructionData data = default(InstructionData);
            data.Metadata = new Metadata();

            Metadata meta = (Metadata)data.Metadata;

            if (args.Length > 1)
            {
                meta.search = int.Parse(args[1]);
            }
            return data;
        }

        public void Output(Player p, string[] args, StreamWriter w)
        {
            if (args.Length > 3)
            {
                w.WriteLine(Name + " " + ushort.Parse(args[3]));
            }
            else
            {
                w.WriteLine(Name);
            }
        }

        struct Coords
        {
            public int X, Y, Z;
            public byte RotX, RotY;
        }

        public override string[] Help { get { return help; } }
        static string[] help = new string[] {
            "%T/BotAI add [name] smarthunt",
            "%HCauses the bot to use 2D A* pathfinding when hunting players.",
        };
    }

    #endregion

    #region Spleef AI

    /* 
        Current AI behaviour:

        -   Chase player if within 10 block range
        -   Delete block below the player if within 5 block range (default reach distance)
        -   33% chance to delete blocks (simulates click speed since nobody clicks at consistent speeds)

        Planned behaviour:

        -   Nobody consistently breaks 5 blocks away, let's make it choose from 3-5        
     */

    sealed class SpleefInstruction : BotInstruction
    {
        public SpleefInstruction() { Name = "spleef"; }

        static int lastY = 0;

        static bool MoveTowards(PlayerBot bot, Player p, Metadata meta)
        {
            if (p == null) return false;
            //int dist = (int)(0.875 * 32);

            int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;

            bot.TargetPos = p.Pos;
            bot.movement = true;

            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            dir = Vec3F32.Normalise(dir);
            Orientation rot = bot.Rot;
            DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);

            MobAI.SetDirectionalSpeeds(bot);

            dx = Math.Abs(dx); dy = Math.Abs(dy); dz = Math.Abs(dz);
            //if (InRange(p, bot, dist)) p.Message("%cInfect");

            bot.Rot = rot;

            if (dx < (6 * 32) && dz < (6 * 32)) // Check if player is 6 blocks away from player
            {
                Random rnd = new Random();
                // This code serves as a sort of 'CPS mechanism' to ensure that the bot does not perfectly delete every single block
                int chance = rnd.Next(3); // 80% chance of deleting the block

                // 1-3 blocks away from the player since nobody consistently deletes 5 blocks away

                int rangeX = rnd.Next(1, 3);
                int rangeZ = rnd.Next(1, 3);

                int distanceX = p.Pos.X - bot.Pos.X, distanceY = p.Pos.Y - bot.Pos.Y, distanceZ = p.Pos.Z - bot.Pos.Z;
                int distance = (distanceX + distanceZ) / 32;

                int speed = rnd.Next(1, 3);

                p.Message("dist " + distance + " sp " + speed);


                if (distance < 0)
                {
                    // Subtract
                    bot.level.UpdateBlock(Player.Console, (ushort)((p.Pos.X / 32) - rangeX), (ushort)((p.Pos.Y / 32) - 2), (ushort)((p.Pos.Z / 32) - rangeZ), Block.Air);

                    if ((p.Pos.Y / 32) > lastY) bot.level.UpdateBlock(Player.Console, (ushort)((p.Pos.X / 32) - rangeX), (ushort)((p.Pos.Y / 32) - 3), (ushort)((p.Pos.Z / 32) - rangeZ), Block.Air);

                    if (speed > 1)
                    {
                        bot.level.UpdateBlock(Player.Console, (ushort)((p.Pos.X / 32) - rangeX - 1), (ushort)((p.Pos.Y / 32) - 2), (ushort)((p.Pos.Z / 32) - rangeZ - 1), Block.Air);
                        if (speed > 2) bot.level.UpdateBlock(Player.Console, (ushort)((p.Pos.X / 32) - rangeX - 2), (ushort)((p.Pos.Y / 32) - 2), (ushort)((p.Pos.Z / 32) - rangeZ - 2), Block.Air);
                    }
                }

                else
                {
                    // Add
                    bot.level.UpdateBlock(Player.Console, (ushort)((p.Pos.X / 32) + rangeX), (ushort)((p.Pos.Y / 32) - 2), (ushort)((p.Pos.Z / 32) + rangeZ), Block.Air);
                    if (speed > 1)
                    {
                        bot.level.UpdateBlock(Player.Console, (ushort)((p.Pos.X / 32) + rangeX + 1), (ushort)((p.Pos.Y / 32) - 2), (ushort)((p.Pos.Z / 32) + rangeZ + 1), Block.Air);
                        if (speed > 2) bot.level.UpdateBlock(Player.Console, (ushort)((p.Pos.X / 32) + rangeX + 2), (ushort)((p.Pos.Y / 32) - 2), (ushort)((p.Pos.Z / 32) + rangeZ + 2), Block.Air);
                    }

                    if ((p.Pos.Y / 32) > lastY) bot.level.UpdateBlock(Player.Console, (ushort)((p.Pos.X / 32) + rangeX), (ushort)((p.Pos.Y / 32) - 3), (ushort)((p.Pos.Z / 32) + rangeZ), Block.Air);
                }

                lastY = (p.Pos.Y / 32);
            }

            return dx <= 8 && dy <= 16 && dz <= 8;
        }

        private readonly Random _random = new Random();

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public override bool Execute(PlayerBot bot, InstructionData data)
        {
            Metadata meta = (Metadata)data.Metadata;

            Player closest = MobAI.ClosestPlayer(bot, 20);

            if (closest == null)
            {
                bot.movement = false;
                bot.NextInstruction();
            }

            bool overlapsPlayer = MoveTowards(bot, closest, meta);
            if (overlapsPlayer && closest != null) { bot.NextInstruction(); return false; }


            return true;
        }

        public override InstructionData Parse(string[] args)
        {
            InstructionData data =
                default(InstructionData);
            data.Metadata = new Metadata();
            return data;
        }

        public void Output(Player p, string[] args, StreamWriter w)
        {
            if (args.Length > 3)
            {
                w.WriteLine(Name + " " + ushort.Parse(args[3]));
            }
            else
            {
                w.WriteLine(Name);
            }
        }

        struct Coords
        {
            public int X, Y, Z;
            public byte RotX, RotY;
        }

        public override string[] Help { get { return help; } }
        static string[] help = new string[] {
            "%T/BotAI add [name] spleef",
            "%HCauses the bot to try and spleef you.",
        };
    }

    #endregion

    #region TNT AI

    /* 
        Current AI behaviour:

        -   Chase player if within 10 block range
        -   Delete block below the player if within 5 block range (default reach distance)
        -   33% chance to delete blocks (simulates click speed since nobody clicks at consistent speeds)

        Planned behaviour:

        -   Nobody consistently breaks 5 blocks away, let's make it choose from 3-5        
     */

    sealed class TNTInstruction : BotInstruction
    {
        public TNTInstruction() { Name = "tnt"; }

        static int lastY = 0;

        static bool MoveTowards(PlayerBot bot, Player p, Metadata meta)
        {
            if (p == null) return false;
            //int dist = (int)(0.875 * 32);

            int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;

            bot.TargetPos = p.Pos;
            bot.movement = true;

            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            dir = Vec3F32.Normalise(dir);
            Orientation rot = bot.Rot;
            DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);

            MobAI.SetDirectionalSpeeds(bot);

            dx = Math.Abs(dx); dy = Math.Abs(dy); dz = Math.Abs(dz);
            //if (InRange(p, bot, dist)) p.Message("%cInfect");

            bot.Rot = rot;

            if (dx < (5 * 32) && dz < (5 * 32)) // 5 block reach
            {
                Random rnd = new Random();
                // This code serves as a sort of 'CPS mechanism' to ensure that the bot does not perfectly delete every single block
                int chance = rnd.Next(0, 4); // 33% chance of deleting the block
                if (chance < 3)
                {
                    bot.level.UpdateBlock(Player.Console, (ushort)(p.Pos.X / 32), (ushort)((p.Pos.Y / 32) - 2), (ushort)(p.Pos.Z / 32), Block.Air);

                    if ((p.Pos.Y / 32) > lastY) bot.level.UpdateBlock(Player.Console, (ushort)(p.Pos.X / 32), (ushort)((p.Pos.Y / 32) - 3), (ushort)(p.Pos.Z / 32), Block.Air);
                }

                lastY = (p.Pos.Y / 32);
            }

            return dx <= 8 && dy <= 16 && dz <= 8;
        }

        private readonly Random _random = new Random();

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public override bool Execute(PlayerBot bot, InstructionData data)
        {
            Metadata meta = (Metadata)data.Metadata;

            Player closest = MobAI.ClosestPlayer(bot, 10);

            if (closest == null)
            {
                bot.movement = false;
                bot.NextInstruction();
            }

            bool overlapsPlayer = MoveTowards(bot, closest, meta);
            if (overlapsPlayer && closest != null) { bot.NextInstruction(); return false; }


            return true;
        }

        public override InstructionData Parse(string[] args)
        {
            InstructionData data =
                default(InstructionData);
            data.Metadata = new Metadata();
            return data;
        }

        public void Output(Player p, string[] args, StreamWriter w)
        {
            if (args.Length > 3)
            {
                w.WriteLine(Name + " " + ushort.Parse(args[3]));
            }
            else
            {
                w.WriteLine(Name);
            }
        }

        struct Coords
        {
            public int X, Y, Z;
            public byte RotX, RotY;
        }

        public override string[] Help { get { return help; } }
        static string[] help = new string[] {
            "%T/BotAI add [name] spleef",
            "%HCauses the bot to try and spleef you.",
        };
    }

    #endregion

    #region Bow item

    public abstract class Bow
    {
        public string Name { get { return "Bow"; } }

        public class BowData
        {
            public BlockID block;
            public Vec3F32 pos, vel;
            public Vec3U16 last, next;
            public Vec3F32 drag;
            public float power;
            public float gravity;
        }

        public static PlayerBot bot;

        public static bool shooting = false;

        public static SchedulerTask task;

        /// <summary> Called when the bot fires this weapon. </summary>
        public static void OnActivated(PlayerBot bot, Vec3F32 dir)
        {
            BowData data = MakeArgs(bot, dir, Block.Red);
            UpdateNext(bot, data);

            Server.MainScheduler.QueueRepeat(BowCallback, data, TimeSpan.FromMilliseconds(50));
        }

        /// <summary> Applies this weapon to the given bot, and sets up necessary state. </summary>
        public static void Enable(PlayerBot bot, Player pl)
        {
            if (shooting) return;
            shooting = true;

            Bow.bot = bot;

            int dx = bot.Pos.X - pl.Pos.X, dy = bot.Pos.Y - pl.Pos.Y, dz = bot.Pos.Z - pl.Pos.Z;

            Vec3F32 dir = new Vec3F32(-dx, -dy, -dz);
            dir = Vec3F32.Normalise(dir);

            OnActivated(bot, dir);
        }

        static Vec3U16 Round(Vec3F32 v)
        {
            unchecked { return new Vec3U16((ushort)Math.Round(v.X), (ushort)Math.Round(v.Y), (ushort)Math.Round(v.Z)); }
        }

        static BowData MakeArgs(PlayerBot bot, Vec3F32 dir, BlockID block)
        {
            BowData args = new BowData();
            args.block = Block.FromRaw(725);

            args.drag = new Vec3F32(0.95f, 0.98f, 0.95f);
            args.gravity = 0.08f;

            args.pos = bot.Pos.BlockCoords;
            args.last = Round(args.pos);
            args.next = Round(args.pos);

            float push = 3 * 0.755f;
            args.vel = new Vec3F32(dir.X * push, dir.Y * push, dir.Z * push);

            return args;
        }

        static void RevertLast(PlayerBot bot, BowData data)
        {
            bot.level.BroadcastRevert(data.last.X, data.last.Y, data.last.Z);
        }

        static void UpdateNext(PlayerBot bot, BowData data)
        {
            bot.level.BroadcastChange(data.next.X, data.next.Y, data.next.Z, data.block);
        }

        static void OnHitPlayer(PlayerBot bot, BowData args, Player pl)
        {
            if (pl == null) return;
            if (pl.invincible || pl.Game.Referee) return;

            PushPlayer(bot, pl);
        }

        static void PushPlayer(PlayerBot bot, Player pl)
        {
            if (pl.level.Config.MOTD.ToLower().Contains("-damage")) return;

            int srcHeight = ModelInfo.CalcEyeHeight(bot);
            int dstHeight = ModelInfo.CalcEyeHeight(pl);
            int dx = bot.Pos.X - pl.Pos.X, dy = (bot.Pos.Y + srcHeight) - (pl.Pos.Y + dstHeight), dz = bot.Pos.Z - pl.Pos.Z;

            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            if (dir.Length > 0) dir = Vec3F32.Normalise(dir);

            float mult = 1 / ModelInfo.GetRawScale(pl.Model);
            float plScale = ModelInfo.GetRawScale(pl.Model);

            if (pl.Supports(CpeExt.VelocityControl))
            {
                // Intensity of force is in part determined by model scale
                // Also incremented by power of bow

                float push = 3 * 0.77f;
                pl.Send(Packet.VelocityControl((-dir.X * mult) * push, 1.0117f * mult, (-dir.Z * mult) * push, 0, 1, 0));
            }
        }

        static void BowCallback(SchedulerTask task)
        {
            Bow.task = task;
            BowData data = (BowData)task.State;
            if (TickBow(bot, data)) return;

            // Done
            RevertLast(bot, data);
            task.Repeating = false;
        }

        static Player PlayerAt(PlayerBot bot, Vec3U16 pos)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.level != bot.level) continue;

                if (Math.Abs(pl.Pos.BlockX - pos.X) <= 1
                    && Math.Abs(pl.Pos.BlockY - pos.Y) <= 1
                    && Math.Abs(pl.Pos.BlockZ - pos.Z) <= 1)
                {
                    return pl;
                }
            }
            return null;
        }

        protected static PlayerBot BotAt(PlayerBot bot, Vec3U16 pos)
        {
            PlayerBot[] bots = bot.level.Bots.Items;
            foreach (PlayerBot b in bots)
            {
                if (b == null) continue;
                if (b == bot) continue;

                Vec3F32 scale = ModelInfo.CalcScale(b);

                float scaleX = scale.X == 0f ? 1f : scale.X;
                float scaleY = scale.Y == 0f ? 1f : scale.Y;
                float scaleZ = scale.Z == 0f ? 1f : scale.Z;

                scaleX = scaleX / 4f;
                if (scaleY > 1f) scaleY = scaleY * 2f;
                scaleZ = scaleZ / 4f;

                if (Math.Abs(b.Pos.BlockX - pos.X) <= scaleX
                    && Math.Abs(b.Pos.BlockY - pos.Y) <= scaleY
                    && Math.Abs(b.Pos.BlockZ - pos.Z) <= scaleZ)
                {
                    return b;
                }
            }
            return null;
        }

        static void OnHitBlock(PlayerBot bot, BowData args, Vec3U16 pos, BlockID block)
        {
        }

        static void OnHitBot(PlayerBot bot, BowData args, PlayerBot hit)
        {
            if (hit == null) return;
            if (bot == hit) return;

            int number;
            bool isNumber = int.TryParse(hit.Owner, out number);
            if (hit.Owner == null || !isNumber) return;

            int damage = 3;

            HurtBot(damage, hit, bot);
        }

        public static void HurtBot(int damage, PlayerBot hit, PlayerBot bot)
        {
            int srcHeight = ModelInfo.CalcEyeHeight(hit);
            int dstHeight = ModelInfo.CalcEyeHeight(bot);
            int dx = bot.Pos.X - hit.Pos.X, dy = (bot.Pos.Y + srcHeight) - (hit.Pos.Y + dstHeight), dz = bot.Pos.Z - hit.Pos.Z;
            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            if (dir.Length > 0) dir = Vec3F32.Normalise(dir);

            float mult = 1 / ModelInfo.GetRawScale(hit.Model);
            float plScale = ModelInfo.GetRawScale(hit.Model);

            Position newPos;
            newPos.X = hit.Pos.X + (int)(hit.Pos.X - bot.Pos.X);
            newPos.Y = hit.Pos.Y;
            newPos.Z = hit.Pos.Z + (int)(hit.Pos.Z - bot.Pos.Z);

            Position newMidPos;
            newMidPos.X = hit.Pos.X + (int)((hit.Pos.X - bot.Pos.X) / 2);
            newMidPos.Y = hit.Pos.Y;
            newMidPos.Z = hit.Pos.Z + (int)((hit.Pos.Z - bot.Pos.Z) / 2);

            if (hit.level.IsAirAt((ushort)newPos.BlockX, (ushort)newPos.BlockY, (ushort)newPos.BlockZ) && hit.level.IsAirAt((ushort)newPos.BlockX, (ushort)(newPos.BlockY - 1), (ushort)newPos.BlockZ) &&
            hit.level.IsAirAt((ushort)newMidPos.BlockX, (ushort)newMidPos.BlockY, (ushort)newMidPos.BlockZ) && hit.level.IsAirAt((ushort)newMidPos.BlockX, (ushort)(newMidPos.BlockY - 1), (ushort)newMidPos.BlockZ))
            {
                hit.Pos = new Position(newPos.X, newPos.Y, newPos.Z);
            }

            int hp;
            bool isNumber = int.TryParse(hit.Owner, out hp);
            if (hit.Owner == null || !isNumber) return;

            hit.Owner = (hp - damage).ToString();

            if (hp <= 0)
            {
                // Despawn bot
                //Command.Find("Effect").Use(p, "smoke " + bot.Pos.FeetBlockCoords.X + " " + bot.Pos.FeetBlockCoords.Y + " " + bot.Pos.FeetBlockCoords.Z + " 0 0 0 true");
                PlayerBot.Remove(bot);
            }
        }

        static bool TickBow(PlayerBot bot, BowData data)
        {
            Vec3U16 pos = data.next;
            BlockID cur = bot.level.GetBlock(pos.X, pos.Y, pos.Z);

            // Hit a block
            if (cur == Block.Invalid) { shooting = false; return false; }
            //BlockDefinition def = p.level.GetBlockDef(cur);
            if (cur != Block.Air) { OnHitBlock(bot, data, pos, cur); shooting = false; return false; }

            // Hit a bot
            PlayerBot hit = BotAt(bot, pos);
            if (hit != null) { OnHitBot(bot, data, hit); shooting = false; return false; }

            // Hit a victim
            Player pl = PlayerAt(bot, pos);
            if (pl != null) { OnHitPlayer(bot, data, pl); shooting = false; return false; }

            // Apply physics
            data.pos += data.vel;
            data.vel.X *= data.drag.X; data.vel.Y *= data.drag.Y; data.vel.Z *= data.drag.Z;
            data.vel.Y -= data.gravity;

            data.next = Round(data.pos);
            if (data.last == data.next) { shooting = false; return true; }

            // Moved a block, update in world
            RevertLast(bot, data);
            UpdateNext(bot, data);
            data.last = data.next;
            shooting = false;
            return true;
        }
    }

    public class AmmunitionData
    {
        public BlockID block;
        public Vec3U16 start;
        public Vec3F32 dir;
        public bool moving = true;

        // Positions of all currently visible "trailing" blocks
        public List<Vec3U16> visible = new List<Vec3U16>();
        // Position of all blocks this ammunition has touched/gone through
        public List<Vec3U16> all = new List<Vec3U16>();
        public int iterations;

        public Vec3U16 PosAt(int i)
        {
            Vec3U16 target;
            target.X = (ushort)Math.Round(start.X + (double)(dir.X * i));
            target.Y = (ushort)Math.Round(start.Y + (double)(dir.Y * i));
            target.Z = (ushort)Math.Round(start.Z + (double)(dir.Z * i));
            return target;
        }
    }

    #endregion
}
