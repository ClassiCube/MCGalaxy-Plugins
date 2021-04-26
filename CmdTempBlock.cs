using System;
using MCGalaxy.Commands;
using MCGalaxy.Blocks;
using MCGalaxy.Maths;
using BlockID = System.UInt16;

namespace MCGalaxy {
    public sealed class CmdTempBlock : Command2 {        
        public override string name { get { return "TempBlock"; } }
        public override string type { get { return CommandTypes.Building; } }

        public override void Use(Player p, string message, CommandData data) {        
            
            if (!(p.group.Permission >= LevelPermission.Operator)) {
                if (!Hacks.CanUseHacks(p)) {
                    if (data.Context != CommandContext.MessageBlock) {
                        p.Message("%cYou cannot use this command manually when hacks are disabled.");
                        return;
                    }
                }
            }
            
            BlockID block  = p.GetHeldBlock();
            string[] parts = message.SplitSpaces();
            Vec3S32 pos;
            
            pos.X = p.Pos.BlockX;
            pos.Y = (p.Pos.Y - 32) / 32;
            pos.Z = p.Pos.BlockZ;
            
            switch (parts.Length) {
                case 1:
                    if (message == "") break;
                    
                    if (!CommandParser.GetBlock(p, parts[0], out block)) return;
                    break;
                case 3:
                    if (!CommandParser.GetCoords(p, parts, 0, ref pos)) return;
                    break;
                case 4:
                    if (!CommandParser.GetBlock(p, parts[0], out block)) return;
                    if (!CommandParser.GetCoords(p, parts, 1, ref pos)) return;
                    break;
                default:
                    p.Message("Invalid number of parameters"); return;
            }
            if (!CommandParser.IsBlockAllowed(p, "place ", block)) return;   
            
            pos = p.level.ClampPos(pos);
            p.SendBlockchange((ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z, block);
            //string blockName = Block.GetName(p, block);
            //p.Message("{3} block was placed at ({0}, {1}, {2}).", P.X, P.Y, P.Z, blockName);
        }
        
        public override void Help(Player p) {
            p.Message("%T/TempBlock <block>");
            p.Message("%HPlaces a client-side block at your feet");
            p.Message("%T/TempBlock <block> [x] [y] [z]");
            p.Message("%HPlaces a client-side block at [x] [y] [z]");
        }
    }
}