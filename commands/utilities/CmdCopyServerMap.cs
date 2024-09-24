using System;
using System.Collections.Generic;
using System.IO;
using MCGalaxy;
using MCGalaxy.Blocks;
using MCGalaxy.Blocks.Extended;
using MCGalaxy.SQL;
using BlockID = System.UInt16;

public sealed class CmdCopyServerMap : Command 
{
	public override string name { get { return "CopyServerMap"; } }
	public override string type { get { return CommandTypes.World; } }
	public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
	
	// Change this to the appropriate server folder
	// You MUST have the trailing \ at the end
	const string oldServer = @"C:\server1\";

	public override void Use(Player p, string message) {
		string[] args = message.SplitSpaces(2);
		string src    = args[0].ToLower();
		string dst    = args.Length > 1 ? args[1].ToLower() : src;
		
		if (message.Length == 0) { Help(p); return; }
		if (!Formatter.ValidMapName(p, src)) return;
		if (!Formatter.ValidMapName(p, dst)) return;
		
		string path = Path.Combine(oldServer, LevelInfo.MapPath(src));
		if (!File.Exists(path)) {
			p.Message("&WLevel {0} does not exist on old server.", src); return;
		}
		if (LevelInfo.MapExists(dst)) {
			p.Message("&WLevel {0} already exists on new server.", dst); return;
		}
		
		File.Copy(path, LevelInfo.MapPath(dst), true);
		CopyProperties(src, dst);
		CopyBlockDefs(src,  dst);
		CopyBlockProps(src, dst);
		ExportPortals(p,  src, dst);
		ExportMessages(p, src, dst);
		p.Message("Successfully imported level {0} as {1}", src, dst);
	}
	
	static void CopyProperties(string src, string dst) {
		string path = Path.Combine(oldServer, LevelInfo.PropsPath(src));
		if (!File.Exists(path)) return;
		File.Copy(path, LevelInfo.PropsPath(dst), true);
	}
	
	static void CopyBlockDefs(string src, string dst) {
		string path = null;
		
		path = Path.Combine(oldServer, BlockDefinition.GlobalPath);
		BlockDefinition[] defs = BlockDefinition.Load(path);
		path = Path.Combine(oldServer, Paths.MapBlockDefs(src));
		
		// Local/Level custom blocks override global ones
		if (File.Exists(path)) {
			BlockDefinition[] localDefs = BlockDefinition.Load(path);
			for (int i = 0; i < localDefs.Length; i++) 
			{
				if (localDefs[i] == null || string.IsNullOrEmpty(localDefs[i].Name)) continue;
				defs[i] = localDefs[i];
			}
		}
		
		// If block was original classic/CPE block on old server, but is now a custom block on new server
		// then make a block definition to prevent textures looking wrong (e.g. for Glass)
		for (int i = 0; i < Block.CPE_COUNT; i++) 
		{
			if (defs[i] == null && BlockDefinition.GlobalDefs[i] != null) {
				defs[i] = DefaultSet.MakeCustomBlock((BlockID)i);
			}
		}
		
		defs[0] = null;
		BlockDefinition.Save(false, defs, Paths.MapBlockDefs(dst));
	}
	
	static void CopyBlockProps(string src, string dst) {
		string path = Path.Combine(oldServer, Paths.BlockPropsPath("_" + src));
		if (!File.Exists(path)) return;
		File.Copy(path, Paths.BlockPropsPath("_" + dst), true);
	}
	
	// TODO The database api kinda sucks, need to rewrite
	// ===========================================================
	// ========================= PORTALS =========================
	// ===========================================================
	static void ExportPortals(Player p, string src, string dst) {
		List<PortalInfo> list = new List<PortalInfo>();
		OldReadRows("Portals" + src, "EntryX,EntryY,EntryZ,ExitX,ExitY,ExitZ,ExitMap", 
					row => list.Add(ParsePortal(row)));
		if (list.Count == 0) return;
		
		p.Message("Creating portals table..");
		foreach (PortalInfo i in list) 
		{
			Portal.Set(dst, i.EntryX, i.EntryY, i.EntryZ,
			           i.ExitX, i.ExitY, i.ExitZ, i.ExitMap.Replace(src, dst));
		}
		p.Message("Done copying portals");
	}

	class PortalInfo 
	{
		public ushort EntryX, EntryY, EntryZ;
		public ushort ExitX,  ExitY,  ExitZ;
		public string ExitMap;
	}

	static PortalInfo ParsePortal(ISqlRecord record) {
		PortalInfo i = new PortalInfo();
		i.EntryX = (ushort)record.GetInt32(0);
		i.EntryY = (ushort)record.GetInt32(1);
		i.EntryZ = (ushort)record.GetInt32(2);

		i.ExitX  = (ushort)record.GetInt32(3);
		i.ExitY  = (ushort)record.GetInt32(4);
		i.ExitZ  = (ushort)record.GetInt32(5);

		i.ExitMap = record.GetString(6);
		return i;
	}


	// ===========================================================
	// ===================== MESSAGE BLOCKS ======================
	// ===========================================================
	static void ExportMessages(Player p, string src, string dst) {
		List<MessageInfo> list = new List<MessageInfo>();
		OldReadRows("Messages" + src, "X,Y,Z,Message", 
					row => list.Add(ParseMessage(row)));
		if (list.Count == 0) return;
		
		p.Message("Creating message blocks table..");
		foreach (MessageInfo i in list) 
		{
			MessageBlock.Set(dst, i.X, i.Y, i.Z, i.Text);
		}
		p.Message("Done copying message blocks");
	}

	class MessageInfo 
	{
		public ushort X, Y, Z;
		public string Text;
	}

	static MessageInfo ParseMessage(ISqlRecord record) {
		MessageInfo i = new MessageInfo();
		i.X = (ushort)record.GetInt32(0);
		i.Y = (ushort)record.GetInt32(1);
		i.Z = (ushort)record.GetInt32(2);

		i.Text = record.GetString(3).Replace("\\'", "\'").Cp437ToUnicode();
		return i;
	}

	// ===========================================================
	// ======================== DATABASE =========================
	// ===========================================================
	sealed class OldSQLiteConnection : SQLiteConnection 
	{
		protected override bool ConnectionPooling { get { return false; } }
		protected override string DBPath { get { return Path.Combine(oldServer, "MCGalaxy.db"); } }
	}
	
	static void OldReadRows(string table, string columns, ReaderCallback callback) {
		using (SQLiteConnection conn = new OldSQLiteConnection()) {
			conn.Open();
			if (!OldTableUsed(conn, table)) return;
			string sql = SQLiteBackend.Instance.ReadRowsSql(table, columns, "");

			using (ISqlCommand cmd = conn.CreateCommand(sql)) {
				using (ISqlReader r = cmd.ExecuteReader()) 
				{
					while (r.Read()) callback(r);
				}
			}
		}
	}
	
	static bool OldTableUsed(ISqlConnection conn, string table) {
		IDatabaseBackend sqliteDB = SQLiteBackend.Instance;
		string sql = sqliteDB.ReadRowsSql("sqlite_master", "COUNT(*)", "WHERE type='table' AND name=@0");
		int count  = 0;
		
		using (ISqlCommand cmd = conn.CreateCommand(sql)) {
			sqliteDB.FillParams(cmd, new object[] { table });
			using (ISqlReader r = cmd.ExecuteReader()) 
			{
				if (r.Read()) count = r.GetInt32(0);
			}
		}
		return count > 0;
	}
	
	public override void Help(Player p) {
		p.Message("&T/CopyServerMap [old name] <new name>");
		p.Message("&HCopies a level across from old server to this server");
		p.Message("&HIf <new name> is not given, then [old name] is used as name for the new level");
	}
}
