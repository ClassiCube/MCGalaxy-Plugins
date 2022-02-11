//reference System.dll
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using MCGalaxy;
using MCGalaxy.Levels.IO;
// Credit to https://github.com/NickstaDB/SerializationDumper 
//  as that program was majorly helpful for validating exported output

namespace CommandExportDat 
{
	sealed class DatWriter
	{
		public BinaryWriter dst;
		public void WriteBytes(byte[] data) {
			dst.Write(data);
		}
		
		public void WriteUInt8(byte value) {
			dst.Write(value);
		}
		public void WriteInt16(short value) {
			dst.Write(IPAddress.HostToNetworkOrder(value));
		}
		
		public void WriteUInt16(ushort value) {
			dst.Write(IPAddress.HostToNetworkOrder((short)value));
		}
		
		public void WriteInt32(int value) {
			dst.Write(IPAddress.HostToNetworkOrder(value));
		}
		
		public void WriteUtf8(string value) {
			WriteUInt16((ushort)value.Length);
			WriteBytes(Encoding.UTF8.GetBytes(value));
		}
	}
	
	sealed class DatExporter : IMapExporter
	{
		public override string Extension { get { return ".dat"; } }
		
		const int DAT_SIGNATURE = 0x271BB788;
		const byte DAT_VERSION  = 0x02;
		
		const ushort JSF_SIGNATURE = 0xACED;
		const ushort JSF_VERSION   = 0x0005;
		const byte SC_SERIALIZABLE = 0x02;
		
		const byte TC_NULL      = 0x70;
		const byte TC_OBJECT    = 0x73;
		const byte TC_CLASSDESC = 0x72;
		const byte TC_STRING    = 0x74;
		const byte TC_ARRAY     = 0x75;
		const byte TC_ENDOFBLOCKDATA = 0x78;

		public override void Write(Stream dst, Level lvl)
		{
			JField[] fields = GetFields(lvl);
			
			using (Stream s = new GZipStream(dst, CompressionMode.Compress))
			{
				DatWriter w = new DatWriter();
				w.dst = new BinaryWriter(s);
				
				w.WriteInt32(DAT_SIGNATURE);
				w.WriteUInt8(DAT_VERSION);
				
				w.WriteUInt16(JSF_SIGNATURE);
				w.WriteUInt16(JSF_VERSION);
				
				// Write serialised Level Object
				w.WriteUInt8(TC_OBJECT);
				WriteClassDesc(w, "com.mojang.minecraft.level.Level", fields);
				foreach (JField field in fields) // classData
					WriteValue(w, field);
			}
		}
		
		void WriteClassDesc(DatWriter w, string klass, JField[] fields)
		{
			// Write serialised Level Object
			w.WriteUInt8(TC_CLASSDESC);
			w.WriteUtf8(klass);
			w.WriteInt32(0); w.WriteInt32(0); // serialUUID
			w.WriteUInt8(SC_SERIALIZABLE); // SC_SERIALIZABLE
			
			w.WriteUInt16((ushort)fields.Length);
			foreach (JField field in fields)
				WriteField(w, field);
			
			w.WriteUInt8(TC_ENDOFBLOCKDATA); // classAnnotations
			w.WriteUInt8(TC_NULL); // superClassDesc
		}
		
		void WriteField(DatWriter w, JField field)
		{
			w.WriteUInt8((byte)field.type);
			w.WriteUtf8(field.field);
			
			if (field.klass == null) return;
			w.WriteUInt8(TC_STRING);
			w.WriteUtf8(field.klass);
		}
		
		void WriteValue(DatWriter w, JField field)
		{
			switch (field.type)
			{
				case 'I':
					w.WriteInt32((int)field.value);
					break;
				case '[':
					w.WriteUInt8(TC_ARRAY);
					WriteClassDesc(w, "[B", new JField[0]);
					w.WriteInt32(((byte[])field.value).Length);
					w.WriteBytes((byte[])field.value);
					break;
				default:
					throw new InvalidOperationException("Can't write fields of type " + field.type);
			}
		}
		
		JField[] GetFields(Level level)
		{
			// yes height/depth are swapped intentionally
			return new JField[]
			{
				new JField("width",  level.Width),
				new JField("depth",  level.Height),
				new JField("height", level.Length),
				new JField("xSpawn", level.spawnx),
				new JField("ySpawn", level.spawny),
				new JField("zSpawn", level.spawnz),
				//new JField("skyColor",   0xFF0000),
				//new JField("fogColor",   0x00FF00),
				//new JField("cloudColor", 0x0000FF),
				new JField("blocks", level.blocks),
			};
		}
		
		class JField
		{
			public string field, klass;
			public char type;
			public object value;
			
			public JField(string field, int value)
			{
				this.field = field;
				this.type  = 'I';
				this.value = value;
			}
			
			public JField(string field, byte[] value)
			{
				this.field = field;
				this.type  = '[';
				this.value = value;
				this.klass = "[B";
			}
		}
	}

	public class CmdExportDat : Command2
	{
		public override string name { get { return "ExportDat"; } }
		public override string type { get { return CommandTypes.World; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
		
		public override void Use(Player p, string message, CommandData data) {
			Directory.CreateDirectory("extra/dat");
			string path = "extra/dat/" + p.level.name + ".dat";
			
			DatExporter exporter = new DatExporter();
			exporter.Write(path, p.level);
			p.Message("Saved {0} &Sto {1}", p.level.ColoredName, path);
		}
		
		public override void Help(Player p) {
			p.Message("&T/ExportDat");
			p.Message("&HSaves current level to the /extra/dat/ folder as a .dat file");
		}
	}
}
