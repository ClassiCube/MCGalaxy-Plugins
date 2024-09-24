// TODO: 
// - Freezes
// - Bans/tempbans

using System;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Commands.Moderation;
using MCGalaxy.Events;
using MCGalaxy.SQL;
using MCGalaxy.Tasks;

namespace Core
{
    public class SessionPunishments : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.2"; } }
        public override string name { get { return "SessionPunishments"; } }

        public override void Load(bool startup)
        {
            InitDB();

            // Unregister the default /mute command and use our new one instead
            Command.Unregister(Command.Find("Mute"));
            Command.Register(new CmdMute());

            Server.MainScheduler.QueueRepeat(CheckMutes, null, TimeSpan.FromSeconds(1));
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Mute"));
        }

        public static string FormatModTaskData(ModAction e)
        {
            long assign = DateTime.UtcNow.ToUnixTime();
            DateTime end = DateTime.MaxValue.AddYears(-1);

            if (e.Duration != TimeSpan.Zero)
            {
                try
                {
                    end = DateTime.UtcNow.Add(e.Duration);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // If user provided an extreme expiry time, ignore it
                }
            }

            long expiry = end.ToUnixTime();
            string assigner = e.Actor.name;
            return assigner + " " + assign + " " + expiry;
        }

        static void AddNote(ModAction e, string type)
        {
            if (!Server.Config.LogNotes) return;
            string src = e.Actor.name;

            string time = DateTime.UtcNow.ToString("dd/MM/yyyy");
            string data = e.Target + " " + type + " " + src + " " + time + " " +
                          e.Reason.Replace(" ", "%20") + " " + e.Duration.Ticks;
            Server.Notes.Append(data);
        }

        public static void LogAction(ModAction e, Player target, string action)
        {
            // TODO should use per-player nick settings
            string targetNick = e.Actor.FormatNick(e.Target);

            if (e.Announce)
            {
                Player who = PlayerInfo.FindExact(e.Target);
                Chat.Message(ChatScope.Global, e.FormatMessage(targetNick, action),
                             null, null, true);
            }
            else
            {
                Chat.MessageOps(e.FormatMessage(targetNick, action));
            }

            action = Colors.StripUsed(action);
            string suffix = "";
            if (e.Duration.Ticks != 0) suffix = " &Sfor " + e.Duration.Shorten();

            switch (e.Type)
            {
                case ModActionType.Frozen:
                    AddNote(e, "F"); break;
                case ModActionType.Kicked:
                    AddNote(e, "K"); break;
                case ModActionType.Muted:
                    AddNote(e, "M"); break;
                case ModActionType.Warned:
                    AddNote(e, "W"); break;
                case ModActionType.Ban:
                    string banType = e.Duration.Ticks != 0 ? "T" : "B";
                    AddNote(e, banType); break;
            }

            Logger.Log(LogType.UserActivity, "{0} was {1} by {2}",
                       e.Target, action, e.Actor.name + suffix);
        }

        // Called every second to serve as a tick for punishment durations
        void CheckMutes(SchedulerTask task)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl == null) continue;
                if (!pl.muted) continue;

                List<string[]> rows = Database.GetRows("Mutes", "Name, TimeRemaining", "WHERE Name=@0", pl.truename);
                if (rows.Count == 0) continue; // If player is not muted, don't bother checking time remaining

                int timeRemaining = int.Parse(rows[0][1]);

                // '-1' is used for permanent durations as 0 is used for measuring sentence completion
                if (timeRemaining == -1) continue;

                // If player has served the entirety of their sentence, unmute them
                if (timeRemaining <= 0)
                {
                    CmdMute.DoUnmute(Player.Console, pl.truename, "auto unmute");
                    Database.DeleteRows("Mutes", "WHERE Name=@0", pl.truename);
                    pl.muted = false;
                }

                else
                {
                    Database.UpdateRows("Mutes", "TimeRemaining=@1", "WHERE NAME=@0", pl.truename, timeRemaining - 1);
                }
            }
        }

        ColumnDesc[] createDatabase = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("TimeRemaining", ColumnType.Int32),
        };

        void InitDB()
        {
            Database.CreateTable("Mutes", createDatabase);
        }
    }

    public sealed class CmdMute : Command2
    {
        public override string name { get { return "Mute"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

        public override CommandAlias[] Aliases
        { get { return new[] { new CommandAlias("Unmute", "-unmute") }; } }
        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(3);
            string target;

            if (args[0].CaselessEq("-unmute"))
            {
                if (args.Length == 1) { Help(p); return; }
                target = PlayerInfo.FindMatchesPreferOnline(p, args[1]);
                if (target == null) return;

                if (!Server.muted.Contains(target))
                {
                    p.Message("{0}&S is not muted.", p.FormatNick(target));
                    return;
                }

                DoUnmute(p, target, args.Length > 2 ? args[2] : "");
                return;
            }

            target = PlayerInfo.FindMatchesPreferOnline(p, args[0]);

            if (target == null) return;

            if (Server.muted.Contains(target))
            {
                p.Message("{0}&S is already muted.", p.FormatNick(target));
                p.Message("You may unmute them with &T/Unmute {0}", target);
            }

            else
            {
                Group group = CheckTarget(p, data, "mute", target);
                if (group == null) return;

                DoMute(p, target, args);
            }
        }

        Group CheckTarget(Player p, CommandData data, string action, string target)
        {
            if (p.name.CaselessEq(target))
            {
                p.Message("You cannot {0} yourself", action); return null;
            }

            Group group = PlayerInfo.GetGroup(target);

            if (!Command.CheckRank(p, data, target, group.Permission, action, false)) return null;
            return group;
        }

        void DoMute(Player p, string target, string[] args)
        {
            TimeSpan duration = Server.Config.ChatSpamMuteTime;
            if (args.Length > 1)
            {
                if (!CommandParser.GetTimespan(p, args[1], ref duration, "mute for", "s")) return;
            }

            List<string[]> rows = Database.GetRows("Mutes", "Name, TimeRemaining", "WHERE Name=@0", p.truename);

            double time = duration.TotalSeconds;
            if (duration.TotalSeconds == 0) time = -1; // '-1' is used for permanent durations as 0 is used for measuring sentence completion

            if (rows.Count == 0)
            {
                Database.AddRow("Mutes", "Name, TimeRemaining", target, time);
            }

            else
            {
                Database.UpdateRows("Mutes", "TimeRemaining=@1", "WHERE NAME=@0", target, time);
            }

            string reason = args.Length > 2 ? args[2] : "";
            reason = ModActionCmd.ExpandReason(p, reason);
            if (reason == null) return;

            ModAction action = new ModAction(target, p, ModActionType.Muted, reason, duration);
            //OnModActionEvent.Call(action); // I left this commented out since it triggers the server's built-in punishment tick which ruins everything

            // These lists are used when a player connects to the server to automatically assign p.muted status as it resets when the player disconnects
            Server.muted.Update(action.Target, SessionPunishments.FormatModTaskData(action));
            Server.muted.Save();

            Player who = PlayerInfo.FindExact(target);
            if (who != null) who.muted = true;

            SessionPunishments.LogAction(action, who, "&8muted");
        }

        public static void DoUnmute(Player p, string target, string reason)
        {
            reason = ModActionCmd.ExpandReason(p, reason);
            if (reason == null) return;
            if (p.name == target) { p.Message("You cannot unmute yourself."); return; }

            List<string[]> rows = Database.GetRows("Mutes", "Name, TimeRemaining", "WHERE Name=@0", target);

            if (rows.Count == 0) return; // If player is not muted, don't bother deleting row

            ModAction action = new ModAction(target, p, ModActionType.Unmuted, reason);

            // These lists are used when a player connects to the server to automatically assign p.muted status as it resets when the player disconnects
            Server.muted.Remove(action.Target);
            Server.muted.Save();

            Player who = PlayerInfo.FindExact(target);
            if (who != null) who.muted = false;

            SessionPunishments.LogAction(action, who, "&aun-muted");

            // Delete row from the database since it is not being used anymore
            Database.DeleteRows("Mutes", "WHERE Name=@0", target);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Mute [player] <timespan> <reason>");
            p.Message("&H Mutes player for <timespan>, which defaults to");
            p.Message("&H the auto-mute timespan.");
            p.Message("&H If <timespan> is 0, the mute is permanent.");
            p.Message("&H For <reason>, @1 substitutes for rule 1, @2 for rule 2, etc.");
            p.Message("&T/Unmute [player] <reason>");
            p.Message("&H Unmutes player with optional <reason>.");
        }
    }
}
