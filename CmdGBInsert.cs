using System;
using MCGalaxy.Bots;
using MCGalaxy.Commands;
using BlockID = System.UInt16;
using System.Collections.Generic;

namespace MCGalaxy.Commands.CPE {
    public class CmdGBInsert : Command {
        public override string name { get { return "GBInsert"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message) {
        	string[] args = message.SplitSpaces(2);
        	if (args.Length != 2) { Help(p); return; }
            BlockDefinition[] globalDefs = BlockDefinition.GlobalDefs;
        	
        	BlockID src;
        	if (!CommandParser.GetBlock(p, args[0], out src)) return;   	
        	if (globalDefs[src] == null) {
        		Player.Message(p, "No global block with that ID for source."); return;
        	}
        	
        	BlockID dst;
        	if (!CommandParser.GetBlock(p, args[1], out dst)) return;   	
        	if (globalDefs[dst] == null) {
        		Player.Message(p, "No global block with that ID for target."); return;
        	}
            
            // lazy fix
            if (globalDefs[src].InventoryOrder == -1) {
        		Player.Message(p, "Give source block an explicit order first. (too lazy to fix)");
        		return;
            }
        	
        	// Sort block definitions by inventory position           
        	List<BlockDefinition> defs = new List<BlockDefinition>();
        	foreach (BlockDefinition def in globalDefs) {
        		if (def == null || def.InventoryOrder == 255) continue;
        		defs.Add(def);
        	}
        	defs.Sort((a, b) => Order(a).CompareTo(Order(b)));
        	int srcOrder = Order(globalDefs[src]);
        	int dstOrder = Order(globalDefs[dst]);
        	
        	// Shift all following block definitions after target down by one position
        	// A B C s X Y Z --> A B C X Y Z
        	for (int i = defs.Count - 1; i >= 0; i--) {
        		if (Order(defs[i]) < srcOrder) break;
        		defs[i].InventoryOrder = Order(defs[i]) - 1;
        	}
        	
        	// Shift all following block definitions after target up by one position
        	// A B C d X Y Z --> A B C - d X Y Z
        	for (int i = defs.Count - 1; i >= 0; i--) {
        		if (Order(defs[i]) < dstOrder) break;
        		defs[i].InventoryOrder = Order(defs[i]) + 1;
        	}
        	
        	globalDefs[src].InventoryOrder = dstOrder;
        	BlockDefinition.UpdateOrder(globalDefs[src], true, null);
        	BlockDefinition.Save(true, null);
        	Player.Message(p, "Inserted block. You might need to rejoin though.");
        }
        
        static int Order(BlockDefinition def) {
        	return def.InventoryOrder == -1 ? def.BlockID : def.InventoryOrder;
        }

        public override void Help(Player p) {
            Player.Message(p, "%T/GBInsert [source id] [target id]");
            Player.Message(p, "%HInserts source block at target block's position in inventory");
        }
    }
}
