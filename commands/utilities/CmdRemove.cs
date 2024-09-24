// The command below should only be used when ABSOLUTELY NECESSARY. Once a player is removed from the playerbase, all of their information
// will be deleted and cannot be undone. Use at your own risk.

using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.SQL;

namespace Remove {
    public class CmdRemove : Command2 {
        public override string name { get { return "Remove"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return "moderation"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override void Use(Player p, string message) {
            Database.Execute("DELETE FROM Players WHERE Name=@0", message);    
        }

        public override void Help(Player p) {
            p.Message("/Remove [name] - Removes [name] from the playerbase.");
        }
    }
}
