using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events;
using MCGalaxy.SQL;
using MCGalaxy.Tasks;

namespace Core {
    public class TimeAFK : Plugin {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.2.5"; } }
        public override string name { get { return "TimeAFK"; } }

        public override void Load(bool startup) {
            InitDB();
            Command.Register(new CmdTimeAFK());
            Server.MainScheduler.QueueRepeat(CheckIdle, null, TimeSpan.FromSeconds(60));
        }

        public override void Unload(bool shutdown) {
        	Command.Unregister(Command.Find("TimeAFK"));
        }
        
        // Called every minute to check if player is idle or not
        void CheckIdle(SchedulerTask task) {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players) {
            	TimeSpan idleTime = DateTime.UtcNow - p.LastAction;
				if (idleTime.TotalMinutes < 1) break;
            	
	        	List<string[]> pRows = Database.GetRows("AFK", "Name, Spent", "WHERE Name=@0", p.truename);
		                    
		        if (pRows.Count == 0) {
		        	Database.AddRow("AFK", "Name, Spent", p.truename, 1);
		        }
		                    
		        else {
		        	int current = int.Parse(pRows[0][1]);
		            int newTime = 1 + current;
		                    	
		            Database.UpdateRows("AFK", "Spent=@1", "WHERE NAME=@0", p.truename, newTime);
	            }
	        }
        }
        
        ColumnDesc[] createDatabase = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("Spent", ColumnType.VarChar, 255), 
        };
        
        void InitDB() {
            Database.CreateTable("AFK", createDatabase);
        }
    }
	
	public sealed class CmdTimeAFK : Command2 {     
        public override string name { get { return "TimeAFK"; } }
        public override string type { get { return CommandTypes.Information; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
		
		int GetInt(string s) { return s == "" ? 0 : int.Parse(s); }
        
        public override void Use(Player p, string message, CommandData data) { // /PInfo <name>
            
            List<string[]> rows = Database.GetRows("AFK", "Name, Spent", "WHERE Name=@0", p.truename);
            
			int curTime = rows.Count == 0 ? 0 : int.Parse(rows[0][1]);
            p.Message("&STime spent idle: &b" + curTime + " minutes");
        }
        
        public override void Help(Player p) {
            p.Message("&T/TimeAFK - &HShows how long you've idled for.");
        }
    }
}
