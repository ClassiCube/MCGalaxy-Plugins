//reference System.dll
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using MCGalaxy;
using MCGalaxy.Network;
using System.Runtime.InteropServices;
using System.Text;
using BlockID = System.UInt16;

namespace PluginAlphaIndev
{
	public sealed class AlphaIndevPlugin : Plugin
	{
		public override string name { get { return "AlphaIndev"; } }
		public override string MCGalaxy_Version { get { return "1.9.4.0"; } }
		ProtocolConstructor oldCons;
        const byte OPCODE_HANDSHAKE = AlphaIndevProtocol.OPCODE_HANDSHAKE;


        public override void Load(bool startup) {
			oldCons = INetSocket.Protocols[OPCODE_HANDSHAKE];

			INetSocket.Protocols[OPCODE_HANDSHAKE] = ConstructClassic;
		}

		public override void Unload(bool shutdown) {
			// restore original protocol constructor
			INetSocket.Protocols[OPCODE_HANDSHAKE] = oldCons;
		}

		static INetProtocol ConstructClassic(INetSocket socket) {
			return new AlphaIndevHandshake(socket);
		}
	}
	
    // Handshake parsing is tricky since need to support Indev, Alpha, and Beta
    unsafe class AlphaIndevHandshake : INetProtocol
    {
        const byte OPCODE_HANDSHAKE = AlphaIndevProtocol.OPCODE_HANDSHAKE;
        const byte OPCODE_LOGIN     = AlphaIndevProtocol.OPCODE_LOGIN;

        AlphaIndevParser parser;
        INetSocket socket;
        string player;

        public AlphaIndevHandshake(INetSocket s) { socket = s; }   
        public void Disconnect() { }

        public int ProcessReceived(byte[] buffer, int length) {
            if (length < 4) return 0;

            switch (buffer[0]) {
                case OPCODE_LOGIN:     return HandleLogin(buffer, length);
                case OPCODE_HANDSHAKE: return HandleHandshake(buffer, length);

                default:
                    Logger.Log(LogType.SystemActivity, "I/A/B player {0} sent unknown opcode {1} in handshake", player, buffer[0]);
                    socket.Close();
                    return length;
            }
        }

        static readonly byte[] handshake_fields = { AlphaIndevParser.FIELD_BYTE, AlphaIndevParser.FIELD_STRING };
        protected int HandleHandshake(byte[] buffer, int length) {
            // handshake packet: u8 opcode, u16 str_len, u8* str_contents
            // utf8 or unicode is used for strings depending on protocol
            //   e.g. utf8: 0x02 0x00 0x01 'A'
            //   e.g. uni:  0x02 0x00 0x01 0x00 'A'
            parser      = new AlphaIndevParser();
            parser.utf8 = buffer[3] != 0;

            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, 0, length, handshake_fields, values);
            if (length < size) return 0;

            player = parser.ReadString(buffer, 1);
            Logger.Log(LogType.SystemActivity, "I/A/B USER: " + player);

            // - for no name name verification
            SendHandshake("-");
            return size;
        }

        void SendHandshake(string serverID) {
            int dataLen = 1 + 2 + parser.CalcStringLength(serverID);
            byte[] data = new byte[dataLen];

            data[0] = OPCODE_HANDSHAKE;
            parser.WriteString(data, 1, serverID);
            socket.Send(data, SendFlags.None);
        }

        static readonly byte[] login_fields = { AlphaIndevParser.FIELD_BYTE, AlphaIndevParser.FIELD_INT };
        int HandleLogin(byte[] buffer, int length) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, 0, length, login_fields, values);
            if (length < size) return 0;

            int version = values[1].I32;
            if (!parser.utf8 && version == IndevProtocol.PROTOCOL_VERSION) { 
                socket.protocol = new IndevProtocol(socket, player, parser.utf8);
            } else {
                socket.protocol = new AlphaProtocol(socket, player, parser.utf8);
            }

            return socket.protocol.ProcessReceived(buffer, length);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct FieldValue
    {
        [FieldOffset(0)]
        public byte U8;
        [FieldOffset(0)]
        public ushort U16;
        [FieldOffset(0)]
        public int I32;
        [FieldOffset(0)]
        public long I64;
        [FieldOffset(0)]
        public float F32;
        [FieldOffset(0)]
        public double F64;
    }
    
    unsafe class AlphaIndevParser
    {
        public bool utf8;
        public Encoding CurEncoding { get { return utf8 ? Encoding.UTF8 : Encoding.BigEndianUnicode; } }

        public static ushort ReadU16(byte[] array, int index) {
            return NetUtils.ReadU16(array, index);
        }

        public static int ReadI32(byte[] array, int index) {
            return NetUtils.ReadI32(array, index);
        }

        public static float ReadF32(byte[] array, int offset) {
            int value = ReadI32(array, offset);
            return *(float*)&value;
        }

        public static double ReadF64(byte[] array, int offset) {
            long hi = ReadI32(array, offset + 0) & 0xFFFFFFFFL;
            long lo = ReadI32(array, offset + 4) & 0xFFFFFFFFL;

            long value = (hi << 32) | lo;
            return *(double*)&value;
        }

        public int ReadStringLength(byte[] buffer, int offset) {
            int len = ReadU16(buffer, offset);
            // Just to confuse you, 'len' isn't always a byte count
            //   utf8 = number of bytes
            //   uni  = number of characters
            return utf8 ? len : (len * 2);
        }

        public string ReadString(byte[] buffer, int offset) {
            int len = ReadStringLength(buffer, offset);
            return CurEncoding.GetString(buffer, offset + 2, len);
        }


        static void WriteU16(ushort value, byte[] array, int index) {
            NetUtils.WriteU16(value, array, index);
        }

        public int CalcStringLength(string value) { return CurEncoding.GetByteCount(value); }
        public void WriteString(byte[] buffer, int offset, string value) {
            int len = CurEncoding.GetBytes(value, 0, value.Length, buffer, offset + 2);

            // Just to confuse you, 'len' isn't always a byte count
            //   utf8 = number of bytes
            //   uni  = number of characters
            if (!utf8) len >>= 1;
            WriteU16((ushort)len, buffer, offset);
        }
        
        
        public const byte FIELD_BYTE   = 0;
        public const byte FIELD_SHORT  = 1;
        public const byte FIELD_INT    = 2;
        public const byte FIELD_FLOAT  = 3;
        public const byte FIELD_DOUBLE = 4;
        public const byte FIELD_STRING = 5;

        static bool CheckFieldSize(int amount, ref int offset, ref int left) {
        	if (left < amount) return false;
        			
        	offset += amount; 
        	left   -= amount;
        	return true;
        }
        
        public int ParsePacket(byte[] buffer, int offset, int left, 
                               byte[] fields, FieldValue* values) {
        	int total = left;
        	
        	foreach (byte field in fields)
        	{
        		if (field == FIELD_BYTE) {
        			if (!CheckFieldSize(1, ref offset, ref left)) return int.MaxValue;
                    values->U8 = buffer[offset - 1]; 
                    values++;

        		} else if (field == FIELD_SHORT) {
        			if (!CheckFieldSize(2, ref offset, ref left)) return int.MaxValue;
                    values->U16 = ReadU16(buffer, offset - 2); 
                    values++;

                } else if (field == FIELD_INT) {
        			if (!CheckFieldSize(4, ref offset, ref left)) return int.MaxValue;
                    values->I32 = ReadI32(buffer, offset - 4);
                    values++;

                } else if (field == FIELD_FLOAT) {
        			if (!CheckFieldSize(4, ref offset, ref left)) return int.MaxValue;
                    values->F32 = ReadF32(buffer, offset - 4);
                    values++;

                } else if (field == FIELD_DOUBLE) {
        			if (!CheckFieldSize(8, ref offset, ref left)) return int.MaxValue;
                    values->F64 = ReadF64(buffer, offset - 8);
                    values++;

                } else if (field == FIELD_STRING) {
        			if (!CheckFieldSize(2, ref offset, ref left)) return int.MaxValue;
        			int strLen = ReadStringLength(buffer, offset - 2);

                    values->I32 = offset - 2;
                    values++;
                    if (!CheckFieldSize(strLen, ref offset, ref left)) return int.MaxValue;
                }
        	}
        	return total - left;
        }
    }

    unsafe abstract class AlphaIndevProtocol : IGameSession, INetProtocol
    {
        protected AlphaIndevParser parser;
        public AlphaIndevProtocol(INetSocket s, string name, bool utf8) {
            socket = s;
            player = new Player(s, this);
            parser = new AlphaIndevParser();

            parser.utf8 = utf8;
            // TEMP HACK
            player.name = name; player.truename = name;
        }


        public override void SendAddTabEntry(byte id, string name, string nick, string group, byte groupRank) { throw new NotImplementedException(); }
        public override void SendRemoveTabEntry(byte id) { throw new NotImplementedException(); }
        public override bool SendSetUserType(byte type) { return false; }
        public override bool SendSetReach(float reach) { return false; }
        public override bool SendHoldThis(BlockID block, bool locked) { return false; }
        public override bool SendSetEnvColor(byte type, string hex) { return false; }
        public override void SendChangeModel(byte id, string model) { }
        public override bool SendSetWeather(byte weather) { return false; }
        public override bool SendSetTextColor(ColorDesc color) { return false; }
        public override bool SendDefineBlock(BlockDefinition def) { return false; }
        public override bool SendUndefineBlock(BlockDefinition def) { return false; }

        public const int OPCODE_PING      = 0x00;
        public const int OPCODE_LOGIN     = 0x01;
        public const int OPCODE_HANDSHAKE = 0x02;
        public const int OPCODE_CHAT      = 0x03;
        public const int OPCODE_SPAWN_POSITION = 0x06;

        public const int OPCODE_SELF_STATEONLY = 0x0A;
        public const int OPCODE_SELF_MOVE      = 0x0B;
        public const int OPCODE_SELF_LOOK      = 0x0C;
        public const int OPCODE_SELF_MOVE_LOOK = 0x0D;
        public const int OPCODE_BLOCK_DIG      = 0x0E;
        public const int OPCODE_BLOCK_PLACE    = 0x0F;
        
        public const int OPCODE_ARM_ANIM  = 0x12;
        public const int OPCODE_NAMED_ADD = 0x14;
        public const int OPCODE_REMOVE_ENTITY = 0x1D;
        public const int OPCODE_REL_MOVE  = 0x1F;
        public const int OPCODE_LOOK      = 0x20;
        public const int OPCODE_REL_MOVE_LOOK = 0x21;
        public const int OPCODE_TELEPORT  = 0x22;
        public const int OPCODE_PRE_CHUNK = 0x32;
        public const int OPCODE_CHUNK     = 0x33;
        public const int OPCODE_BLOCK_CHANGE = 0x35;


        public const int OPCODE_KICK = 0xFF;

        public override bool Supports(string extName, int version) { return false; }


        protected static void WriteU16(ushort value, byte[] array, int index) {
            NetUtils.WriteU16(value, array, index);
        }

        protected static void WriteI32(int value, byte[] array, int index) {
            NetUtils.WriteI32(value, array, index);
        }

        protected static void WriteF32(float value, byte[] buffer, int offset) {
            int num = *(int*)&value;
            WriteI32(num, buffer, offset + 0);
        }

        protected static void WriteF64(double value, byte[] buffer, int offset) {
            long num = *(long*)&value;
            WriteI32((int)(num >> 32), buffer, offset + 0);
            WriteI32((int)(num >>  0), buffer, offset + 4);
        }
        
        
        public const byte FIELD_BYTE   = AlphaIndevParser.FIELD_BYTE;
        public const byte FIELD_SHORT  = AlphaIndevParser.FIELD_SHORT;
        public const byte FIELD_INT    = AlphaIndevParser.FIELD_INT;
        public const byte FIELD_FLOAT  = AlphaIndevParser.FIELD_FLOAT;
        public const byte FIELD_DOUBLE = AlphaIndevParser.FIELD_DOUBLE;
        public const byte FIELD_STRING = AlphaIndevParser.FIELD_STRING;

        protected abstract int CalcStringLength(string value);
        protected abstract void WriteString(byte[] buffer, int offset, string value);

        protected static string CleanupColors(string value) {
            return LineWrapper.CleanupColors(value, false, false);
        }


#region Packet senders
        public override void SendPing() {
            Send(new byte[] { OPCODE_PING });
        }

        public override void SendSetSpawnpoint(Position pos, Orientation rot) {
            byte[] spawn = new byte[1 + 4 + 4 + 4];
            spawn[0] = OPCODE_SPAWN_POSITION;
            NetUtils.WriteI32(pos.BlockX, spawn, 1);
            NetUtils.WriteI32(pos.BlockY, spawn, 5);
            NetUtils.WriteI32(pos.BlockZ, spawn, 9);
            Send(spawn);
        }

        public override void SendRemoveEntity(byte id) {
            byte[] data = new byte[1 + 4];
            data[0] = OPCODE_REMOVE_ENTITY;
            WriteI32(id, data, 1);
            Send(data);
        }

        public override void SendChat(string message) {
            message = CleanupColors(message);
            List<string> lines = LineWrapper.Wordwrap(message, true);
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Replace('&', '§');
                Send(MakeChat(line));
            }
        }

        public override void SendMessage(CpeMessageType type, string message) {
            message = CleanupColors(message);
            if (type != CpeMessageType.Normal) return;
            message = message.Replace('&', '§');
            Send(MakeChat(message));
        }

        public override void SendKick(string reason, bool sync) {
            reason = CleanupColors(reason);
            reason = reason.Replace('&', '§');
            socket.Send(MakeKick(reason), sync ? SendFlags.Synchronous : SendFlags.None);
        }

        protected void SendHandshake(string serverID) {
            Send(MakeHandshake(serverID));
        }

        public override void SendBlockchange(ushort x, ushort y, ushort z, BlockID block) {
            byte[] packet = new byte[1 + 4 + 1 + 4 + 1 + 1];
            byte raw = (byte)ConvertBlock(block);
            WriteBlockChange(packet, 0, raw, x, y, z);
            Send(packet);
        }

        public override void SendTeleport(byte id, Position pos, Orientation rot) {
            if (id == Entities.SelfID) {
                Send(MakeSelfMoveLook(pos, rot));
            } else {
                Send(MakeEntityTeleport(id, pos, rot));
            }
        }

        bool sentMOTD;
        public override void SendMotd(string motd) {
            if (sentMOTD) return; // TODO work out how to properly resend map
            sentMOTD = true;
            Send(MakeLogin(motd));
        }

        public override void SendSpawnEntity(byte id, string name, string skin, Position pos, Orientation rot) {
            name = CleanupColors(name);
            name = name.Replace('&', '§');
            skin = skin.Replace('&', '§');

            if (id == Entities.SelfID) {
                Send(MakeSelfMoveLook(pos, rot));
            } else {
                Send(MakeNamedAdd(id, name, skin, pos, rot));
            }
        }
#endregion


#region Packet builders
        byte[] MakeHandshake(string serverID) {
            int dataLen = 1 + 2 + CalcStringLength(serverID);
            byte[] data = new byte[dataLen];

            data[0] = OPCODE_HANDSHAKE;
            WriteString(data, 1, serverID);
            return data;
        }

        byte[] MakeChat(string text) {
            int textLen = CalcStringLength(text);
            byte[] data = new byte[1 + 2 + textLen];

            data[0] = OPCODE_CHAT;
            WriteString(data, 1 , text);
            return data;
        }

        byte[] MakeKick(string reason) {
            int textLen = CalcStringLength(reason);
            byte[] data = new byte[1 + 2 + textLen];

            data[0] = OPCODE_KICK;
            WriteString(data, 1 , reason);
            return data;
        }

        protected abstract byte[] MakeLogin(string motd);

        protected abstract byte[] MakeSelfMoveLook(Position pos, Orientation rot);

        protected abstract byte[] MakeNamedAdd(byte id, string name, string skin, Position pos, Orientation rot);

        protected virtual byte[] MakeEntityTeleport(byte id, Position pos, Orientation rot) {
            int dataLen = 1 + 4 + (4 + 4 + 4) + (1 + 1);
            byte[] data = new byte[dataLen];
            data[0] = OPCODE_TELEPORT;

            WriteI32(id, data, 1);
            WriteI32(pos.X, data,  5);
            WriteI32(pos.Y, data,  9);
            WriteI32(pos.Z, data, 13);

            data[17] = (byte)(rot.RotY + 128); // TODO fixed yaw kinda
            data[18] = rot.HeadX;
            return data;
        }
#endregion


#region Packet handlers
        static readonly byte[] chat_fields = { FIELD_BYTE, FIELD_STRING };
        protected int HandleChat(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, chat_fields, values);
            if (left < size) return 0;

            string text = parser.ReadString(buffer, offset + 1);
            player.ProcessChat(text, false);
            return size;
        }

        static readonly byte[] state_fields = { FIELD_BYTE, FIELD_BYTE };
        protected int HandleSelfStateOnly(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, state_fields, values);
            if (left < size) return 0;
            // bool state

            Position pos    = player.Pos;
            Orientation rot = player.Rot;
            player.ProcessMovement(pos.X, pos.Y, pos.Z, rot.RotY, rot.HeadX, 0);
            return size;
        }
#endregion


        public override void UpdatePlayerPositions() {
            Player[] players = PlayerInfo.Online.Items;
            Player dst = player;
            
            foreach (Player p in players) 
            {
                if (dst == p || dst.level != p.level || !dst.CanSeeEntity(p)) continue;
                
                Orientation rot = p.Rot;
                Position pos    = p._tempPos;
                // TODO TEMP HACK
                Position delta  = new Position(pos.X - p._lastPos.X, pos.Y - p._lastPos.Y, pos.Z - p._lastPos.Z);
                bool posChanged = delta.X  != 0 || delta.Y != 0 || delta.Z != 0;
                bool oriChanged = rot.RotY != p._lastRot.RotY   || rot.HeadX != p._lastRot.HeadX;
                if (posChanged || oriChanged)
                    SendTeleport(p.id, pos, rot);
            }
        }

        public override byte[] MakeBulkBlockchange(BufferedBlockSender buffer) {
            int size = 1 + 4 + 1 + 4 + 1 + 1;
            byte[] data = new byte[size * buffer.count];
            Level level = buffer.level;

            for (int i = 0; i < buffer.count; i++)
            {
                int index = buffer.indices[i];
                int x = (index % level.Width);
                int y = (index / level.Width) / level.Length;
                int z = (index / level.Width) % level.Length;

                WriteBlockChange(data, i * size, (byte)buffer.blocks[i], x, y, z);
            }
            return data;
        }

        protected virtual void WriteBlockChange(byte[] data, int offset, byte block, int x, int y, int z){
            data[offset + 0] = OPCODE_BLOCK_CHANGE;
            WriteI32(x, data, offset + 1);
            data[offset + 5] = (byte)y;
            WriteI32(z, data, offset + 6);
            data[offset + 10] = block;
            data[offset + 11] = 0; // metadata
        }
    }

    unsafe class AlphaProtocol : AlphaIndevProtocol
    {
        const int ALPHA_111_PROTOCOL_VERSION = 2;
        const int  BETA_173_PROTOCOL_VERSION = 14;

        const int OPCODE_SLOT_SWITCHED = 0x10;
        public bool IsBeta {  get { return !parser.utf8; } }

        public AlphaProtocol(INetSocket s, string name, bool utf8) : base(s, name, utf8) { }

        protected override int HandlePacket(byte[] buffer, int offset, int left) {
            //Console.WriteLine("IN: " + buffer[offset]);
            switch (buffer[offset]) {
                case OPCODE_PING:      return 1; // Ping
                case OPCODE_LOGIN:     return HandleLogin(buffer, offset, left);
                case OPCODE_CHAT:      return HandleChat(buffer, offset, left);
                case OPCODE_SELF_STATEONLY: return HandleSelfStateOnly(buffer, offset, left);
                case OPCODE_SELF_MOVE:      return HandleSelfMove(buffer, offset, left);
                case OPCODE_SELF_LOOK:      return HandleSelfLook(buffer, offset, left);
                case OPCODE_SELF_MOVE_LOOK: return HandleSelfMoveLook(buffer, offset, left);
                case OPCODE_BLOCK_DIG:      return HandleBlockDig(buffer, offset, left);
                case OPCODE_BLOCK_PLACE:    return HandleBlockPlace(buffer, offset, left);
                case OPCODE_ARM_ANIM:       return HandleArmAnim(buffer, offset, left);

                case OPCODE_SLOT_SWITCHED:  return HandleSlotSwitched(buffer, offset, left);

                default:
                    player.Leave("Unhandled opcode \"" + buffer[offset] + "\"!", true);
                    return left;
            }
        }

        protected override int CalcStringLength(string value) {
            return parser.CalcStringLength(value);
        }

        protected override void WriteString(byte[] buffer, int offset, string value) {
            parser.WriteString(buffer, offset, value);
        }

        static BlockID ReadBlock(byte[] buffer, int offset) {
            return Block.FromRaw(buffer[offset]); 
        }
        

#region Common processing
        static readonly byte[] login_fields = { FIELD_BYTE, FIELD_INT, FIELD_STRING, FIELD_STRING };
        int HandleLogin(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, login_fields, values);
            if (left < size) return 0;

            int version = values[1].I32;
            if (!IsBeta && version == ALPHA_111_PROTOCOL_VERSION) {
                // okay
            } else if (IsBeta && version == BETA_173_PROTOCOL_VERSION) {
                // TODO the beta login packet is structured differently from Alpha,
                //  and this just happens to work by accident
                // (client sends rest of fields with 0, so they're treated as empty strings or ping packets)
            } else {
                player.Leave("Unsupported protocol version!"); return left;
            }

            string name = parser.ReadString(buffer, values[2].I32);
            string pass = parser.ReadString(buffer, values[3].I32);

            if (!player.ProcessLogin(name, pass)) return left;

            for (byte b = 0; b < Block.CPE_COUNT; b++)
            {
                fallback[b] = Block.ConvertClassic(b, Server.VERSION_0030);
            }

            player.CompleteLoginProcess();
            return size;
        }

        static readonly byte[] move_fields = { FIELD_BYTE, FIELD_DOUBLE, FIELD_DOUBLE, FIELD_DOUBLE, FIELD_DOUBLE, FIELD_BYTE };
        int HandleSelfMove(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, move_fields, values);
            if (left < size) return 0;

            double x = values[1].F64;
            double s = values[2].F64; // TODO probably wrong (e.g. when crouching)
            double y = values[3].F64;
            double z = values[4].F64;
            // bool state

            Orientation rot = player.Rot;
            player.ProcessMovement((int)(x * 32), (int)(y * 32), (int)(z * 32),
                              rot.RotY, rot.HeadX, 0);
            return size;
        }

        static readonly byte[] look_fields = { FIELD_BYTE, FIELD_FLOAT, FIELD_FLOAT, FIELD_BYTE };
        int HandleSelfLook(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, look_fields, values);
            if (left < size) return 0;

            float yaw   = values[1].F32 + 180.0f;
            float pitch = values[2].F32;
            // bool state

            Position pos = player.Pos;
            player.ProcessMovement(pos.X, pos.Y, pos.Z,
                              (byte)(yaw / 360.0f * 256.0f), (byte)(pitch / 360.0f * 256.0f), 0);
            return size;
        }

        static byte[] movelook_fields = { FIELD_BYTE, FIELD_DOUBLE, FIELD_DOUBLE, FIELD_DOUBLE, FIELD_DOUBLE, FIELD_FLOAT, FIELD_FLOAT, FIELD_BYTE };
        int HandleSelfMoveLook(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, movelook_fields, values);
            if (left < size) return 0;

            double x = values[1].F64;
            double s = values[2].F64; // TODO probably wrong (e.g. when crouching)
            double y = values[3].F64;
            double z = values[4].F64;

            float yaw   = values[5].F32 + 180.0f;
            float pitch = values[6].F32;
            // bool state

            player.ProcessMovement((int)(x * 32), (int)(y * 32), (int)(z * 32),
                              (byte)(yaw / 360.0f * 256.0f), (byte)(pitch / 360.0f * 256.0f), 0);
            return size;
        }

        static readonly byte[] dig_fields = { FIELD_BYTE, FIELD_BYTE, FIELD_INT, FIELD_BYTE, FIELD_INT, FIELD_BYTE };
        int HandleBlockDig(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, dig_fields, values);
            if (left < size) return 0;

            byte status = values[1].U8;
            int x    = values[2].I32;
            int y    = values[3].U8; // Y is a byte
            int z    = values[4].I32;
            byte dir = values[5].U8;

            if (status == 3)
                player.ProcessBlockchange((ushort)x, (ushort)y, (ushort)z, 0, 0);
            return size;
        }

        static readonly byte[] place_fields = { FIELD_BYTE, FIELD_SHORT, FIELD_INT, FIELD_BYTE, FIELD_INT, FIELD_BYTE };
        int HandleBlockPlace(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, place_fields, values);
            if (left < size) return 0;

            BlockID block = values[1].U16;
            int x    = values[2].I32;
            int y    = values[3].U8; // Y is a byte
            int z    = values[4].I32;
            byte dir = values[5].U8;

            player.ProcessBlockchange((ushort)x, (ushort)y, (ushort)z, 1, block);
            return size;
        }

        static readonly byte[] anim_fields = { FIELD_BYTE, FIELD_INT, FIELD_BYTE };
        int HandleArmAnim(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, anim_fields, values);
            if (left < size) return 0;

            // TODO something
            return size;
        }
        #endregion


        #region Beta processing
        static readonly byte[] slotswitch_fields = { FIELD_BYTE, FIELD_SHORT };
        int HandleSlotSwitched(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, slotswitch_fields, values);
            if (left < size) return 0;

            // TODO something
            return size;
        }
        #endregion

        public override void SendLevel(Level prev, Level level) {         
            byte* conv = stackalloc byte[Block.ExtendedCount];
            for (int j = 0; j < Block.ExtendedCount; j++)
            {
                conv[j] = (byte)ConvertBlock((BlockID)j);
            }

            // unload chunks from previous world
            if (prev != null)
            {
                for (int z = 0; z < prev.ChunksZ; z++)
                    for (int x = 0; x < prev.ChunksX; x++)
                    {
                        Send(MakePreChunk(x, z, false));
                    }
            }

            for (int z = 0; z < level.ChunksZ; z++)
                for (int x = 0; x < level.ChunksX; x++)
                {
                    Send(MakePreChunk(x, z, true));
                    Send(MakeChunk(x, z, level, conv));
                }
        }

        protected override byte[] MakeLogin(string motd) {
            return IsBeta ? MakeBetaLogin(motd) : MakeAlphaLogin(motd);
        }

        byte[] MakeAlphaLogin(string motd) {
            int nameLen = CalcStringLength(Server.Config.Name);
            int motdLen = CalcStringLength(motd);
            int dataLen = 1 + 4 + (2 + nameLen) + (2 + motdLen);
            byte[] data = new byte[dataLen];

            data[0] = OPCODE_LOGIN;
            NetUtils.WriteI32(2, data, ALPHA_111_PROTOCOL_VERSION);
            WriteString(data, 1 + 4,               Server.Config.Name);
            WriteString(data, 1 + 4 + 2 + nameLen, motd);
            return data;
        }

        byte[] MakeBetaLogin(string motd) {
            string name = Server.Config.Name;
            // indev client disconnects when receiving a server name with > 16 characters
            //  "java.io.IOException: Received string length longer than maximum allowed (18 > 16)"
            if (name.Length > 16) name = name.Substring(0, 16);

            int nameLen = CalcStringLength(name);
            int dataLen = 1 + 4 + (2 + nameLen) + (8 + 1);
            byte[] data = new byte[dataLen];

            data[0] = OPCODE_LOGIN;
            NetUtils.WriteI32(Entities.SelfID, data, 1);
            WriteString(data, 1 + 4, name);
            // U64 map seed, U8 dimension
            return data;
        }

        protected override byte[] MakeSelfMoveLook(Position pos, Orientation rot) {
            byte[] data = new byte[1 + 8 + 8 + 8 + 8 + 4 + 4 + 1];
            float yaw   = rot.RotY  * 360.0f / 256.0f;
            float pitch = rot.HeadX * 360.0f / 256.0f;
            data[0] = OPCODE_SELF_MOVE_LOOK;

            WriteF64(pos.X / 32.0, data,  1);
            WriteF64(pos.Y / 32.0, data,  9); // stance?
            WriteF64(pos.Y / 32.0, data, 17);
            WriteF64(pos.Z / 32.0, data, 25);

            WriteF32(yaw,   data, 33);
            WriteF32(pitch, data, 37);
            data[41] = 1;
            return data;
        }

        protected override byte[] MakeNamedAdd(byte id, string name, string skin, Position pos, Orientation rot) {
            // TODO fixes Y kinda
            pos.Y -= 51;

            int nameLen = CalcStringLength(name);
            int dataLen = 1 + 4 + (2 + nameLen) + (4 + 4 + 4) + (1 + 1) + 2;
            byte[] data = new byte[dataLen];

            data[0] = OPCODE_NAMED_ADD;
            WriteI32(id, data, 1);
            WriteString(data, 5, name);

            WriteI32(pos.X, data,  7 + nameLen);
            WriteI32(pos.Y, data, 11 + nameLen);
            WriteI32(pos.Z, data, 15 + nameLen);

            data[19 + nameLen] = rot.RotY;
            data[20 + nameLen] = rot.HeadX;
            WriteU16(0, data, 21 + nameLen); // current item
            return data;
        }

        protected override byte[] MakeEntityTeleport(byte id, Position pos, Orientation rot) {
            // TODO fixes Y kinda
            pos.Y -= 51;
            return base.MakeEntityTeleport(id, pos, rot);
        }

        byte[] MakePreChunk(int x, int z, bool load)
        {
            byte[] data = new byte[1 + 4 + 4 + 1];
            data[0] = OPCODE_PRE_CHUNK;

            WriteI32(x, data, 1);
            WriteI32(z, data, 5);
            data[9] = (byte)(load ? 1 : 0);
            return data;
        }


        byte[] MakeChunk(int x, int z, Level lvl, byte* conv) {
            MemoryStream tmp = new MemoryStream();

            //using (DeflateStream dst = new DeflateStream(tmp, CompressionMode.Compress))
            using (ZLibStream dst = new ZLibStream(tmp))
            {
                byte[] block_data  = new byte[16 * 16 * 128];
                byte[] block_meta  = new byte[(16 * 16 * 128) / 2];
                byte[] block_light = new byte[(16 * 16 * 128) / 2];
                byte[] sky_light   = new byte[(16 * 16 * 128) / 2];

                int height = Math.Min(128, (int)lvl.Height);

                for (int YY = 0; YY < height; YY++)
                    for (int ZZ = 0; ZZ < 16; ZZ++)
                        for (int XX = 0; XX < 16; XX++)
                        {
                            int X = (x * 16) + XX, Y = YY, Z = (z * 16) + ZZ;
                            if (!lvl.IsValidPos(X, Y, Z)) continue;

                            block_data[YY + (ZZ * 128 + (XX * 128 * 16))] = conv[lvl.FastGetBlock((ushort)X, (ushort)Y, (ushort)Z)];
                        }

                // Make everything insanely bright
                for (int i = 0; i < sky_light.Length; i++) sky_light[i] = 0xFF;

                dst.Write(block_data,  0,  block_data.Length);
                dst.Write(block_meta,  0,  block_meta.Length);
                dst.Write(block_light, 0, block_light.Length);
                dst.Write(sky_light,   0,   sky_light.Length);
            }

            byte[] chunk = tmp.ToArray();
            int dataLen  = 1 + 4 + 2 + 4 + 1 + 1 + 1 + 4 + chunk.Length;
            byte[] data  = new byte[dataLen];

            data[0] = OPCODE_CHUNK;
            WriteI32(x * 16, data, 1); // X/Y/Z chunk origin
            WriteU16(0,      data, 5);
            WriteI32(z * 16, data, 7);
            data[11] = 15;  // X/Y/Z chunk size - 1
            data[12] = 127;
            data[13] = 15;

            WriteI32(chunk.Length, data, 14);
            Array.Copy(chunk, 0, data, 18, chunk.Length);
            return data;
        }

        class ZLibStream : Stream
        {
            Stream underlying;
            DeflateStream dst;
            bool wroteHeader;
            uint s1 = 1, s2 = 0;

            public ZLibStream(Stream tmp)
            {
                underlying = tmp;
                dst = new DeflateStream(tmp, CompressionMode.Compress, true);
            }

            public override bool CanRead { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return true; } }

            static Exception ex = new NotSupportedException();
            public override void Flush() { }
            public override long Length { get { throw ex; } }
            public override long Position { get { throw ex; } set { throw ex; } }
            public override int Read(byte[] buffer, int offset, int count) { throw ex; }
            public override long Seek(long offset, SeekOrigin origin) { throw ex; }
            public override void SetLength(long length) { throw ex; }

            public override void Close() {
                dst.Close();
                WriteFooter();
                base.Close();
            }

            public override void Write(byte[] buffer, int offset, int count) {
                if (!wroteHeader) WriteHeader();

                for (int i = 0; i < count; i++)
                {
                    s1 = (s1 + buffer[offset + i]) % 65521;
                    s2 = (s2 + s1)                 % 65521;
                }
                dst.Write(buffer, offset, count);
            }
            // NOTE: don't call WriteByte because it's imlicitly Write(new byte[] {value}, 0, 1});

            void WriteHeader() {
                byte[] header = new byte[] { 0x78, 0x9C };
                wroteHeader   = true;
                underlying.Write(header, 0, header.Length);
            }

            void WriteFooter() {
                byte[] footer = new byte[4];
                uint adler32 = (s2 << 16) | s1;
                WriteI32((int)adler32, footer, 0);
                underlying.Write(footer, 0, footer.Length);
            }
        }


        public override string ClientName() {
            return IsBeta ? "Beta 1.7.3" : "Alpha 1.1.1";
        }
        
        /*public override ushort ConvertBlock(ushort block)
        {
        	// TODO temp hack
            return Block.Convert(block);
        }*/
    }

    unsafe class IndevProtocol : AlphaIndevProtocol
    {
        public const int PROTOCOL_VERSION = 9;

        // NOTE indev replaces bottom 2 layers with lava
        //  although second layer *can* be replaced via SetBlock,
        //  bottom layer will always be hardcoded to lava
        // so have to shift the whole world up instead
        const int WORLD_SHIFT_BLOCKS = 2;
        const int WORLD_SHIFT_COORDS = 64;

        public IndevProtocol(INetSocket s, string name, bool utf8) : base(s, name, utf8) { }

        protected override int HandlePacket(byte[] buffer, int offset, int left) {
            //Console.WriteLine("IN: " + buffer[offset]);
            switch (buffer[offset]) {
                case OPCODE_PING:      return 1; // Ping
                case OPCODE_LOGIN:     return HandleLogin(buffer, offset, left);
                case OPCODE_CHAT:      return HandleChat(buffer, offset, left);
                case OPCODE_SELF_STATEONLY: return HandleSelfStateOnly(buffer, offset, left);
                case OPCODE_SELF_MOVE:      return HandleSelfMove(buffer, offset, left);
                case OPCODE_SELF_LOOK:      return HandleSelfLook(buffer, offset, left);
                case OPCODE_SELF_MOVE_LOOK: return HandleSelfMoveLook(buffer, offset, left);
                case OPCODE_BLOCK_DIG:      return HandleBlockDig(buffer, offset, left);
                case OPCODE_BLOCK_PLACE:    return HandleBlockPlace(buffer, offset, left);
                case OPCODE_ARM_ANIM:       return HandleArmAnim(buffer, offset, left);

                default:
                    player.Leave("Unhandled opcode \"" + buffer[offset] + "\"!", true);
                    return left;
            }
        }

        protected override int CalcStringLength(string value) {
            //return Encoding.BigEndianUnicode.GetByteCount(value);
            return value.Length * 2;
        }

        protected override void WriteString(byte[] buffer, int offset, string value) {
            // actually trying to send unicode tends to kill the client with
            //  java.lang.ArrayIndexOutOfBoundsException: 9787

            //int len = Encoding.BigEndianUnicode.GetBytes(value, 0, value.Length, buffer, offset + 2);
            WriteU16((ushort)value.Length, buffer, offset);
            offset += 2;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i] == '§' ? '§' : value[i].UnicodeToCp437();
                buffer[offset++] = (byte)(c >> 8);
                buffer[offset++] = (byte)c;
            }
        }

        static BlockID ReadBlock(byte[] buffer, int offset) { return Block.FromRaw(buffer[offset]); }


#region Classic processing
        static readonly byte[] login_fields = { FIELD_BYTE, FIELD_INT, FIELD_STRING, FIELD_DOUBLE, FIELD_STRING, FIELD_STRING };
        int HandleLogin(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, login_fields, values);
            if (left < size) return 0;

            int version = values[1].I32;
            if (version != PROTOCOL_VERSION) {
                player.Leave("Unsupported protocol version!"); return left;
            }

            string name = parser.ReadString(buffer,  values[2].I32);

            // TODO what do these 8 bytes even do? usually 0
            long unknown = values[3].I64;

            // TODO I dunno what these two strings are really for
            string motd1 = parser.ReadString(buffer, values[4].I32); // usually "Loading level..."
            string motd2 = parser.ReadString(buffer, values[5].I32); // usually "Loading server..."

            Logger.Log(LogType.SystemActivity, "MOTD 1: " + motd1);
            Logger.Log(LogType.SystemActivity, "MOTd 2:" + motd2);
            if (!player.ProcessLogin(name, "")) return left;

            for (byte b = 0; b < Block.CPE_COUNT; b++)
            {
                fallback[b] = Block.ConvertClassic(b, Server.VERSION_0030);
            }

            player.CompleteLoginProcess();
            return size;
        }

        static readonly byte[] move_fields = { FIELD_BYTE, FIELD_FLOAT, FIELD_FLOAT, FIELD_FLOAT, FIELD_FLOAT, FIELD_BYTE };
        int HandleSelfMove(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, move_fields, values);
            if (left < size) return 0;

            float x = values[1].F32;
            float y = values[2].F32;
            float s = values[3].F32;
            float z = values[4].F32;
            // bool state

            y += 1.59375f; // feet -> 'head' position
            y -= WORLD_SHIFT_BLOCKS;

            Orientation rot = player.Rot;
            player.ProcessMovement((int)(x * 32), (int)(y * 32), (int)(z * 32),
                              rot.RotY, rot.HeadX, 0);
            return size;
        }

        static readonly byte[] look_fields = { FIELD_BYTE, FIELD_FLOAT, FIELD_FLOAT, FIELD_BYTE };
        int HandleSelfLook(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, look_fields, values);
            if (left < size) return 0;

            float yaw   = values[1].F32 + 180.0f;
            float pitch = values[2].F32;
            // bool state

            Position pos = player.Pos;
            player.ProcessMovement(pos.X, pos.Y, pos.Z,
                              (byte)(yaw / 360.0f * 256.0f), (byte)(pitch / 360.0f * 256.0f), 0);
            return size;
        }

        static readonly byte[] movelook_fields = { FIELD_BYTE, FIELD_FLOAT, FIELD_FLOAT, FIELD_FLOAT, FIELD_FLOAT, FIELD_FLOAT, FIELD_FLOAT, FIELD_BYTE };
        int HandleSelfMoveLook(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, movelook_fields, values);
            if (left < size) return 0;

            float x = values[1].F32;
            float y = values[2].F32;
            float s = values[3].F32;
            float z = values[4].F32;

            float yaw   = values[5].F32 + 180.0f;
            float pitch = values[6].F32;
            // bool state

            y += 1.59375f; // feet -> 'head' position
            y -= WORLD_SHIFT_BLOCKS;

            player.ProcessMovement((int)(x * 32), (int)(y * 32), (int)(z * 32),
                              (byte)(yaw / 360.0f * 256.0f), (byte)(pitch / 360.0f * 256.0f), 0);
            return size;
        }

        static readonly byte[] dig_fields = { FIELD_BYTE, FIELD_BYTE, FIELD_INT, FIELD_BYTE, FIELD_INT, FIELD_BYTE };
        int HandleBlockDig(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, dig_fields, values);
            if (left < size) return 0;

            byte status = values[1].U8;
            int x    = values[2].I32;
            int y    = values[3].U8; // Y is a byte
            int z    = values[4].I32;
            byte dir = values[5].U8;
            y -= WORLD_SHIFT_BLOCKS;

            if (status == 2)
                player.ProcessBlockchange((ushort)x, (ushort)y, (ushort)z, 0, 0);
            return size;
        }

        static readonly byte[] place_fields = { FIELD_BYTE, FIELD_INT, FIELD_BYTE, FIELD_INT, FIELD_BYTE, FIELD_SHORT };
        int HandleBlockPlace(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, place_fields, values);
            if (left < size) return 0;

            int x    = values[1].I32;
            int y    = values[2].U8; // Y is a byte
            int z    = values[3].I32;
            byte dir = values[4].U8;
            BlockID block = values[5].U16;

            y -= WORLD_SHIFT_BLOCKS;
            player.ProcessBlockchange((ushort)x, (ushort)y, (ushort)z, 1, block);
            return size;
        }

        static readonly byte[] anim_fields = { FIELD_BYTE, FIELD_INT, FIELD_BYTE };
        int HandleArmAnim(byte[] buffer, int offset, int left) {
            FieldValue* values = stackalloc FieldValue[20];
            int size = parser.ParsePacket(buffer, offset, left, anim_fields, values);
            if (left < size) return 0;

            // TODO something
            return size;
        }
        #endregion


        public override void SendSpawnEntity(byte id, string name, string skin, Position pos, Orientation rot) {
            // indev client disconnects when receiving an entity with nametag > 16 characters
            //  "java.io.IOException: Received string length longer than maximum allowed (18 > 16)"
            // try to remove colors and then if still too long, rest of name too
            if (name.Length > 16) name = Colors.StripUsed(name);
            if (name.Length > 16) name = name.Substring(0, 16);

            base.SendSpawnEntity(id, name, skin, pos, rot);
        }

        byte[] GetBlocks(Level level)
        {
            // NOTE indev client always overwrites bottom 2 layers with lava.. ?
            byte[] blocks = new byte[level.blocks.Length];
            int i = level.PosToInt(0, 2, 0);
            //for (int j = 0; j < i; j++) blocks[j] = Block.Bedrock;

            byte* conv = stackalloc byte[Block.ExtendedCount];
            for (int j = 0; j < Block.ExtendedCount; j++)
            {
                conv[j] = (byte)ConvertBlock((BlockID)j);
            }


            // TODO TERRIBLY AWFULLY EXTREMELY SLOW
            for (int y = 0; y < level.Height - 2; y++)
                for (int z = 0; z < level.Length; z++)
                    for (int x = 0; x < level.Width; x++)
                    {
                        blocks[i++] = conv[level.FastGetBlock((ushort)x, (ushort)y, (ushort)z)];
                    }
            return blocks;
        }

        public override void SendLevel(Level prev, Level level) {
            // TODO what even this
            byte[] C_blks = CompressData(GetBlocks(level));
            byte[] C_meta = CompressData(new byte[level.blocks.Length]);
            byte[] tmp = new byte[4];

            using (MemoryStream ms = new MemoryStream())
            {
                byte[] map_data = new byte[1 + 4 + 4 + 4];
                map_data[0] = OPCODE_PRE_CHUNK;
                NetUtils.WriteI32(C_blks.Length, map_data, 1);
                NetUtils.WriteI32(C_meta.Length, map_data, 5);
                NetUtils.WriteI32(100, map_data, 9); // TODO what even is this
                ms.Write(map_data, 0, map_data.Length);

                // TODO this seems wrong
                NetUtils.WriteI32(C_blks.Length, tmp, 0);
                ms.Write(tmp, 0, tmp.Length);
                ms.Write(C_blks, 0, C_blks.Length);

                NetUtils.WriteI32(C_meta.Length, tmp, 0);
                ms.Write(tmp, 0, tmp.Length);
                ms.Write(C_meta, 0, C_meta.Length);

                Send(ms.ToArray());
            }

            byte[] final = new byte[1 + 4 + 4 + 4 + 4 + 4];
            final[0] = OPCODE_CHUNK;

            final[1] = 0x01;
            // 4 bytes ??
            // 01 00 00 00 - 128x64x128 world
            // 01 01 00 00 - 256x64x256 world
            // 01 02 00 00 - 512x64x512 world
            NetUtils.WriteI32(level.Width, final,  5);
            NetUtils.WriteI32(level.Height, final,  9);
            NetUtils.WriteI32(level.Length, final, 13);
            // 4 bytes ???? checksum???
            //final[19] = 0x01; final[20] = 0x2E;
            Send(final);

            SendSetSpawnpoint(level.SpawnPos, default(Orientation));
        }

        byte[] CompressData(byte[] data)
        {
            using (MemoryStream dst = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(dst, CompressionMode.Compress, true))
                {
                    byte[] buffer = new byte[4];
                    NetUtils.WriteI32(data.Length, buffer, 0);
                    gz.Write(buffer, 0, 4);

                    gz.Write(data, 0, data.Length);
                }
                return dst.ToArray();
            }
        }

        protected override byte[] MakeLogin(string motd) {
            int nameLen = CalcStringLength(Server.Config.Name);
            int motdLen = CalcStringLength(motd);
            int dataLen = 1 + 14 + (2 + nameLen) + (2 + motdLen);
            byte[] data = new byte[dataLen];

            data[0] = OPCODE_LOGIN;
            // TODO not sure what first 14 bytes of data are
            // bytes 0-4 look like initial world time though?
            //   NetUtils.WriteI32(2, data, PROTOCOL_VERSION);
            WriteString(data, 1 + 14,               Server.Config.Name);
            WriteString(data, 1 + 14 + 2 + nameLen, motd);
            return data;
        }

        protected override byte[] MakeSelfMoveLook(Position pos, Orientation rot) {
            byte[] data = new byte[1 + 4 + 4 + 4 + 4 + 4 + 4 + 1];
            float yaw   = rot.RotY  * 360.0f / 256.0f;
            float pitch = rot.HeadX * 360.0f / 256.0f;
            data[0] = OPCODE_SELF_MOVE_LOOK;

            pos.Y += 83; // TODO not sure why this much 
            pos.Y += WORLD_SHIFT_COORDS;

            WriteF32(pos.X / 32.0f, data,  1);
            WriteF32(pos.Y / 32.0f, data,  5); // stance?
            WriteF32(pos.Y / 32.0f, data,  9);
            WriteF32(pos.Z / 32.0f, data, 13);

            WriteF32(yaw,   data, 17);
            WriteF32(pitch, data, 21);
            data[25] = 1;
            return data;
        }

        protected override byte[] MakeNamedAdd(byte id, string name, string skin, Position pos, Orientation rot) {
            int nameLen = CalcStringLength(name);
            int dataLen = 1 + 4 + (2 + nameLen) + (4 + 4 + 4) + (1 + 1) + 2;
            byte[] data = new byte[dataLen];
            // TODO fixes Y kinda
            pos.Y -= 19;
            pos.Y += WORLD_SHIFT_COORDS;

            data[0] = OPCODE_NAMED_ADD;
            WriteI32(id, data, 1);
            WriteString(data, 5, name);

            WriteI32(pos.X, data,  7 + nameLen);
            WriteI32(pos.Y, data, 11 + nameLen);
            WriteI32(pos.Z, data, 15 + nameLen);

            data[19 + nameLen] = (byte)(rot.RotY + 128); // TODO fixed yaw kinda
            data[20 + nameLen] = rot.HeadX;
            WriteU16(0, data, 21 + nameLen); // current item
            return data;
        }

        protected override byte[] MakeEntityTeleport(byte id, Position pos, Orientation rot) {
            // TODO fixes Y kinda
            pos.Y -= 19;
            pos.Y += WORLD_SHIFT_COORDS;
            return base.MakeEntityTeleport(id, pos, rot);
        }

        public override string ClientName() { return "Indev"; }

        protected override void WriteBlockChange(byte[] data, int offset, byte block, int x, int y, int z) {
            y += WORLD_SHIFT_BLOCKS;
            base.WriteBlockChange(data, offset, block, x, y, z);
        }
    }
}