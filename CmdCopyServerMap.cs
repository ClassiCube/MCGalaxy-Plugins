//reference System.Data.dll
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using MCGalaxy.Blocks;
using MCGalaxy.SQL;
using BlockID = System.UInt16;

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
			string src    = args[0];
			string dst    = args.Length > 1 ? args[1] : src;
			
			if (message.Length == 0) { Help(p); return; }
			if (!Formatter.ValidMapName(p, src)) return;
			if (!Formatter.ValidMapName(p, dst)) return;
			
			string path = Path.Combine(oldServer, LevelInfo.MapPath(src));
			if (!File.Exists(path)) {
				p.Message("%WLevel {0} does not exist on old server.", src); return;
			}
			if (LevelInfo.MapExists(dst)) {
				p.Message("%WLevel {1} already exists on new server.", dst); return;
			}
			
			File.Copy(path, LevelInfo.MapPath(dst), true);
			CopyProperties(src, dst);
			CopyBlockDefs(src, dst);
			ExportPortals(p, map);
			ExportMessages(p, map);
			p.Message("Successfully imported level {0} as {1}", src, dst);
		}
		
		static void CopyProperties(string src, string dst) {
			string path = Path.Combine(oldServer, LevelInfo.PropsPath(src));
			if (!File.Exists(path)) return;
			
			string props = File.ReadAllText(path);
			File.WriteAllText(LevelInfo.PropsPath(dst), props);
		}
		
		static void CopyBlockDefs(string src, string dst) {
			string path = null;
			
			path = Path.Combine(oldServer, BlockDefinition.GlobalPath);
			BlockDefinition[] defs = BlockDefinition.Load(path);
			path = Path.Combine(oldServer, Paths.MapBlockDefs(src));
			
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
			BlockDefinition.Save(false, defs, Paths.MapBlockDefs(dst));
		}
		
		// TODO The database api kinda sucks, need to rewrite

		// ===========================================================
		// ========================= PORTALS =========================
		// ===========================================================
		static void ExportPortals(Player p, string[] args) {
			string table = "Portals" + args[0];
			if (!TableUsed(table)) return;
			p.Message("Creating portals table..");

			List<PortalInfo> list = new List<PortalInfo>();
			Database.Backend.ReadRows(table, "EntryX,EntryY,EntryZ,ExitX,ExitY,ExitZ", list, ProcessPortal);

			table = "Portals" + args[1];
			using (SQLiteConnection conn = new OldSQLiteConnection()) {
				ResetTable(p, conn, table, portals_fmt);

				foreach (PortalInfo i in list) {
					ExecuteSql(p, conn, "INSERT INTO `" + table + "` VALUES(@0,@1,@2,@3,@4,@5,@6)",
					           i.EntryX, i.EntryY, i.EntryZ, args[1], i.ExitX, i.ExitY, i.ExitZ);
				}
			}
			p.Message("Done copying portals");
		}

		const string portals_fmt = @"(
EntryX MEDIUMINT UNSIGNED,
EntryY MEDIUMINT UNSIGNED,
EntryZ MEDIUMINT UNSIGNED,
ExitMap CHAR(20),
ExitX MEDIUMINT UNSIGNED,
ExitY MEDIUMINT UNSIGNED,
ExitZ MEDIUMINT UNSIGNED);";

		class PortalInfo {
			public int EntryX, EntryY, EntryZ;
			public int ExitX,  ExitY,  ExitZ;
		}

		static object ProcessPortal(IDataRecord record, object arg) {
			PortalInfo i = new PortalInfo();
			i.EntryX = record.GetInt32(0);
			i.EntryY = record.GetInt32(1);
			i.EntryZ = record.GetInt32(2);

			i.ExitX = record.GetInt32(3);
			i.ExitY = record.GetInt32(4);
			i.ExitZ = record.GetInt32(5);

			((List<PortalInfo>)arg).Add(i);
			return arg;
		}


		// ===========================================================
		// ===================== MESSAGE BLOCKS ======================
		// ===========================================================
		static void ExportMessages(Player p, string[] args) {
			string table = "Messages" + args[0];
			if (!TableUsed(table)) return;
			p.Message("Creating message blocks table..");

			List<MessageInfo> list = new List<MessageInfo>();
			Database.Backend.ReadRows(table, "X,Y,Z,Message", list, ProcessMessage);

			table = "Messages" + args[1];
			using (SQLiteConnection conn = new OldSQLiteConnection()) {
				ResetTable(p, conn, table, messages_fmt);

				foreach (MessageInfo i in list) {
					ExecuteSql(p, conn, "INSERT INTO `" + table + "` VALUES(@0,@1,@2,@3)",
					           i.X, i.Y, i.Z, i.Text.UnicodeToCp437());
				}
			}
			p.Message("Done copying message blocks");
		}

		const string messages_fmt = @"(
X MEDIUMINT UNSIGNED,
Y MEDIUMINT UNSIGNED,
Z MEDIUMINT UNSIGNED,
Message CHAR(255));";

		class MessageInfo {
			public int X, Y, Z;
			public string Text;
		}

		static object ProcessMessage(IDataRecord record, object arg) {
			MessageInfo i = new MessageInfo();
			i.X = record.GetInt32(0);
			i.Y = record.GetInt32(1);
			i.Z = record.GetInt32(2);

			i.Text = record.GetString(3).Cp437ToUnicode();
			((List<MessageInfo>)arg).Add(i);
			return arg;
		}

		// ===========================================================
		// ======================== DATABASE =========================
		// ===========================================================
		sealed class OldSQLiteConnection : SQLiteConnection {
			protected override bool ConnectionPooling { get { return false; } }
			protected override string DBPath { get { return Path.Combine(oldServer, "MCGalaxy.db"); } }
		}

		static void ExecuteSql(Player p, SQLiteConnection conn, string sql, params object[] args) {
			try {
				using (IDbCommand cmd = new SQLiteCommand(sql, conn)) {
					for (int i = 0; i < args.Length; i++) {
						IDbDataParameter arg = new SQLiteParameter();
						arg.ParameterName = "@" + i;
						arg.Value         = args[i];
						cmd.Parameters.Add(arg);
					}					
					cmd.ExecuteNonQuery();
				}
			} catch (Exception ex) {
				p.Message("Error executing SQL " + sql);
				p.Message(ex.Message);
			}
		}

		static bool TableUsed(string table) {
			return Database.TableExists(table) && Database.CountRows(table) > 0;
		}

		static void ResetTable(Player p, SQLiteConnection conn, string table, string fmt) {
			conn.Open();
			ExecuteSql(p, conn, "DROP TABLE IF EXISTS `" + table + "`");
			ExecuteSql(p, conn, "CREATE TABLE IF NOT EXISTS `" + table + "` " + fmt);
		}
		
		public override void Help(Player p) {
			p.Message("%T/CopyServerMap [old name] <new name>");
			p.Message("%HCopies a level across from old server to this server");
			p.Message("%HIf <new name> is not given, then [old name] is used as name for the new level");
		}
	}
}
