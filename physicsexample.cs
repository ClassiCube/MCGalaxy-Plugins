using System;
using MCGalaxy.Blocks.Physics;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Tasks;
using BlockID = System.UInt16;

namespace MCGalaxy {
	
	public sealed class PluginPhysicsExample : Plugin {
		public override string name { get { return "physicsexample"; } }
		public override string MCGalaxy_Version { get { return "1.9.4.1"; } }
        //This name is used to determine who to send debug text to
        static string author = "goodlyay+";
		public override string creator { get { return author; } }
        
        //The level we want to add a custom physics block to.
        static string physicsLevelName = "goodlyay+";
        
        //This is a server-side block ID. Client-side this is 103.
        static BlockID customPhysicsBlock = 359; 
            //You can find the server-side block ID in a custom command with:
            //Vec3S32 pos = p.Pos.FeetBlockCoords;
            //p.Message("Server-side BlockID at this location is {0}", p.level.GetBlock((ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z));
        
		public override void Load(bool startup) {
            
            //The map we want to add a physics block to might already be loaded when the plugin starts, thus we have to add it right away in that case.
			Level[] levels = LevelInfo.Loaded.Items;
			foreach (Level lvl in levels) {
                if (lvl.name == physicsLevelName) {
                   lvl.PhysicsHandlers[customPhysicsBlock] = DoBalloon;
                }
			}
            
            //Otherwise, we will look for when it loads using an event
            OnBlockHandlersUpdatedEvent.Register(OnBlockHandlersUpdated, Priority.Low);
		}
		public override void Unload(bool shutdown) {
            OnBlockHandlersUpdatedEvent.Unregister(OnBlockHandlersUpdated);
		}
        
		static void OnBlockHandlersUpdated(Level lvl, BlockID block) {
            if (lvl.name != physicsLevelName) { return; }
            if (block != customPhysicsBlock) { return; }
            lvl.PhysicsHandlers[customPhysicsBlock] = DoBalloon;
		}
        
        //Like sand, but falls up
        static void DoBalloon(Level lvl, ref PhysInfo C) {
            ushort x = C.X, y = C.Y, z = C.Z;
            int index = C.Index;
            bool movedUp = false;
            ushort yCur = y;
            
            do {
                index = lvl.IntOffset(index, 0, 1, 0); yCur++;// Get block above each loop
                BlockID cur = lvl.GetBlock(x, yCur, z);
                if (cur == Block.Invalid) break;
                bool hitBlock = false;
                
                switch (cur) {
                    case Block.Air:
                    case Block.Water:
                    case Block.Lava:
                        movedUp = true;
                        break;
                        //Adv physics crushes plants
                    case Block.Sapling:
                    case Block.Dandelion:
                    case Block.Rose:
                    case Block.Mushroom:
                    case Block.RedMushroom:
                        if (lvl.physics > 1) movedUp = true;
                        break;
                    default:
                        hitBlock = true;
                        break;
                }
                if (hitBlock || lvl.physics > 1) break;
            } while (true);            

            if (movedUp) {
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
                if (lvl.physics > 1)
                    lvl.AddUpdate(index, C.Block);
                else
                    lvl.AddUpdate(lvl.IntOffset(index, 0, -1, 0), C.Block);
                
                //This method is private, can't be used in plugins at the moment
                // ActivateablePhysics.CheckNeighbours(lvl, x, y, z);
            }
            C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }
        
        
        static void MsgDebugger(string message, params object[] args) {
            Player debugger = PlayerInfo.FindExact(PluginPhysicsExample.author); if (debugger == null) { return; }
            debugger.Message(message, args);
        }
        
	}
	
}
