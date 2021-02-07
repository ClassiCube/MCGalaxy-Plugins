using System;
using System.IO;
using MCGalaxy.Bots;
using MCGalaxy.Maths;

namespace MCGalaxy {
	
	public sealed class FootballPlugin : Plugin {
		BotInstruction ins;
		
		public override string name { get { return "FootballInstruction"; } }
		public override string MCGalaxy_Version { get { return "1.9.1.4"; } }
		public override string creator { get { return ""; } }
		
		public override void Load(bool startup) {
			ins = new FootballInstruction();
			BotInstruction.Instructions.Add(ins);
		}
		
		public override void Unload(bool shutdown) {
			BotInstruction.Instructions.Remove(ins);
		}
	}
	
	sealed class FootballInstruction : BotInstruction {
		public FootballInstruction() { Name = "football"; }
		
		public override bool Execute(PlayerBot bot, InstructionData data) {
			int strength = 20;
			if (data.Metadata != null) strength = (ushort)data.Metadata;
			
			if (bot.movementSpeed > 0) {
				Step(bot);
				//Server.s.Log("STEP: " + bot.movementSpeed);
				bot.movementSpeed--;
			}
			
			GetKicked(bot, strength);
			return true;
		}
		
		void Step(PlayerBot bot) {
			bot.TargetPos = bot.Pos;
			bot.movement  = true;
			Vec3F32 dir = DirUtils.GetDirVector(bot.Rot.RotY, 0);
			
			bot.TargetPos.X = bot.Pos.X + (int)(dir.X * bot.movementSpeed);
			bot.TargetPos.Z = bot.Pos.Z + (int)(dir.Z * bot.movementSpeed);
		}
		
		void GetKicked(PlayerBot bot, int strength) {
			int closestDist  = int.MaxValue;
			Player[] players = PlayerInfo.Online.Items;
			Player closest = null;
			
			foreach (Player p in players) {
				if (p.level != bot.level || p.invincible || p.hidden) continue;
				
				int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;
				dx = Math.Abs(dx); dy = Math.Abs(dy); dz = Math.Abs(dz);
				
				if (dx > 8 || dy > 8 || dz > 8) continue;
				int dist = dx + dy + dz;
				if (dist > closestDist) continue;
				
				closest = p;
				closestDist = dist;
			}
			
			if (closest == null) return;
			bot.SetYawPitch(closest.Rot.RotY, closest.Rot.HeadX);
			
			bot.movementSpeed = strength;
			Step(bot);
		}
		
		static bool MoveTowards(PlayerBot bot, Player p) {
			int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;
			bot.TargetPos = p.Pos;
			bot.movement = true;
			
			Vec3F32 dir = new Vec3F32(dx, dy, dz);
			dir = Vec3F32.Normalise(dir);
			Orientation rot = bot.Rot;
			DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);
			
			// If we are very close to a player, switch from trying to look
			// at them to just facing the opposite direction to them
			if (Math.Abs(dx) < 4 && Math.Abs(dz) < 4) {
				rot.RotY = (byte)(p.Rot.RotY + 128);
			}
			bot.Rot = rot;
			
			return dx <= 8 && dy <= 16 && dz <= 8;
		}
		
		public override InstructionData Parse(string[] args) {
			InstructionData data = default(InstructionData);
			if (args.Length > 1)
				data.Metadata = ushort.Parse(args[1]);
			return data;
		}
		
		public override void Output(Player p, string[] args, StreamWriter w) {
			if (args.Length > 3) {
				w.WriteLine(Name + " " + ushort.Parse(args[3]));
			} else {
				w.WriteLine(Name);
			}
		}
		
		public override string[] Help { get { return help; } }
		static string[] help = new string[] {
			"%T/BotAI add [name] football <strength>",
			"%HCauses the bot to get kicked around when players touch it.",
			"%H[strength] is how much a power a 'kick' does to the ball",
			"%H  <strength> defaults to 20.",
		};
	}
}
