using System;
using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.DB;

namespace Marry {
	public sealed class MarryPlugin : Plugin {
		public override string name { get { return "MarryPlugin"; } }
		public override string MCGalaxy_Version { get { return "1.9.3.1"; } }
		
		public const string EXTRA_KEY = "__Marry_Name";
		public static PlayerExtList marriages;
		static OnlineStatPrinter onlineLine;
		static OfflineStatPrinter offlineLine;

		public override void Load(bool startup) {
			Command.Register(new CmdAccept());
			Command.Register(new CmdDeny());
			Command.Register(new CmdDivorce());
			Command.Register(new CmdMarry());

			marriages = PlayerExtList.Load("extra/marriages.txt");		
			onlineLine  = (p, who) => FormatMarriedTo(p, who.name);
			offlineLine = (p, who) => FormatMarriedTo(p, who.Name);
			OnlineStat.Stats.Add(onlineLine);
			OfflineStat.Stats.Add(offlineLine);
		}
		
		public override void Unload(bool shutdown) {
			Command.Unregister(Command.Find("Accept"));
			Command.Unregister(Command.Find("Deny"));
			Command.Unregister(Command.Find("Divorce"));
			Command.Unregister(Command.Find("Marry"));
			
			OnlineStat.Stats.Remove(onlineLine);
			OfflineStat.Stats.Remove(offlineLine);
		}
		
		
		static void FormatMarriedTo(Player p, string who) {
			string data = marriages.FindData(who);
			if (data == null) return;
			p.Message("  Married to {0}", p.FormatNick(data));
		}
				
		public static Player CheckProposal(Player p) {
			string name = p.Extras.GetString(MarryPlugin.EXTRA_KEY);
			if (name == null) {
				p.Message("You do not have a pending marriage proposal."); return null;
			}
			
			Player src = PlayerInfo.FindExact(name);
			if (src == null) {
				p.Message("The person who proposed to marry you isn't online."); return null;
			}
			
			if (MarryPlugin.marriages.FindData(name) != null) {
				p.Message("{0} is already married to someone else.", p.FormatNick(name));
				p.Extras.Remove(MarryPlugin.EXTRA_KEY); return null;
			}
			
			if (MarryPlugin.marriages.FindData(p.name) != null) {
                p.Message("You are already married to someone else.");
				return null;
			}
			return src;
		}
	}
	
	public sealed class CmdAccept : Command {
		public override string name { get { return "Accept"; } }
		public override string type { get { return "fun"; } }
		
		public override void Use(Player p, string message) {
			Player proposer = MarryPlugin.CheckProposal(p);
			if (proposer == null) return;
			
			const string msg = "-λNICK &aaccepted {0}&S's proposal, and they are now happily married-";
			Chat.MessageFrom(p, string.Format(msg, p.FormatNick(proposer)));
			
			p.Message("&bYou &aaccepted &b{0}&b's proposal", p.FormatNick(proposer));
			proposer.SetMoney(proposer.money - 200);
			
			MarryPlugin.marriages.Update(p.name, proposer.name);
			MarryPlugin.marriages.Update(proposer.name, p.name);
			MarryPlugin.marriages.Save();
			p.Extras.Remove(MarryPlugin.EXTRA_KEY);
		}
		
		public override void Help(Player p) {
			p.Message("&T/Accept &H- Accepts a pending marriage proposal.");
		}
	}

	public class CmdDeny : Command {
		public override string name { get { return "Deny"; } }
		public override string type { get { return "fun"; } }
		
		public override void Use(Player p, string message) {
			Player proposer = MarryPlugin.CheckProposal(p);
			if (proposer == null) return;
			
			const string msg = "-λNICK &Sdenied {0}&S's proposal, it just wasn't meant to be-";
			Chat.MessageFrom(p, string.Format(msg, p.FormatNick(proposer)));
			
			p.Message("&bYou &cdenied &b{0}&b's proposal", p.FormatNick(proposer));
			p.Extras.Remove(MarryPlugin.EXTRA_KEY);
		}
		
		public override void Help(Player p) {
			p.Message("&T/Deny &H- Denies a pending marriage proposal.");
		}
	}

	public sealed class CmdDivorce : Command {
		public override string name { get { return "Divorce"; } }
		public override string type { get { return "fun"; } }
		
		public override void Use(Player p, string message) {
			string marriedTo = MarryPlugin.marriages.FindData(p.name);
			if (marriedTo == null) { p.Message("You are not married to anyone."); return; }
			
			if (p.money < 50) {
				p.Message("You need at least 50 &3{0} &Sto divorce your partner.", Server.Config.Currency); 
				return;
			}
			p.SetMoney(p.money - 50);
			
			MarryPlugin.marriages.Remove(p.name);
			MarryPlugin.marriages.Remove(marriedTo);
			MarryPlugin.marriages.Save();
			
			const string msg = "-λNICK&S just divorced {0}&S-";
			Chat.MessageFrom(p, string.Format(msg, p.FormatNick(marriedTo)));
			
			Player partner = PlayerInfo.FindExact(marriedTo);
			if (partner != null)
				partner.Message("{0} &bjust divorced you.", partner.FormatNick(p));
		}
		
		public override void Help(Player p) {
			p.Message("&T/Divorce");
			p.Message("&HLeaves the player you are currently married to.");
			p.Message("  &HCosts 50 &3" + Server.Config.Currency);
		}
	}

	public sealed class CmdMarry : Command {
		public override string name { get { return "Marry"; } }
		public override string type { get { return "fun"; } }
		
		public override void Use(Player p, string message) {
			string entry = MarryPlugin.marriages.FindData(p.name);
			if (entry != null) {
				p.Message("&WYou are already married to someone"); return;
			}
			
			if (p.money < 200) {
				p.Message("You need at least 200 &3{0} &Sto marry someone.", Server.Config.Currency); 
				return;
			}
			
			Player partner = PlayerInfo.FindMatches(p, message);
			if (partner == null) return;
			if (partner == p) { p.Message("&WYou cannot marry yourself."); return; }
			
			entry = MarryPlugin.marriages.FindData(partner.name);
			if (entry != null) {
				p.Message("{0} &Sis already married to someone else", p.FormatNick(partner)); 
				return;
			}
			
			const string msg = "λNICK&S is asking {0}&S for their hand in marriage!";
			Chat.MessageFrom(p, "-λNICK&S gets down on one knee-");
			Chat.MessageFrom(p, string.Format(msg, p.FormatNick(partner)));
			
			partner.Extras[MarryPlugin.EXTRA_KEY] = p.name;
			partner.Message("&bTo accept their proposal type &a/Accept");
			partner.Message("&bOr to deny it, type &c/Deny");
		}
		
		public override void Help(Player p) {
			p.Message("&T/Marry [player]");
			p.Message("&HProposes to the given player.");
			p.Message("  &HCosts 200 &3" + Server.Config.Currency);
		}
	}	
}
