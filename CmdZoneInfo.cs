using MCGalaxy.Maths;
using MCGalaxy.Network;
using MCGalaxy.Tasks;
using MCGalaxy;
using System;

namespace CommandZoneInfo
{
	public sealed class CmdZoneinfo : Command
	{
		public override string name { get { return "Zoneinfo"; } }
		public override string shortcut { get { return "Zinfo"; } }
		public override string type { get { return CommandTypes.Information; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Builder; } }
		public override void Use(Player p, string message) {
			if (string.IsNullOrWhiteSpace(message)) {
				Command zones = Find("ZoneList");
				if (p.Rank >= zones.defaultRank) zones.Use(p, "");
				Help(p); return;
			}
			string zonename = message.ToLower();
			Zone z = Matcher.FindZones(p, p.level, zonename);
			if (z == null) { p.Message("No zones found with the name: &F" + zonename); return; }
			p.Message("Information for Zone, {0}&S:", z.ColoredName);
			p.Message("  Ranks: {0} &S-> {1}", Group.GetColoredName(z.Config.BuildMin), Group.GetColoredName(z.Config.BuildMax));
			p.Message("  Bounds:");
			p.Message("    Min - &4X:&F{0} &AY:&F{1} &1Z:&F{2} ", z.MinX, z.MinY, z.MinZ);
			p.Message("    Max - &4X:&F{0} &AY:&F{1} &1Z:&F{2} ", z.MaxX, z.MaxY, z.MaxZ);
			p.Message("  Size: &F{0} &Sx &F{1} &Sx &F{2}", z.MaxX - z.MinX + 1, z.MaxY - z.MinY + 1, z.MaxZ - z.MinZ + 1);
			if (z.Config.BuildWhitelist.Count > 0) p.Message("  Whitelist: &F" + z.Config.BuildWhitelist.Join());
			if (z.Config.BuildBlacklist.Count > 0) p.Message("  Blacklist: &F" + z.Config.BuildBlacklist.Join());
			if (!p.Supports(CpeExt.SelectionCuboid)) return;
			HighlightZoneArgs args = new HighlightZoneArgs() { Player = p, Zone = z, Repeats = 250 };
			Server.Background.QueueRepeat(HighlightZones, args, TimeSpan.FromMilliseconds(20));
		}
		public override void Help(Player p) {
			p.Message("&T/Zoneinfo [name]");
			p.Message("&HHighlights and shows you information for a specified zone.");
		}

		class HighlightZoneArgs {
			public Player Player;
			public Zone Zone;
			public int Repeats;
		}

		static void HighlightZones(SchedulerTask task) {
			HighlightZoneArgs args = (HighlightZoneArgs)task.State;
			if (!args.Player.Supports(CpeExt.SelectionCuboid)) { task.Repeating = false; return; }
			Zone zone = args.Zone;
			if (args.Repeats == 1) {
				if (zone.Shows) {
					ColorDesc cola;
					Colors.TryParseHex(zone.Config.ShowColor, out cola);
					Vec3U16 mina = new Vec3U16(zone.MinX, zone.MinY, zone.MinZ);
					Vec3U16 maxa = new Vec3U16((ushort)(zone.MaxX + 1), (ushort)(zone.MaxY + 1), (ushort)(zone.MaxZ + 1));
					args.Player.Send(Packet.MakeSelection(zone.ID, zone.Config.Name, mina, maxa, cola.R, cola.G, cola.B, (byte)zone.Config.ShowAlpha, args.Player.hasCP437));
				} else {
					args.Player.Send(Packet.DeleteSelection(zone.ID));
				}
				task.Repeating = false;
				return;
			}
			int j = (250 - args.Repeats) % 30;
			if (j >= 16) j = 30 - j;
			char col = j < 10 ? (char)('0' + j) : (char)('A' + (j - 10));
			string c = new string(col, 6);
			ColorDesc colb;
			Colors.TryParseHex(c, out colb);
			Vec3U16 minb = new Vec3U16(zone.MinX, zone.MinY, zone.MinZ);
			Vec3U16 maxb = new Vec3U16((ushort)(zone.MaxX + 1), (ushort)(zone.MaxY + 1), (ushort)(zone.MaxZ + 1));
			args.Player.Send(Packet.MakeSelection(zone.ID, "ZInfo-" + zone.Config.Name, minb, maxb, colb.R, colb.G, colb.B, 127, args.Player.hasCP437));
			args.Repeats--;
		}
	}
}