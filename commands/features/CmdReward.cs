// You will need to put a random code on line 16

using System;
using MCGalaxy.Eco;

namespace MCGalaxy {
	public class CmdReward : Command {
		public override string name { get { return "Reward"; } }
		public override string type { get { return "Economy"; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }
        
		public override void Use(Player p, string message) {
			p.lastCMD = "cheat"; // We don't want them to bypass by typing '/'
			string[] args = message.SplitSpaces();
			if (args.Length == 0) return;
			if (args[0] != "secret-code-here") { p.Message("You cannot use this command normally!"); return; }
			p.SetMoney(p.money + int.Parse(args[1])); // Will error if you don't specify an amount
			// Not really any point in checking because it's a secret command
		}

		public override void Help(Player p) {
			p.Message("&T/Reward [secret code] [amount]");
			p.Message("&HGives you rewards after completing a task.");
		}
	}
}
