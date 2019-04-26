using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MCGalaxy;
using MCGalaxy.Blocks;
using MCGalaxy.Levels.IO;
using MCGalaxy.Maths;
using BlockID = System.UInt16;
using AttribsDict = System.Collections.Generic.Dictionary<string, string>;

namespace PluginMagicaVoxelImport {
	public sealed class Core : Plugin_Simple {
		public override string creator { get { return "UnknownShadow200"; } }
		public override string name { get { return "MagicaVoxelImport"; } }
		public override string MCGalaxy_Version { get { return "1.9.1.0"; } }
		
		IMapImporter importer;
		public override void Load(bool startup) {
			importer = new VoxImporter();
			IMapImporter.Formats.Add(importer);
		}
		
		public override void Unload(bool shutdown) {
			IMapImporter.Formats.Remove(importer);
		}
	}

	public sealed class VoxImporter : IMapImporter {

		public override string Extension { get { return ".vox"; } }
		public override string Description { get { return "MagicaVoxel map"; } }

		public override Vec3U16 ReadDimensions(Stream src) {
			throw new NotSupportedException();
		}
		
		
		// data reading stuff
		struct Chunk {
			public string FourCC;
			public int ChunkContentSize;
			public int ChildChunkContentSize;
		}
		
		static Chunk ReadChunk(BinaryReader reader) {
			Chunk c;
			c.FourCC = ReadFourCC(reader);
			c.ChunkContentSize = reader.ReadInt32();
			c.ChildChunkContentSize = reader.ReadInt32();
			return c;
		}
		
		static string ReadFourCC(BinaryReader reader) {
			byte[] bytes = reader.ReadBytes(4);
			return Encoding.ASCII.GetString(bytes);
		}
		
		static void ReadNode(BinaryReader reader, Node node) {
			node.Id      = reader.ReadInt32();
			node.Attribs = ReadDict(reader);
		}
		
		static string ReadString(BinaryReader reader) {
			int len = reader.ReadInt32();
			byte[] bytes = reader.ReadBytes(len);
			return Encoding.ASCII.GetString(bytes);
		}
		
		static AttribsDict ReadDict(BinaryReader reader) {
			AttribsDict dict = new AttribsDict();
			int count = reader.ReadInt32();
			
			for (int i = 0; i < count; i++) {
				string key = ReadString(reader);
				string val = ReadString(reader);
				dict.Add(key, val);
			}
			return dict;
		}
		
		// actual decoding stuff
		public override Level Read(Stream src, string name, bool metadata) {
			BinaryReader reader = new BinaryReader(src);
			if (ReadFourCC(reader) != "VOX ")
				throw new NotSupportedException("Invalid header");
			if (reader.ReadInt32() != 150)
				throw new NotSupportedException("Unsupported version number");
			
			Chunk main = ReadChunk(reader);
			if (main.FourCC != "MAIN")
				throw new NotSupportedException("MAIN chunk expected" );
			
			uint[] palette = new uint[256] {
				0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff, 0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
				0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff, 0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
				0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc, 0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
				0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc, 0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
				0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc, 0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
				0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999, 0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
				0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099, 0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
				0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66, 0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
				0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366, 0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
				0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33, 0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
				0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633, 0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
				0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00, 0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
				0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600, 0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
				0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000, 0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
				0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700, 0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
				0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd, 0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111,
			};
			
			src.Seek(main.ChunkContentSize, SeekOrigin.Current);
			long end = src.Position + main.ChildChunkContentSize;
			Level lvl = null;
			List<Model> models = new List<Model>();
			Dictionary<int, Node> nodes = new Dictionary<int, Node>();
			
			while (src.Position < end) {
				Chunk chunk = ReadChunk(reader);
				Server.s.Log(chunk.FourCC + " - " + chunk.ChunkContentSize);
				
				if (chunk.FourCC == "RGBA") {
					if (chunk.ChunkContentSize != 4 * 256)
						throw new NotSupportedException("RGBA chunk must be 1024 bytes");
					for (int i = 0; i <= 254; i++) {
						palette[i + 1] = reader.ReadUInt32();
					}
					reader.ReadUInt32();
				} else if (chunk.FourCC == "SIZE") {
					if (chunk.ChunkContentSize != 12)
						throw new NotSupportedException("Size chunk must be 12 bytes");
					Model part = new Model();
					
					// voxel offsets are from centre of model
					part.SizeX = reader.ReadInt32(); part.X = -part.SizeX / 2;
					part.SizeY = reader.ReadInt32(); part.Y = -part.SizeY / 2;
					part.SizeZ = reader.ReadInt32(); part.Z = -part.SizeZ / 2;
					models.Add(part);
				} else if (chunk.FourCC == "XYZI") {
					int numVoxels = reader.ReadInt32();
					Model part = models[models.Count - 1];
					part.Voxels = new Voxel[numVoxels];
					
					for (int i = 0; i < numVoxels; i++) {
						part.Voxels[i].X   = reader.ReadByte();
						part.Voxels[i].Z   = reader.ReadByte();
						part.Voxels[i].Y   = reader.ReadByte();
						part.Voxels[i].Idx = reader.ReadByte();
					}
				} else if (chunk.FourCC == "nTRN") {
					TransformNode node = new TransformNode();
					node.Type = NodeType.Transform;
					ReadNode(reader, node);
					node.Children = new int[1];
					
					node.Children[0] = reader.ReadInt32();
					int reservedID = reader.ReadInt32();
					int layerID    = reader.ReadInt32();
					int numFrames  = reader.ReadInt32();
					
					if (numFrames != 1) throw new NotSupportedException("Transform node must only have one frame");
					node.FrameAttribs = ReadDict(reader);
					nodes[node.Id] = node;
				} else if (chunk.FourCC == "nGRP") {
					GroupNode node = new GroupNode();
					node.Type = NodeType.Group;
					ReadNode(reader, node);
					node.Children = new int[reader.ReadInt32()];
					
					for (int i = 0; i < node.Children.Length; i++) {
						node.Children[i] = reader.ReadInt32();
					}
					nodes[node.Id] = node;
				} else if (chunk.FourCC == "nSHP") {
					ShapeNode node = new ShapeNode();
					node.Type = NodeType.Shape;
					ReadNode(reader, node);
					node.Children = new int[0];
					
					int numModels  = reader.ReadInt32();
					if (numModels != 1) throw new NotSupportedException("Shape node must only have one model");

					int modelID = reader.ReadInt32();
					node.Model = models[modelID];
					node.ModelAttribs = ReadDict(reader);
					nodes[node.Id] = node;
				} else {
					src.Seek(chunk.ChunkContentSize, SeekOrigin.Current);
				}
				src.Seek(chunk.ChildChunkContentSize, SeekOrigin.Current);
			}
			
			// assume root node is 0
			Transform(0, "", Vec3S32.Zero, nodes);
			
			int minX = 0, maxX = 0, minY = 0, maxY = 0, minZ = 0, maxZ = 0;
			foreach (Model m in models) {
				minX = Math.Min(minX, m.X); maxX = Math.Max(maxX, m.X + (m.SizeX - 1));
				minY = Math.Min(minY, m.Y); maxY = Math.Max(maxY, m.Y + (m.SizeY - 1));
				minZ = Math.Min(minZ, m.Z); maxZ = Math.Max(maxZ, m.Z + (m.SizeZ - 1));
			}
			
			// magicavox uses Z for vertical
			int width = (maxX - minX) + 1, length = (maxY - minY) + 1, height = (maxZ - minZ) + 1, 
			lvl = new Level(name, (ushort)width, (ushort)height, (ushort)length);
			
			ComposeParts(lvl, models, minX, minY, minZ);
			ComposePalette(lvl, palette);
			return lvl;
		}
		
		static void Transform(int id, string indent, Vec3S32 offset, Dictionary<int, Node> nodes) {
			Node node = nodes[id];
			Server.s.Log(indent + node.Type + " = " + offset.X + ", " + offset.Y + ", " + offset.Z);
			switch (node.Type) {
					// traverse the multiple children nodes
				case NodeType.Group:
					for (int i = 0; i < node.Children.Length; i++) {
						Transform(node.Children[i], indent + "   ", offset, nodes);
					}
					break;
					
					// transform might or might not change parent transformation
				case NodeType.Transform:
					TransformNode trans = (TransformNode)node;
					string raw;
					
					if (trans.FrameAttribs.TryGetValue("_t", out raw)) {
						string[] bits = raw.SplitSpaces(3);
						offset.X += int.Parse(bits[0]);
						offset.Y += int.Parse(bits[1]);
						offset.Z += int.Parse(bits[2]);
					}
					if (trans.FrameAttribs.TryGetValue("_r", out raw)) {
						byte flags = byte.Parse(raw);
						Vec3S32 X = Vec3S32.Zero, Y = Vec3S32.Zero, Z = Vec3S32.Zero;
						
						
						//unsigned char _r = (1 << 0) | (2 << 2) | (0 << 4) | (1 << 5) | (1 << 6)
					}
					Transform(node.Children[0], indent + "   ", offset, nodes);
					break;
					
					// apply transformation to model
				case NodeType.Shape:
					ShapeNode shape = (ShapeNode)node;
					shape.Model.X += offset.X;
					shape.Model.Y += offset.Y;
					shape.Model.Z += offset.Z;
					break;
			}
		}
		
		static void ComposeParts(Level lvl, List<Model> parts, int minX, int minY, int minZ) {
			foreach (Model part in parts) {
				for (int i = 0; i < part.Voxels.Length; i++) {
					Voxel v  = part.Voxels[i];
					int x = v.X, y = v.Z, z = v.Y; // need to switch here for whatever reason
					byte idx = v.Idx;		
					
					x += part.X - minX;
					y += part.Y - minY;
					z += part.Z - minZ;

					if (idx < Block.CpeCount) {
						lvl.SetTile((ushort)x, (ushort)z, (ushort)y, idx);
					} else {
						lvl.SetTile((ushort)x, (ushort)z, (ushort)y, Block.custom_block);
						lvl.SetExtTile((ushort)x, (ushort)z, (ushort)y, idx);
					}
				}
			}
		}
		
		static void ComposePalette(Level lvl, uint[] palette) {
			for (int i = 1; i <= 255; i++) {
				BlockDefinition def = new BlockDefinition();
				def.RawID = (BlockID)i;
				def.CollideType = CollideType.Solid;
				def.BlocksLight = false;
				def.Speed = 1.0f;
				def.BlockDraw = DrawType.Opaque;
				def.Shape = 16;
				
				def.FogR = (byte)(palette[i] >>  0);
				def.FogG = (byte)(palette[i] >>  8);
				def.FogB = (byte)(palette[i] >> 16);
				def.Name = palette[i].ToString("X8") + "#";
				def.MaxX = 16; def.MaxZ = 16; def.MaxY = 16;
				lvl.UpdateCustomBlock(def.RawID, def);
			}
			BlockDefinition.Save(false, lvl);
			lvl.Config.EdgeLevel = 0;
			lvl.Config.Terrain = "https://i.imgur.com/kuuDIkw.png";
		}
		
		struct Voxel { public byte X, Y, Z, Idx; }
		class Model {
			public int SizeX, SizeY, SizeZ;
			public int X, Y, Z;
			public Voxel[] Voxels;
		}
		
		enum NodeType { Transform, Group, Shape };
		class Node {
			public NodeType Type;
			public int Id;
			public AttribsDict Attribs;
			public int[] Children;
		}
		
		class TransformNode : Node {
			// NOTE: Spec says must be 1 frame
			public AttribsDict FrameAttribs;
		}
		
		class GroupNode : Node { }
		
		class ShapeNode : Node {
			// NOTE: Spec says must be 1 model
			public Model Model;
			public AttribsDict ModelAttribs;
		}
	}
}
