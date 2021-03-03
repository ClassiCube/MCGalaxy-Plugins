using System;
using MCGalaxy;
using MCGalaxy.Commands;

namespace PluginReward {
	public sealed class RewardPlugin : Plugin {
		public override string creator { get { return "Not UnknownShadow200"; } }
		public override string MCGalaxy_Version { get { return "1.9.2.6"; } }
		public override string name { get { return "Reward"; } }
		
		static PlayerExtList list;
		Command cmd;
		
		public override void Load(bool startup) {
			list = PlayerExtList.Load("text/rewardtimes.txt");
			cmd  = new CmdReward();
			Command.Register(cmd);
		}

		public override void Unload(bool shutdown) {
			Command.Unregister(cmd);
		}
		
		public sealed class CmdReward : Command2 {
			public override string name { get { return "Reward"; } }
			public override bool SuperUseable { get { return false; } }
			public override string type { get { return CommandTypes.Other; } }
			// this way people can't use /last to see the secret password
			public override bool UpdatesLastCmd { get { return false; } }
			// this way only certain users can place this in an MB
			public override bool MessageBlockRestricted { get { return true; } }
			
			public override void Use(Player p, string message, CommandData data) {
				// this command can only be used from within an /MB
				if (data.Context != CommandContext.MessageBlock) {
					p.Message("%W/RewardMoney can only be used in an /MB");
					return;
				}
				
				string[] bits = message.SplitSpaces(3);
				int amount    = 0;
				// money amount must be 0 or a positive number
				if (!CommandParser.GetInt(p, bits[0], "Amount", ref amount, 0)) return;
				
				string timeout  = bits.Length > 1 ? bits[1] : "24h";
				TimeSpan period = default(TimeSpan);
				if (!CommandParser.GetTimespan(p, timeout, ref period, "specify a period of", "h")) return;
				
				// some MBs might use a custom per-user timer instead of global per-user timer
				string prefix = bits.Length > 2 ? bits[2] + "##" : "";
				
				// only allow using once every [period]
				long now   = DateTime.UtcNow.ToUnixTime();
				string raw = list.FindData(prefix + p.name);
				long last  = raw == null ? 0 : long.Parse(raw);
				double end = last + period.TotalSeconds;

				if (end > now) {
					TimeSpan left = TimeSpan.FromSeconds(end - now);
					p.Message("%WYou can only claim this reward every {0}, and must therefore wait another {1}", 
						period.Shorten(true), left.Shorten(true));
					return;
				}
				
				// Remember point in time user last used /Reward
				list.Update(prefix + p.name, now.ToString());
				list.Save();
				
				p.SetMoney(p.money + amount);
				p.Message("You received &a{0} &3{1}!", amount, Server.Config.Currency);
			}

			public override void Help(Player p) {
				p.Message("%T/Reward [amount] <period>");
				p.Message("%HGives you [amount] {0} as a reward", Server.Config.Currency);
				p.Message("%H<period> is how long you must wait between being able to claim a reward, and defaults to 24 hours.");
				p.Message("%T/Reward [amount] [period] [ID]");
				p.Message("%H Uses a ID-specific <period> cooldown instead of global cooldown");
				p.Message("%HNote that %T/Reward %Hcan ony be used in a %T/MB");
			}
		}
	}
}
