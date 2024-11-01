using System;
using System.Collections.Generic;
using System.IO;

using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Config;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events;
using MCGalaxy.Tasks;

namespace Core
{
    public class DailyBonus : Plugin
    {
        public static PlayerExtList claimed; // Dates of when players have claimed rewards
        public static PlayerExtList streaks; // Consecutive amount of times claimed rewards

        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
        public override string name { get { return "DailyBonus"; } }

        public static SchedulerTask task;
        const string CONFIG_PATH = "plugins/DailyBonus/config.properties";

        public static class Config
        {
            [ConfigInt("type", "Rewards", 0)]
            public static int Type = 0; // 0 = players get on join, 1 = player types /daily, 2 = automatic daily after x time

            [ConfigInt("time", "Rewards", 0)]
            public static int Time = 30; // (non-afk) Time in minutes required to play on the server before automatically being given reward

            [ConfigInt("amount", "Rewards", 30)]
            public static int Amount = 30; // The amount given to the player

            [ConfigInt("increment", "Rewards", 1)]
            public static int Increment = 1; // Whether or not to increment amount given for consecutive logins

            [ConfigBool("enable-streaks", "Rewards", false)]
            public static bool EnableStreaks = false; // Increment amount for consecutive logins. E.g, day 1 = 30, day 2 = 31, day 3 = 32 etc

            static ConfigElement[] cfg;
            public static void Load()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.ParseFile(cfg, CONFIG_PATH, null);
            }

            public static void Save()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.SerialiseSimple(cfg, CONFIG_PATH, null);
            }
        }

        public override void Load(bool startup)
        {
            try { Directory.CreateDirectory("plugins/DailyBonus"); } catch { }

            if (!File.Exists(CONFIG_PATH)) Config.Save();
            Config.Load(); // Load config

            // Load lists so we can add/check data to them

            claimed = PlayerExtList.Load("plugins/DailyBonus/claimed.txt");
            streaks = PlayerExtList.Load("plugins/DailyBonus/streaks.txt");

            if (Config.Type == 0) OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.High);
            if (Config.Type == 1) Command.Register(new CmdDailyBonus());
            if (Config.Type == 2) Server.MainScheduler.QueueRepeat(DoTick, null, TimeSpan.FromMinutes(1));
        }

        public override void Unload(bool shutdown)
        {
            if (Config.Type == 0) OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
            if (Config.Type == 1) Command.Unregister(Command.Find("DailyBonus"));
            if (Config.Type == 2) Server.MainScheduler.Cancel(task);
        }

        void HandlePlayerConnect(Player p)
        {
            string date = DateTime.UtcNow.ToShortDateString();
            string lastDate = claimed.FindData(p.name);

            if (lastDate == null || lastDate != date) GiveReward(p, date, lastDate);
        }

        public static void GiveReward(Player pl, string date, string lastDate)
        {
            int reward = Config.Amount;

            if (lastDate == date) return; // Player has already claimed their daily bonus for the day

            int streak = 0;

            if (Config.EnableStreaks)
            {
                if (streaks.FindData(pl.name) != null) streak = int.Parse(streaks.FindData(pl.name));

                // Check to see if there is a 2 day gap between current date and last claimed date
                if (lastDate == null || (DateTime.Now - DateTime.Parse(lastDate)).Days >= 2) streaks.Update(pl.name, "1");
                else streaks.Update(pl.name, (streak + 1).ToString());

                streaks.Save();

                streak = int.Parse(streaks.FindData(pl.name)); // Check updated list
                reward += streak - 1; // Add streak increment to reward
            }

            pl.Message("&aYou claimed your daily bonus of &6" + reward + " &a" + Server.Config.Currency + "&a.");
            if (Config.EnableStreaks) pl.Message("&aYou currently have a streak of &b" + streak + "&a!");
            pl.SetMoney(pl.money + reward);

            claimed.Update(pl.name, date);
            claimed.Save();
        }

        public void DoTick(SchedulerTask t)
        {
            task = t;
            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (pl.IsAfk) continue;

                pl.Extras["SESSION_TIME"] = pl.Extras.GetInt("SESSION_TIME") + 1;

                if (pl.Extras.GetInt("SESSION_TIME") == Config.Time)
                {
                    string date = DateTime.UtcNow.ToShortDateString();
                    string lastDate = claimed.FindData(pl.name);

                    GiveReward(pl, date, lastDate);
                }
            }
        }
    }

    public sealed class CmdDailyBonus : Command2
    {
        public override string name { get { return "DailyBonus"; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandAlias[] Aliases { get { return new[] { new CommandAlias("Daily") }; } }
        public override string type { get { return CommandTypes.Economy; } }

        public override void Use(Player p, string message, CommandData data)
        {
            string date = DateTime.UtcNow.ToShortDateString();
            string lastDate = DailyBonus.claimed.FindData(p.name);

            if (lastDate == date)
            {
                p.Message("&cYou have already claimed your daily bonus for today.");
                return;
            }

            if (lastDate == null || lastDate != date) DailyBonus.GiveReward(p, date, lastDate);
        }

        public override void Help(Player p)
        {
            p.Message("&T/DailyBonus - &HClaims your daily bonus for today.");
        }
    }
}