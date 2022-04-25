using System;
using BlockID = System.UInt16;
using MCGalaxy;
using MCGalaxy.Network;

namespace PluginClassic0015
{
	public sealed class Classic0015Plugin : Plugin
	{
		public override string name { get { return "Classic0015Plugin"; } }
		public override string MCGalaxy_Version { get { return "1.9.4.0"; } }
		ProtocolConstructor oldCons;

		public override void Load(bool startup) {
			oldCons = INetSocket.Protocols[Opcode.Handshake];
			
			INetSocket.Protocols[Opcode.Handshake] = ConstructClassic;
		}

		public override void Unload(bool shutdown) {
			// restore original protocol constructor
			INetSocket.Protocols[Opcode.Handshake] = oldCons;
		}

		static INetProtocol ConstructClassic(INetSocket socket) {
			return new ClassicHandshakeParser(socket);
		}
	}
	
	class ClassicHandshakeParser : INetProtocol
	{
		INetSocket socket;
		public ClassicHandshakeParser(INetSocket s) { socket = s; }

		public void Disconnect() { }

		public int ProcessReceived(byte[] buffer, int length) {
			if (length < 2) return 0;

			byte version = buffer[1];
			if (version >= 'A' && version <= 'z') {
				socket.protocol = new Classic0015Protocol(socket);
			} else {
				socket.protocol = new ClassicProtocol(socket);
			}
			return socket.protocol.ProcessReceived(buffer, length);
		}
	}

	public class Classic0015Protocol : ClassicProtocol
	{
		public Classic0015Protocol(INetSocket s) : base(s) {
			ProtocolVersion = 2; // made up but less than Server.VERSION_0016
		}

		protected override int HandlePacket(byte[] buffer, int offset, int left) {
			switch (buffer[offset]) 
			{
				case Opcode.Ping:              return 1;
				case Opcode.Handshake:         return HandleLogin(buffer, offset, left);
				case Opcode.SetBlockClient:    return HandleBlockchange(buffer, offset, left);
				case Opcode.EntityTeleport:    return HandleMovement(buffer, offset, left);

				default:
					player.Leave("Unhandled opcode \"" + buffer[offset] + "\"!", true);
					return left;
			}
		}

		public override bool Supports(string extName, int version) { return false; }


		#region Packet processing
		int HandleLogin(byte[] buffer, int offset, int left) {
			int size = 1 + 64;
			if (left < size)     return 0;
			if (player.loggedIn) return size;

			ProtocolVersion = Server.VERSION_0016;
			string name = NetUtils.ReadString(buffer, offset + 1);
			if (!player.ProcessLogin(name, "")) return left;

			UpdateFallbackTable();
			player.CompleteLoginProcess();
			return size;
		}
		
		int HandleBlockchange(byte[] buffer, int offset, int left) {
			int size = 1 + 6 + 1 + 1;
			if (left < size) return 0;
			if (!player.loggedIn) return size;

			ushort x = NetUtils.ReadU16(buffer, offset + 1);
			ushort y = NetUtils.ReadU16(buffer, offset + 3);
			ushort z = NetUtils.ReadU16(buffer, offset + 5);

			byte action = buffer[offset + 7];
			if (action > 1) {
				player.Leave("Unknown block action!", true); return left;
			}

			BlockID held = Block.FromRaw(buffer[offset + 8]);
			player.ProcessBlockchange(x, y, z, action, held);
			return size;
		}
		
		int HandleMovement(byte[] buffer, int offset, int left) {
			int size = 1 + 6 + 2 + 1;
			if (left < size) return 0;
			if (!player.loggedIn) return size;

			int x = NetUtils.ReadI16(buffer, offset + 2);
			int y = NetUtils.ReadI16(buffer, offset + 4);
			int z = NetUtils.ReadI16(buffer, offset + 6);

			byte yaw   = buffer[offset + 8];
			byte pitch = buffer[offset + 9];
			player.ProcessMovement(x, y, z, yaw, pitch, -1);
			return size;
		}
		#endregion


		#region Packet sending
		public override void SendRemoveEntity(byte id) {
			// 0.0.15a uses different packet opcode for RemoveEntity
			byte[] packet = new byte[] { 9, id };
			Send(packet);
		}

		public override void SendChat(string message) { }
		public override void SendMessage(CpeMessageType type, string message) { }
		public override void SendKick(string reason, bool sync) { }
		public override bool SendSetUserType(byte type) { return false; }

		public override void SendMotd(string motd) {
			byte[] packet = new byte[1 + 64];
			packet[0] = Opcode.Handshake;
			NetUtils.Write(Server.Config.Name, packet, 1, player.hasCP437);

			Send(packet);
		}
		#endregion
		
		public override string ClientName() { return "Classic 0.15"; }

		// TODO modularise and move common code back into Entities.cs
		public unsafe override void UpdatePlayerPositions()
		{
			Player[] players = PlayerInfo.Online.Items;
			Player dst = player;

			foreach (Player p in players)
			{
				if (dst == p || dst.level != p.level || !dst.CanSeeEntity(p)) continue;
				Orientation rot = p.Rot;
				Position pos    = p._tempPos;
				
				// TODO TEMP HACK
				bool posChanged = pos.X    != p._lastPos.X    || pos.Y     != p._lastPos.Y || pos.Z != p._lastPos.Z;
				bool oriChanged = rot.RotY != p._lastRot.RotY || rot.HeadX != p._lastRot.HeadX;
				if (posChanged || oriChanged)
					SendTeleport(p.id, pos, rot);
			}
		}

		static byte FlippedPitch(byte pitch) {
			if (pitch > 64 && pitch < 192) return pitch;
			else return 128;
		}
	}
}