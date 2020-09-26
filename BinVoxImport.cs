using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MCGalaxy;
using MCGalaxy.Levels.IO;
using MCGalaxy.Maths;

namespace PluginBinVoxelImport {
	public sealed class Core : Plugin_Simple {
		public override string creator { get { return "UnknownShadow200"; } }
		public override string name { get { return "BinVoxelImport"; } }
		public override string MCGalaxy_Version { get { return "1.9.0.9"; } }
		
		IMapImporter importer;
		public override void Load(bool startup) {
			importer = new BinVoxImporter();
			IMapImporter.Formats.Add(importer);
		}
		
		public override void Unload(bool shutdown) {
			IMapImporter.Formats.Remove(importer);
		}
	}
    public sealed class BinVoxImporter : IMapImporter {

        public override string Extension { get { return ".binvox"; } }
        public override string Description { get { return "Binvox map"; } }
        
        public override Vec3U16 ReadDimensions(Stream src) {
            BinaryReader reader = new BinaryReader(src);
            return ReadHeader(reader);
        }
        
        public override Level Read(Stream src, string name, bool metadata) {
            BinaryReader reader = new BinaryReader(src);
            Vec3U16 dims = ReadHeader(reader);
            Level lvl = new Level(name, dims.X, dims.Y, dims.Z);
            lvl.Config.EdgeLevel = 0;

            byte[] blocks = lvl.blocks;
            int size = dims.X * dims.Y * dims.Z, i = 0;
            while (i < size) {
                byte value = reader.ReadByte(), count = reader.ReadByte();
                if (value == Block.Air) { i += count; continue; } // skip redundantly changing air
                
                for (int j = 0; j < count; j++) {
                    int index = i + j;
                    int x = (index / dims.Y) / dims.Z; // need to reorder from X Z Y to Y Z X
                    int y = (index / dims.Y) % dims.Z;
                    int z = index % dims.Z;
                    blocks[(y * dims.Z + z) * dims.X + x] = value;
                }
                i += count;
            }
            return lvl;
        }
        
        // http://www.patrickmin.com/binvox/binvox.html
        // http://www.patrickmin.com/binvox/ReadBinvox.java
        static Vec3U16 ReadHeader(BinaryReader reader) {
            Vec3U16 dims = default(Vec3U16);
            while (reader.BaseStream.Position < reader.BaseStream.Length) {
                string line = ReadString(reader);
                if (line.CaselessEq("data")) break;
                if (!line.CaselessStarts("dim ")) continue;
                
                string[] parts = line.SplitSpaces(4);
                dims.Z = ushort.Parse(parts[1]);
                dims.Y = ushort.Parse(parts[2]);
                dims.X = ushort.Parse(parts[3]);
            }
            return dims;
        }

        static string ReadString(BinaryReader reader) {
            List<byte> buffer = new List<byte>();
            
            byte value = 0;
            do {
                value = reader.ReadByte();
                buffer.Add(value);
            } while (value != '\n');
            
            return Encoding.ASCII.GetString(buffer.ToArray()).Trim('\r', '\n');
        }
    }
}