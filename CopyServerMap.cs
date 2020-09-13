namespace MCGalaxy {
	public sealed class CmdCopyServerMap : Command {
		public override string name { get { return "CopyServerMap"; } }
		public override string type { get { return CommandTypes.World; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
		
		// Change this to the appropriate server folder
		// You MUST have the trailing \ at the end
		const string oldServer = @"C:\server1\";

		public override void Use(Player p, string message) {
			string[] args = message.SplitSpaces(2);
			string map    = args[0];
			
			if (args.Length != 1) { Help(p); return; }
			if (!Formatter.ValidMapName(p, map)) return;
			
			string path = Path.Combine(oldServer, LevelInfo.MapPath(map));
			if (!File.Exists(path)) {
				p.Message("%WMap does not exist on old server."); return;
			}
			if (LevelInfo.MapExists(map)) {
				p.Message("%WMap already exists on new server."); return;
			}
			
			File.Copy(path, LevelInfo.MapPath(map), true);
			CopyProperties(map);
			CopyBlockDefs(map);
			p.Message("Successfully copied across map {0}!", map);
		}
		
		static void CopyProperties(string map) {
			string props = null;
			if (File.Exists(PropsPathOld(oldServer, map))) {
				props = File.ReadAllText(PropsPathOld(oldServer, map));
			} else if (File.Exists(PropsPath(oldServer, map))) {
				props = File.ReadAllText(PropsPath(oldServer, map));
			}
			// TODO fixup
			
			if (props == null) return;
			File.WriteAllText(LevelInfo.PropsPath(map), props);
		}
		
		static void CopyBlockDefs(string map) {
			string path = null;
			
			path = Path.Combine(oldServer, BlockDefinition.GlobalPath);
			BlockDefinition[] defs = BlockDefinition.Load(path);
			path = Path.Combine(oldServer, Paths.MapBlockDefs(map));
			
			// Local/Level custom blocks override global ones
			if (File.Exists(path)) {
				BlockDefinition[] localDefs = BlockDefinition.Load(path);
				for (int i = 0; i < localDefs.Length; i++) {
					if (localDefs[i] == null || string.IsNullOrEmpty(localDefs[i].Name)) continue;
					defs[i] = localDefs[i];
				}
			}
			
			// If block was original classic/CPE block on old server, but is now a custom block on new server
			// then make a block definition to prevent textures looking wrong (e.g. for Glass)
			for (int i = 0; i < Block.CpeCount; i++) {
				if (defs[i] == null && BlockDefinition.GlobalDefs[i] != null) {
					defs[i] = DefaultSet.MakeCustomBlock((BlockID)i);
				}
			}
			
			defs[0] = null;
			BlockDefinition.Save(false, defs, Paths.MapBlockDefs(map));
		}

		static string PropsPath(string dir, string file) {
			return dir + @"levels\level properties\" + file + ".properties";
		}

		static string PropsPathOld(string dir, string file) {
			return dir + @"levels\level properties\" + file;
		}
		
		public override void Help(Player p) {
			p.Message("%T/CopyServerMap [map]");
			p.Message("%HCopies a map across from old server to this server");
		}
	}
}
