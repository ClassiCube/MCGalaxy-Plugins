using MCGalaxy;

namespace Core {
	public sealed class CmdAdventure : Command2 {
		public override string name { get { return "Adventure"; } }
        public override string shortcut { get { return "ad"; } }
		public override string type { get { return "World"; } }
		
		public override void Use(Player p, string message, CommandData data) {
		    Command.Find("Map").Use(p, "buildable");
		    Command.Find("Map").Use(p, "deletable");
        }
		
		public override void Help(Player p) {
            p.Message("&T/Adventure &H- Toggles adventure mode for a map.");
		}
	}
}
