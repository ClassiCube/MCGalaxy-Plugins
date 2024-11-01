using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Eco;
using MCGalaxy.Events;
using MCGalaxy.Events.EntityEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Tasks;

namespace MCGalaxy {

    public class Lottery : Plugin {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
        public override string name { get { return "Lottery"; } }

        public override void Load(bool startup) {
            Command.Register(new CmdLottery());
            OnPlayerDisconnectEvent.Register(HandlePlayerDisconnect, Priority.High);
        }

        public override void Unload(bool shutdown) {
            Command.Unregister(Command.Find("Lottery"));
            OnPlayerDisconnectEvent.Unregister(HandlePlayerDisconnect);
        }
        
        void HandlePlayerDisconnect(Player p, string reason) {
            if (CmdLottery.Lottery.Contains(p.truename)) {
                CmdLottery.Lottery.Remove(p.truename);
                // Remove comment if you want player to be refunded when they disconnect (not recommended)
                // p.SetMoney(p.money + 10);
            }
        }
    }
    
    public sealed class CmdLottery : Command2 {
        public override string name { get { return "Lottery"; } }
        public override string shortcut { get { return "luck"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool museumUsable { get { return true; } }
        //public override CommandEnable Enabled { get { return CommandEnable.Zombie | CommandEnable.Lava; } } //remove the // if you want /lottery allowed only for ZS or LS.
        public override bool SuperUseable { get { return false; } }
        
        public static VolatileArray<string> Lottery = new VolatileArray<string>();
        
        void DoLottery() {
            string[] players = Lottery.Items;
            if (players.Length == 0) return;

            // Ensure the players are actually online
            List<Player> online = new List<Player>(players.Length);
            foreach (string name in players) {
                Player pl = PlayerInfo.FindExact(name);
                if (pl == null) continue;
                online.Add(pl);
                Economy.EcoStats stats = Economy.RetrieveStats(pl.name);
                stats.TotalSpent += 10;
                Economy.UpdateStats(stats);
            }
            if (online.Count == 0) return;

            int amount = 10;
            Player winner = online[0];
            if (online.Count == 1) {
                winner.Message("Your money was refunded as you were " +
                                   "the only player still in the lottery.");
            } else {
                Random rand = new Random();
                winner = online[rand.Next(online.Count)];
                amount = 9 * online.Count;
               Chat.Message(ChatScope.Global, "&b" + winner.truename + " %7won the lottery for &a"
                                        + amount + " " + Server.Config.Currency + "&7.", null, null, true);
            }
            Lottery.Clear();
            winner.SetMoney(winner.money + amount);
        }
        
        void DoLotteryTick(SchedulerTask task) {
            DoLottery();
            Server.MainScheduler.Cancel(task);
        }

        public override void Use(Player p, string message, CommandData data) {
            string[] players = Lottery.Items;
            if (message == "list") {
                if (players.Length != 0) p.Message("Players in the lottery: " + players.Join());
                else p.Message("Nobody has entered the lottery yet");
                return;
            }

            if (p.money < 10) {
                p.Message("You need &f10 " + Server.Config.Currency + " %Sto enter the lottery."); return;
            }

            for (int i = 0; i < players.Length; i++) {
                if (players[i].CaselessEq(p.name)) {
                    p.Message("You are already in the lottery, which has &a"
                                       + players.Length + " %Splayers in it."); return;
                }
            }
            
            if (players.Length == 0) {
                Chat.Message(ChatScope.Global, "&b" + p.truename + " %7started the lottery. Type %b/lottery %7to join.", null, null, true);
                Server.MainScheduler.QueueRepeat(DoLotteryTick, null, TimeSpan.FromSeconds(30));
            }
            
            else {
                Chat.Message(ChatScope.Global, "&b" + p.truename + " %7entered the lottery.", null, null, true);
            }

            p.SetMoney(p.money - 10);
            Lottery.Add(p.name);
        }

        public override void Help(Player p) {
            p.Message("&T/Lottery %H- Enters lottery for &f10 " + Server.Config.Currency);
            p.Message("&T/Lottery list %H- Lists players in lottery");
            p.Message("&HThe winner is calculated at the end of each round.");
            p.Message("&HYou are &cnot refunded %Hif you disconnect.");
        }
    }
}
