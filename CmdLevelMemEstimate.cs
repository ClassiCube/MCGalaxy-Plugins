using System;
using System.Reflection;
using MCGalaxy.Util;

namespace MCGalaxy 
{
	public sealed class CmdLevelMemEstimate : Command2 
	{
		public override string name { get { return "LevelMemoryEstimate"; } }
		public override string shortcut { get { return "memestimate"; } }
		public override string type { get { return "Info"; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

		public override void Use(Player p, string message, CommandData data) {
			Level[] loaded = LevelInfo.Loaded.Items;
			long totalMem  = 0;

			p.Message("Estimated memory usage of currently loaded levels:");
			foreach (Level lvl in loaded)
			{
				long level_blocks  = lvl.blocks.Length;
				long level_custom  = EstimateCustomBlocks(lvl);
				long level_physics = EstimatePhysics(lvl);
				long level_total   = level_blocks + level_custom + level_physics;

				p.Message("  {0}: &T{1} &S({2} blocks, {3} physics)",
						lvl.ColoredName, Simplify(level_total), Simplify(level_blocks + level_custom), Simplify(level_physics));
				totalMem += level_total;
			}

			p.Message("Total memory: &T{0}", Simplify(totalMem));
		}

		static long EstimateCustomBlocks(Level lvl) {
			byte[][] chunks = lvl.CustomBlocks;
			long memory = chunks.Length * (long)IntPtr.Size;

			for (int i = 0; i < chunks.Length; i++)
			{
				if (chunks[i] == null) continue;
				memory += chunks[i].Length;
			}
			return memory;
		}

		static long EstimatePhysics(Level lvl) {
			return
				GetCount(lvl, "ListCheck")  * (long)8 + // sizeof(Check)
				GetCount(lvl, "ListUpdate") * (long)8;  // sizeof(Update)
		}

		static int GetCount(Level lvl, string name)
		{
			object list = GetField(lvl, name);
			// Check type is internal, so can't easily use it in a custom command
			// FastList<Check> checks  = (FastList<Check>)GetField(lvl,  "ListCheck");
			return (int)GetField(list, "Count");
		}
		
		static object GetField(object obj, string name)
		{
			FieldInfo field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			return field.GetValue(obj);
		}

		static string Simplify(long bytes) {
			const long ONE_MB = 1024 * 1024;
			if (bytes <= ONE_MB) {
				return (bytes / 1024.0).ToString("F2") + " KB";
			}
			return (bytes / (1024.0 * 1024.0)).ToString("F2") + " MB";
		}
		
		public override void Help(Player p) {
			p.Message("&T/LevelMemoryEstimate");
			p.Message("&HEstimates how much memory is used by all of the currently loaded levels");
		}
	}
}
