using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using MCGalaxy;
using MCGalaxy.Blocks.Extended;

public sealed class CmdImportICraft : Command
{
	public override string name { get { return "ImportIcraft"; } }
	public override string type { get { return CommandTypes.World; } }
	public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
	
	public override void Use(Player p, string path) {
		if (path.Length == 0) { Help(p); return; }
		
		string map = Path.GetFileName(path); // last folder name
		if (LevelInfo.MapExists(map)) {
			p.Message("&WMap {0} already exists. Rename the folder to something else before importing", map);
			return;
		}
		p.Message("Importing from {0}..", path);
		
		string mapFile= Path.Combine(path, "blocks.gz");
		byte[] blocks = DecodeBlocks(mapFile);
		
		string propsFile = Path.Combine(path, "world.meta");
		MetaFile meta = DecodeMeta(propsFile);
		
		MetaSection size = meta["size"];
		ushort x = ushort.Parse(size["x"]);
		ushort y = ushort.Parse(size["y"]);
		ushort z = ushort.Parse(size["z"]);
		
		int volume = x * y * z;
		if (volume != blocks.Length)
			throw new InvalidDataException("Map volume and length of blocks array do not match");
		Level lvl = new Level(map, x, y, z, blocks);
		
		MetaSection spawn = meta["spawn"];
		lvl.spawnx = ushort.Parse(spawn["x"]);
		lvl.spawny = ushort.Parse(spawn["y"]);
		lvl.spawnz = ushort.Parse(spawn["z"]);
		lvl.rotx   = byte.Parse(spawn["h"]);
		
		p.Message("Importing MBs/Portals from {0}..", path);
		ImportPortals(meta,  lvl);
		ImportMessages(meta, lvl);
		
		try {
			lvl.Save(true);
		} finally {
			lvl.Dispose();
			Server.DoGC();
		}
		
		p.Message("Map {0} imported", map);
	}

	public override void Help(Player p) {
		p.Message("&T/ImportICraft [folder]");
		p.Message("&H- Imports an iCraft map from the given folder");
	}
	
	
	static byte[] DecodeBlocks(string path) {
		using (Stream fs = File.OpenRead(path),
		       gs = new GZipStream(fs, CompressionMode.Decompress))
		{
			byte[] size = new byte[4];
			ReadFully(gs, size, 4);
			int len = NetUtils.ReadI32(size, 0);
			
			byte[] blocks = new byte[len];
			ReadFully(gs, blocks, len);
			return blocks;
		}
	}
	
	class MetaFile : Dictionary<string, MetaSection> { }
	class MetaSection : Dictionary<string, string> { }
	static MetaFile DecodeMeta(string path)
	{
		MetaFile meta = new MetaFile();
		MetaSection curSection = null;
		string line, lastKey = null;
		
		using (StreamReader r = new StreamReader(path))
		{
			while ((line = r.ReadLine()) != null)
			{
				line = line.TrimEnd();
				if (line.Length == 0) continue;
				
				if (line[0] == '[') {
					string section = line.Substring(1, line.IndexOf(']') - 1);
					curSection = new MetaSection();
					meta[section] = curSection;
				} else if (line[0] == '\t') {
					curSection[lastKey.Trim()] += "\n" + line.Substring(1);
				} else {
					string key, val;
					line.Separate('=', out key, out val);
					curSection[key.Trim()] = val.Trim();
					lastKey = key;
				}
			}
		}
		return meta;
	}
	
	
	static void ReadFully(Stream s, byte[] data, int count) {
		int offset = 0;
		while (count > 0) {
			int read = s.Read(data, offset, count);
			
			if (read == 0) throw new EndOfStreamException("End of stream reading data");
			offset += read; count -= read;
		}
	}
	
	
	static void ImportPortals(MetaFile file, Level lvl) {
		MetaSection section;
		if (!file.TryGetValue("teleports", out section)) return;
		ushort x, y, z;
		
		foreach (var kvp in section)
		{
			int idx = int.Parse(kvp.Key);
			lvl.IntToPos(idx, out x, out y, out z);
			
			string[] args = kvp.Value.Split(',');
			Portal.Set(lvl.name, x, y, z,
			           ushort.Parse(args[1]),
			           ushort.Parse(args[2]),
			           ushort.Parse(args[3]),
			           args[0]);
			lvl.blocks[idx] = MapPortal(lvl.blocks[idx]);
		}
	}
	
	static byte MapPortal(byte b) {
		switch (b)
		{
				case Block.Air: return Block.Portal_Air;
				case Block.Water: return Block.Portal_Water;
				case Block.StillWater: return Block.Portal_Water;
				case Block.Lava: return Block.Portal_Lava;
				case Block.StillLava: return Block.Portal_Lava;
		}
		return Block.Portal_Blue;
	}
	
	static void ImportMessages(MetaFile file, Level lvl) {
		MetaSection section;
		if (!file.TryGetValue("messages", out section)) return;
		ushort x, y, z;
		
		foreach (var kvp in section)
		{
			int idx = int.Parse(kvp.Key);
			lvl.IntToPos(idx, out x, out y, out z);
			
			MessageBlock.Set(lvl.name, x, y, z, kvp.Value);
			lvl.blocks[idx] = MapMB(lvl.blocks[idx]);
		}
	}
	
	static byte MapMB(byte b) {
		switch (b)
		{
				case Block.Air: return Block.MB_Air;
				case Block.Water: return Block.MB_Water;
				case Block.StillWater: return Block.MB_Water;
				case Block.Lava: return Block.MB_Lava;
				case Block.StillLava: return Block.MB_Lava;
		}
		return Block.MB_White;
	}
}
