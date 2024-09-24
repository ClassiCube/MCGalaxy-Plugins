//This is an example plugin source!
using System;
namespace MCGalaxy {
	public class Example : Plugin {
		public override string name { get { return "Example"; } }
		public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
		public override int build { get { return 100; } }
		public override string welcome { get { return "Loaded Message!"; } }
		public override string creator { get { return "Venk's Private Server"; } }
		public override bool LoadAtStartup { get { return true; } }

		public override void Load(bool startup) {
			//LOAD YOUR PLUGIN WITH EVENTS OR OTHER THINGS!
		}
                        
		public override void Unload(bool shutdown) {
			//UNLOAD YOUR PLUGIN BY SAVING FILES OR DISPOSING OBJECTS!
		}
                        
		public override void Help(Player p) {
			//HELP INFO!
		}
	}
}
