/*
    Copyright 2011 MCForge
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
namespace MCGalaxy.Commands {
    
    public sealed class CmdImpersonate : Command2 {
        public override string name { get { return "Impersonate"; } }
        public override string shortcut { get { return "imp"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Nobody; } }
        
        public override void Use(Player p, string message, CommandData data) {
            string[] args = message.SplitSpaces(2);
            if (args.Length == 1) { Help(p); return; }
            
            Player who = PlayerInfo.FindMatches(p, args[0]);
            if (who == null) { Help(p); return; }
            if (who.muted)   { Player.Message(p, "Cannot impersonate a muted player"); return; }
            
            if (CheckRank(p, data, who, "impersonate", false)) {
                Chat.MessageChat(who, "λFULL: &f" + args[1], null, true);
            }
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/Impersonate [player] [message]");
            Player.Message(p, "%HSends a message as if it came from [player]");
        }
    }
}
