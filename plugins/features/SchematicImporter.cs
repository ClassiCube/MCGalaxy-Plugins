//reference System.dll
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using fNbt;
using MCGalaxy;
using MCGalaxy.Levels.IO;
using MCGalaxy.Maths;
using BlockID = System.UInt16;

namespace PluginSchematicImport
{
	public sealed class SchematicImportPlugin : Plugin 
	{
		public override string MCGalaxy_Version { get { return "1.9.2.4"; } }
		public override string name { get { return "SchematicImportPlugin"; } }
		IMapImporter schem;
		
		public override void Load(bool startup) {
			schem = new SchematicImporter();
			IMapImporter.Formats.Add(schem);
		}
		
		public override void Unload(bool shutdown) {
			IMapImporter.Formats.Remove(schem);
		}
	}
	
	sealed class SchematicImporter : IMapImporter {
		public override string Extension { get { return ".schematic"; } }
		public override string Description { get { return "Minecraft Schematic"; } }

		public override Vec3U16 ReadDimensions(Stream src) {
			throw new NotSupportedException();
		}

		public override Level Read(Stream src, string name, bool metadata) {
			NbtFile file = new NbtFile();
			file.LoadFromStream(src);
			NbtCompound root = file.RootTag;
			
			byte[] raw  = root["Blocks"].ByteArrayValue;
			byte[] meta = root["Data"].ByteArrayValue;
			int width   = root["Width" ].ShortValue;
			int height  = root["Height"].ShortValue;
			int length  = root["Length"].ShortValue;
			
			Level lvl     = new Level(name, (ushort)width, (ushort)height, (ushort)length);
			byte[] blocks = lvl.blocks;
			for (int i = 0; i < blocks.Length; i++) {
				blocks[i] = (byte)mcConv[raw[i], meta[i] & 0x0F];
			}
			
			for (int i = 0; i < blocks.Length; i++) {
				byte block = blocks[i];
				if (block < Block.CPE_COUNT) continue;
				blocks[i] = Block.custom_block;
				
				ushort x, y, z;
				lvl.IntToPos(i, out x, out y, out z);
				lvl.FastSetExtTile(x, y, z, block);
			}
			return lvl;
		}
		
		static byte[,] mcConv = new byte[256, 16];		
		static SchematicImporter() {
			for (int i = 0; i < 256; i++)
				for(int j = 0; j < 16; j++)
					mcConv[i, j] = Block.Magma;
			
			SetAll( 0, Block.Air);
			SetAll( 1, Block.Stone); // S
			SetAll( 2, Block.Grass);
			SetAll( 3, Block.Dirt); // S
			SetAll( 4, Block.Cobblestone);
			SetAll( 5, Block.Wood); // S
			SetAll( 6, Block.Sapling); // S
			SetAll( 7, Block.Bedrock);
			SetAll( 8, Block.Water);
			SetAll( 9, Block.Water);
			SetAll(10, Block.Lava);
			SetAll(11, Block.Lava);
			SetAll(12, Block.Sand);
			SetAll(13, Block.Gravel);
			SetAll(14, Block.GoldOre);
			SetAll(15, Block.IronOre);
			SetAll(16, Block.CoalOre);
			SetAll(17, Block.Wood); // S
			SetAll(18, Block.Leaves); // S
			SetAll(19, Block.Sponge);
			SetAll(20, Block.Glass);
			
			SetAll(24, Block.Sandstone);
			
			SetAll(37, Block.Dandelion);
			SetAll(38, Block.Rose); // S
			SetAll(39, Block.Mushroom);
			SetAll(40, Block.RedMushroom);
			SetAll(41, Block.Gold);
			SetAll(42, Block.Iron);
			SetAll(43, Block.DoubleSlab); // S
			SetAll(44, Block.Slab); // S
			SetAll(45, Block.Brick);
			SetAll(46, Block.TNT);
			SetAll(47, Block.Bookshelf);
			
			SetAll(48, Block.MossyRocks);
			SetAll(49, Block.Obsidian);
			
			SetAll(51, Block.Fire);
			
			SetAll(78, Block.Snow);
			SetAll(79, Block.Ice);
			
			SetAll(98, Block.StoneBrick ); // S
			
			mcConv[35,  0] = Block.White;
			mcConv[35,  1] = Block.Orange;
			mcConv[35,  2] = Block.Violet;
			mcConv[35,  3] = Block.Cyan;
			mcConv[35,  4] = Block.Yellow;
			mcConv[35,  5] = Block.Green;
			mcConv[35,  6] = Block.LightPink;
			mcConv[35,  7] = Block.Black;
			mcConv[35,  8] = Block.Gray;
			mcConv[35,  9] = Block.Turquoise;
			mcConv[35, 10] = Block.Indigo;
			mcConv[35, 11] = Block.DeepBlue;
			mcConv[35, 12] = Block.Brown;
			mcConv[35, 13] = Block.ForestGreen;
			mcConv[35, 14] = Block.Red;
			mcConv[35, 15] = Block.Obsidian;
		}

		static void SetAll( int mcID, byte ccID ) {
			for (int i = 0; i < 16; i++)
				mcConv[mcID, i] = ccID;
		}
	}
}
