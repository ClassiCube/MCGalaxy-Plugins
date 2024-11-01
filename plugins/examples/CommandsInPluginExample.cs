using System;
using MCGalaxy;

namespace Core {
    public class CommandsInPluginExample : Plugin {  
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.2.8"; } }
        public override string name { get { return "CommandsInPluginExample"; } }

        public override void Load(bool startup) {
            Command.Register(new CmdSomething());
        }
        
        public override void Unload(bool shutdown) {
        	Command.Unregister(Command.Find("Something"));
        }
    }
    
    public class CmdSomething : Command2 {
        public override string name { get { return "Something"; } }
        public override string type { get { return "other"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        
        public override void Use(Player p, string message) {
            p.Message("Something.");
        }

        public override void Help(Player p) {
            p.Message("&T/Something &S- Does something.");
        }
    }
}
