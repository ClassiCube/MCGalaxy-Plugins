using System;
using System.IO;
using MCGalaxy.Bots;
using MCGalaxy.Maths;

namespace MCGalaxy {
    public sealed class PushAI: Plugin {
        BotInstruction ins;

        public override string name {
            get {
                return "PushAI";
            }
        }
        public override string MCGalaxy_Version {
            get {
                return "1.9.1.1";
            }
        }
        public override string creator {
            get {
                return "";
            }
        }

        public override void Load(bool startup) {
            ins = new SneakInstruction();
            BotInstruction.Instructions.Add(ins);
        }

        public override void Unload(bool shutdown) {
            BotInstruction.Instructions.Remove(ins);
        }
    }

    sealed class SneakInstruction: BotInstruction {
        public SneakInstruction() {
            Name = "sneak";
        }

        public override bool Execute(PlayerBot bot, InstructionData data) {
            int search = 10;
            if (data.Metadata != null) search = (ushort) data.Metadata;
            Player closest = ClosestPlayer(bot, search);

            if (closest == null) {
                bot.NextInstruction();
                return false;
            }
            MoveTowards(bot, closest);
            return true;
        }

        internal static Player ClosestPlayer(PlayerBot bot, int search) {
            int maxDist = search * 32;
            Player[] players = PlayerInfo.Online.Items;
            Player closest = null;

            foreach(Player p in players) {
                if (p.level != bot.level || p.invincible || p.hidden) continue;

                int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;
                int playerDist = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                if (playerDist >= maxDist) continue;

                closest = p;
                maxDist = playerDist;
            }
            return closest;
        }

        void MoveTowards(PlayerBot bot, Player p) {
            string msg = bot.DeathMessage;
            if (msg == null) msg = "@p %Swas &ccaught.";
            p.HandleDeath(Block.Cobblestone, msg);
        }

        public override InstructionData Parse(string[] args) {
            InstructionData data =
                default (InstructionData);
            if (args.Length > 1)
                data.Metadata = ushort.Parse(args[1]);
            return data;
        }

        public override void Output(Player p, string[] args, TextWriter w) {
            if (args.Length > 3) {
                w.WriteLine(Name + " " + ushort.Parse(args[3]));
            } else {
                w.WriteLine(Name);
            }
        }

        public override string[] Help {
            get {
                return help;
            }
        }
        static string[] help = new string[] {
            "%T/BotAI add [name] sneak <radius>",
            "%HCauses the bot to kil the closest player in the search radius.",
            "%H  <radius> defaults to 10 blocks.",
        };
    }
}
