using System;
using System.Collections.Generic;
using System.IO;
using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.DB;
using MCGalaxy.Drawing.Ops;
using MCGalaxy.Maths;
using BlockID = System.UInt16;
using MCGalaxy.Util;

namespace CommandPruneDB
{
	public class CmdPruneDB : Command2 
	{
		public override string name { get { return "PruneDB"; } }
		public override string type { get { return CommandTypes.Moderation; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
		public override bool SuperUseable { get { return false; } }
		
		public override void Use(Player p, string message, CommandData data) {
			if (message.Length == 0) { p.Message("You need to provide a player name."); return; }
			
			string[] parts = message.SplitSpaces(), names = null;
			int[] ids = GetIds(p, data, parts, out names);
			if (ids == null) return;
			
			TimeSpan delta = GetDelta(p, parts[0], parts, 1);
			if (delta == TimeSpan.MinValue) return;

			BlockDB db = p.level.BlockDB;
			DateTime start = DateTime.UtcNow - delta;
			
			BlockDBCacheWriter w = new BlockDBCacheWriter();
			w.start = (int)((start - BlockDB.Epoch).TotalSeconds);
			
			using (IDisposable locker = db.Locker.AccquireWrite()) {
				if (!File.Exists(db.FilePath)) {
					p.Message("&WBlockDB file for this map doesn't exist.");
					return;
				}
				
				Vec3U16 dims;
				FastList<BlockDBEntry> entries = new FastList<BlockDBEntry>(4096);
				
				using (Stream src = OpenRead(db.FilePath), dst = OpenWrite(db.FilePath + ".tmp")) {
					BlockDBFile format = BlockDBFile.ReadHeader(src, out dims);
					BlockDBFile.WriteHeader(dst, dims);
					w.dst = dst; w.format = format; w.ids = ids;
					
					Read(src, format, w.Output);
					// flush entries leftover
					if (w.entries.Count > 0) format.WriteEntries(dst, w.entries);
				}
				
				string namesStr = names.Join(name => p.FormatNick(name));
				if (w.left > 0) {
					File.Delete(db.FilePath);
					File.Move(db.FilePath + ".tmp", db.FilePath);
					p.Message("Pruned {1}&S's changes ({2} entries left now) for the past &b{0}",
					          delta.Shorten(true), namesStr, w.left);
				} else {
					File.Delete(db.FilePath + ".tmp");
					p.Message("No changes found by {1} &Sin the past &b{0}",
					          delta.Shorten(true), namesStr);
				}
			}
		}
		
		class BlockDBCacheWriter {
			public Stream dst;
			public BlockDBFile format;
			public int[] ids;
			public FastList<BlockDBEntry> entries = new FastList<BlockDBEntry>(4096);
			public int left, start;
			
			public void Output(BlockDBEntry entry) {
				if (entry.TimeDelta >= start) {
					for (int i = 0; i < ids.Length; i++) {
						if (entry.PlayerID == ids[i]) return;
					}
				}
				
				left++;			
				entries.Add(entry);
				if (entries.Count == 4096) {
					format.WriteEntries(dst, entries);
					entries.Count = 0;
				}
			}
		}
		
		// all this copy paste makes me sad
        // TODO: Refactor core MCGalaxy so we don't have to do this..
		
		FileStream OpenWrite(string path) {
			return new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
		}
		
		FileStream OpenRead(string path) {
			return new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
		}
		
		
		static int[] GetIds(Player p, CommandData data, string[] parts, out string[] names) {
			int count = Math.Max(1, parts.Length - 1);
			List<int> ids = new List<int>();
			names = new string[count];
			
			for (int i = 0; i < names.Length; i++) {
				names[i] = PlayerDB.MatchNames(p, parts[i]);
				if (names[i] == null) return null;
				
				Group grp = Group.GroupIn(names[i]);
				if (!CheckRank(p, data, names[i], grp.Permission, "prune", false)) return null;
				ids.AddRange(NameConverter.FindIds(names[i]));
			}
			return ids.ToArray();
		}
		
		static TimeSpan GetDelta(Player p, string name, string[] parts, int offset) {
			TimeSpan delta = TimeSpan.Zero;
			string timespan = parts.Length > offset ? parts[parts.Length - 1] : "30m";
			bool self = p.name.CaselessEq(name);
			
			if (timespan.CaselessEq("all")) {
				return self ? TimeSpan.FromSeconds(int.MaxValue) : p.group.MaxUndo;
			} else if (!CommandParser.GetTimespan(p, timespan, ref delta, "undo the past", "s")) {
				return TimeSpan.MinValue;
			}

			if (delta.TotalSeconds == 0)
				delta = TimeSpan.FromMinutes(90);
			if (!self && delta > p.group.MaxUndo) {
				p.Message("{0}&Ss may only undo up to {1}",
				          p.group.ColoredName, p.group.MaxUndo.Shorten(true, true));
				return p.group.MaxUndo;
			}
			return delta;
		}
		
		static unsafe void Read(Stream s, BlockDBFile format, Action<BlockDBEntry> output) {
			byte[] bulk = new byte[BlockDBFile.BulkEntries * BlockDBFile.EntrySize];
			fixed (byte* ptr = bulk) {
				while (true) {
					BlockDBEntry* e = (BlockDBEntry*)ptr;
					int count = format.ReadForward(s, bulk, e);
					if (count == 0) break;
					
					for (int i = 0; i < count; i++, e++) {
						output(*e);
					}
				}
			}
		}

		public override void Help(Player p) {
			p.Message("&T/PruneDB [player1] <player2..> <timespan>");
			p.Message("&HDeletes the block changes of [players] in the past <timespan> from BlockDB.");
			p.Message("&cSlow and dangerous. Use with care.");
		}
	}
}
