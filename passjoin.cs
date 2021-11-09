using System;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Commands;

namespace PluginPassJoin {
	public sealed class Core : Plugin {
		public override string creator { get { return "Goodly"; } }
		public override string name { get { return "passjoin"; } }
		public override string MCGalaxy_Version { get { return "1.9.3.6"; } }
		const string PASSWORD = "changethis";
		public override void Load(bool startup) {
			OnPlayerCommandEvent.Register(OnPlayerCommand, Priority.Low);
            OnPlayerChatEvent.Register(OnPlayerChat, Priority.High);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerCommandEvent.Unregister(OnPlayerCommand);
            OnPlayerChatEvent.Unregister(OnPlayerChat);
		}
        
        static void OnPlayerCommand(Player p, string cmd, string args, CommandData data) {
            
            // the command /agree is actually just an alias for "/rules agree", so...
            
			if (cmd.CaselessEq("rules")) {
                if (!args.CaselessEq("agree")) { return; } //we only want to modify logic of /rules agree
                p.cancelcommand = true; //setting this to true will cause the command intercepted by this event to not run
                
                if (!p.hasreadrules) { p.Message("&9You must read &T/Rules &9before agreeing."); return; }
                p.Message("Use &T/Pass [password] &Sto finish agreeing to the rules.");
                return;
            }
            
			if (cmd.CaselessEq("pass")) {
                p.cancelcommand = true;
                //the command /pass actually exists and does something else but we're going to hijack it because it is
                //one of the few commands that actually invokes OnPlayerCommand before the user has agreed to the rules
                if (p.agreed) { p.Message("You already correctly entered the password. ☺"); return; }
                if (!p.hasreadrules) { p.Message("&9You must read &T/Rules &9before entering the password."); return; }
                if (args.Length == 0) { p.Message("Please enter a password."); return; }
                if (args != PASSWORD) { p.Message("Incorrect password."); return; }
                p.Message("&6You've succesfully entered the password. Welcome!");
                Command.Find("rules").Use(p, "agree");
                return; //this return is only here in case you add other command cases below this if statement
			}
		}
        static void OnPlayerChat(Player p, string message) {
            if (!p.agreed) { p.Message("&cBefore speaking, you must read /rules then agree to them with /agree."); p.cancelchat = true; }
        }
		
	}
}