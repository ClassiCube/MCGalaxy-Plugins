using System;
using MCGalaxy;

namespace MCGalaxy.Commands.CPE {
    public sealed class CmdAnnounce : Command2 {
        public override string name { get { return "Announce"; } }
        public override string shortcut { get { return "ann"; } }
        public override string type { get { return "other"; } }
            
        public override CommandPerm[] ExtraPerms {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can announce messages to the level they are in."),
                    new CommandPerm(LevelPermission.Admin, "can announce messages to the whole server.") }; }
        }
 
        public override void Use(Player p, string message, CommandData data) {
            string[] args = message.SplitSpaces(2);
            
            if (args[0].Length > 0) {
                if (args[0].CaselessEq("level") || args[0].CaselessEq("global")) {
                    if (args[0].CaselessEq("level"))  {
                        if (args[1].Length > 0) {
                            if (!HasExtraPerm(p, data.Rank, 1)) return;
                            foreach (Player pl in PlayerInfo.Online.Items) {
                                if (pl.level != p.level) continue;            
                                pl.SendCpeMessage(CpeMessageType.Announcement, args[1]);
                            }
                        } else { Help(p); }
                    }

                    else if (args[0].CaselessEq("global")) {
                        if (args[1].Length > 0) {
                            if (!HasExtraPerm(p, data.Rank, 2)) return;
                            foreach (Player pl in PlayerInfo.Online.Items) {   
                                pl.SendCpeMessage(CpeMessageType.Announcement, args[1]);
                            }
                        }
                        
                        else { Help(p); }
                    }
                }
                
                else {
                    p.SendCpeMessage(CpeMessageType.Announcement, message);
                }
            }
            
            else { Help(p); }
        }
 
        public override void Help(Player p) {
            p.Message("&T/Announce [message] &H- Displays a message on your screen.");
            p.Message("&T/Announce level [message] &H- Displays a message on players' screens in your level.");
            p.Message("&T/Announce global [message] &H- Displays a message on players' screens globally.");
        }
    }
}
