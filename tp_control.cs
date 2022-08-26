using System;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Commands;

namespace PluginTpControl {
    public sealed class Core : Plugin {
        public override string creator { get { return "Goodly"; } }
        public override string name { get { return "tp_control"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.2"; } }
        
        public override void Load(bool startup) {
            OnPlayerCommandEvent.Register(OnPlayerCommand, Priority.Low);
        }
        
        public override void Unload(bool shutdown) {
            OnPlayerCommandEvent.Unregister(OnPlayerCommand);
        }
        
        static bool CanTpWithinLevel(Player p) {
            if (LevelInfo.IsRealmOwner(p.name, p.level.name)) { return true; } //realm owner can always tp in their own map
            if (p.level.Config.MOTD.Contains("+tp")) { return true; }
            if (p.level.Config.MOTD.Contains("-tp") || !Hacks.CanUseHacks(p)) {
                p.Message("&cYou are not allowed to teleport within this level.");
                p.cancelcommand = true; return false;
            }
            return true;
        }
        static bool CanTpToPlayer(Player p, string cmd, string args, Player who) {
            if (LevelInfo.IsRealmOwner(p.name, who.level.name)) { return true; } //realm owner can always tp into their own map
            if (who.level == p.level) { return CanTpWithinLevel(p); }
            if (who.level.Config.MOTD.Contains("+tp")) { return true; }
            
            //we can't use CanUseHacks(who) because you shouldn't be able to directly TP to an op who is using maphack in a parkour map
            if (who.level.Config.MOTD.Contains("-hax") || who.level.Config.MOTD.Contains("-tp")) {
                p.Message("&cYou are not allowed to directly teleport to {0}&c because teleporting is disabled in the map they are in.", who.DisplayName);
                p.Message("Using %b/goto {0} %Sinstead...", who.level.name);
                Command.Find("goto").Use(p, who.level.name);
                Logger.Log(LogType.CommandUsage, "{0} used /{1} {2}", p.name, "goto", who.level.name);
                p.lastCMD = cmd + " " + args;
                p.cancelcommand = true; return false;
            }
            return true;
        }
        
        void OnPlayerCommand(Player p, string cmd, string args, CommandData data) {
            
            if (p.group.Permission >= LevelPermission.Operator) { return; } //op and above can always tp anywhere
            
            if (cmd.CaselessEq("tp")) {
                if (args.Length == 0) { return; }
                string[] bits = args.SplitSpaces();
                
                //TPing to coords case
                if (bits.Length >= 3 && data.Context != CommandContext.MessageBlock && !CanTpWithinLevel(p)) { return; }
                
                //tp to bot case
                if (args.CaselessStarts("bot ") && !CanTpWithinLevel(p)) { return; }
                
                //tp to player case
                if (bits.Length == 1) {
                    Player who = PlayerInfo.FindMatches(p, bits[0]);
                    if (who == null) { p.cancelcommand = true; return; } //if target is null, we're done. Cancel the command to avoid the double "player not found" message
                    if (!CanTpToPlayer(p, cmd, args, who)) { return; }
                }
            }
            
        }
        
    }
}