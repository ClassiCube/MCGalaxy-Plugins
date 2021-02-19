using System;
using MCGalaxy.Commands;
using MCGalaxy.Blocks;
using BlockID = System.UInt16;

namespace MCGalaxy {
    public sealed class CmdTempBlock : Command2 {        
        public override string name { get { return "TempBlock"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }

        public override void Use(Player p, string message, CommandData data) {		
			
			if (!(p.group.Permission >= LevelPermission.Operator)) {
				if (!Hacks.CanUseHacks(p)) {
					if (data.Context != CommandContext.MessageBlock) {
						p.Message("%cYou cannot use this command manually when hacks are disabled.");
						return;
					}
				}
			}
			
			BlockID block = p.GetHeldBlock();
            int x = p.Pos.BlockX, y = (p.Pos.Y - 32) / 32, z = p.Pos.BlockZ;

            try {
                string[] parts = message.Split(' ');
                switch (parts.Length) {
                    case 1:
                        if (message == "") break;
                        
                        if (!CommandParser.GetBlock(p, parts[0], out block)) return;
                        break;
                    case 3:
                        x = int.Parse(parts[0]);
                        y = int.Parse(parts[1]);
                        z = int.Parse(parts[2]);
                        break;
                    case 4:
                        if (!CommandParser.GetBlock(p, parts[0], out block)) return;
                        
                        x = int.Parse(parts[1]);
                        y = int.Parse(parts[2]);
                        z = int.Parse(parts[3]);
                        break;
                    default: Player.Message(p, "Invalid number of parameters"); return;
                }
            } catch { 
                p.Message("Invalid parameters"); return; 
            }

			if (!CommandParser.IsBlockAllowed(p, "place ", block)) return;
            
            x = Clamp(x, p.level.Width);
            y = Clamp(y, p.level.Height);
            z = Clamp(z, p.level.Length);
			
            p.SendBlockchange( (ushort)x, (ushort)y, (ushort)z, block);
            //string blockName = Block.GetName(p, block);
            //Player.Message(p, "{3} block was placed at ({0}, {1}, {2}).", P.X, P.Y, P.Z, blockName);
        }
		
        static int Clamp(int value, int axisLen) {
            if (value < 0) return 0;
            if (value >= axisLen) return axisLen - 1;
            return value;
        }
        
        public override void Help(Player p) {
            p.Message("%T/TempBlock [block] <x> <y> <z>");
            p.Message("%HPlaces a client-side block at your feet or <x> <y> <z>");
        }
		
    }
}