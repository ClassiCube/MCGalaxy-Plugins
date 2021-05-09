using System;
using MCGalaxy;
using MCGalaxy.Blocks;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Events.PlayerEvents;
using BlockID = System.UInt16;


namespace MCGalaxy {
	
	public sealed class PluginPlayerClickExample : Plugin {
		public override string name { get { return "PlayerClickExample"; } }
		public override string MCGalaxy_Version { get { return "1.9.0.5"; } }
		public override string creator { get { return "not goodly"; } }
		
		public override void Load(bool startup) {
			OnPlayerClickEvent.Register(HandlePlayerClick, Priority.High);
		}
		
		public override void Unload(bool shutdown) {
			OnPlayerClickEvent.Unregister(HandlePlayerClick);
		}
		
		// The possible values of the Enums used in HandlePlayerClick
		//public enum MouseButton { Left, Right, Middle }  
		//public enum MouseAction { Pressed, Released }
		//public enum TargetBlockFace { AwayX, TowardsX, AwayY, TowardsY, AwayZ, TowardsZ, None }
		
		
		static void HandlePlayerClick
		(
		Player p,
		MouseButton button, MouseAction action,
		ushort yaw, ushort pitch,
		byte entity, ushort x, ushort y, ushort z,
		TargetBlockFace face
		)
		{
			//you can uncomment this to make it only work in maps that have both buildable and deletable off
			// if (p.level.Config.Deletable && p.level.Config.Buildable) { return; }
			
			// as an example of using the above enums, you can uncomment this to do nothing and quit if the player just used pick-block button.
			// if (button == MouseButton.Middle) { return; }
			
			
			BlockID clickedBlock;
			clickedBlock = p.level.GetBlock(x, y, z);
			//clickedBlock is now the block ID that was clicked.
			//This ID is MCGalaxy block ID, which means it's not mapped the same as client block ID.
			//However, 0-65 are still identical.
			
			//Example of running /place to put a stone block at the coordinates the player has clicked.
			Command.Find("place").Use(p, "stone "+x+" "+y+" "+z+"");
			
			
		}
	
}
