using System;
using System.Collections.Generic;
using System.IO;

using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Config;
using MCGalaxy.Games;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events;

namespace MCGalaxy.Games
{
    public class StaffEligibility : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.2"; } }
        public override string name { get { return "StaffEligibility"; } }

        public class Config
        {
            [ConfigString("valid-message", "Extra", "&a+")]
            public static string ValidMessage = "&a+";

            [ConfigString("invalid-message", "Extra", "&cx")]
            public static string InvalidMessage = "&cx";

            [ConfigBool("force-time-spent", "Extra", true)]
            public static bool ForceTimeSpent = true;

            [ConfigString("min-time-spent", "Extra", "1d")]
            public static string MinTimeSpent = "1d";

            [ConfigBool("force-max-notes", "Extra", true)]
            public static bool ForceMaxNotes = true;

            [ConfigInt("max-notes", "Extra", 5)]
            public static int MaxNotes = 5;

            [ConfigBool("force-recent-notes", "Extra", true)]
            public static bool ForceRecentNotes = true;

            [ConfigString("last-note-recency", "Extra", "12w")]
            public static string LastNoteRecency = "12w";

            [ConfigBool("force-discord", "Extra", false)]
            public static bool ForceDiscord = false;

            static ConfigElement[] cfg;
            public void Load()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.ParseFile(cfg, "./plugins/StaffEligibility/config.properties", this);
            }

            public void Save()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.SerialiseSimple(cfg, "./plugins/StaffEligibility/config.properties", this);
            }
        }

        public static void MakeConfig()
        {
            using (StreamWriter w = new StreamWriter("./plugins/StaffEligibility/config.properties"))
            {
                w.WriteLine("# Edit the settings below to modify how the plugin operates.");
                w.WriteLine("# The message the token should show when a player has met the requirement.");
                w.WriteLine("valid-message = &a+");
                w.WriteLine("# The message the token should show when a player has not met the requirement.");
                w.WriteLine("invalid-message = &cx");
                w.WriteLine("# Whether or not to force players to have a minimum time spent on the server.");
                w.WriteLine("force-time-spent = true");
                w.WriteLine("# If above is true, the minimum amount of time players are required to spend on the server. Use timespan formatting e.g, '7d12h32m for 7 days, 12 hours and 32 minutes.");
                w.WriteLine("min-time-spent = 1d");
                w.WriteLine("# Whether or not players should be forced to have under a certain amount of /notes.");
                w.WriteLine("force-max-notes = true");
                w.WriteLine("# If above is true, the maximum amount of notes a player can have.");
                w.WriteLine("max-notes = 5");
                w.WriteLine("# Whether or not players should be allowed to have recent /notes.");
                w.WriteLine("force-recent-notes = true");
                w.WriteLine("# If above is true, the minimum amount of time a note has to have passed to be deemed inactive.");
                w.WriteLine("last-note-recency = 12w");
                //w.WriteLine("# Whether or not players should be forced to join the Discord server. NOTE: Requires DiscordVerify to work.");
                //w.WriteLine("force-discord = false");
                w.WriteLine();
            }
        }

        public static Config cfg = new Config();

        static string TimeSpentToken(Player p)
        {
            try
            {
                TimeSpan requiredTime = TimeSpan.Zero;

                if (!CommandParser.GetTimespan(p, Config.MinTimeSpent, ref requiredTime, "", "m")) return "Invalid timespan";
                if (p.TotalTime < requiredTime) return Config.InvalidMessage;
                else return Config.ValidMessage;
            }

            catch (FormatException)
            {
                return "Invalid timespan";
            }
        }

        static string MaxNotesToken(Player p)
        {
            List<string> notes = Server.Notes.FindAllExact(p.name);
            if (notes.Count >= Config.MaxNotes) return Config.InvalidMessage;
            else return Config.ValidMessage;
        }

        static string RecentNotesToken(Player p)
        {
            try
            {
                List<string> notes = Server.Notes.FindAllExact(p.name);
                foreach (string note in notes)
                {
                    string[] args = note.SplitSpaces();
                    if (args.Length <= 3) continue;

                    TimeSpan noteRecency = DateTime.Parse(args[3]).TimeOfDay;
                    TimeSpan lastNoteRecency = TimeSpan.Zero;

                    if (!CommandParser.GetTimespan(p, Config.LastNoteRecency, ref lastNoteRecency, "", "d")) return "Invalid timespan";

                    if (noteRecency < lastNoteRecency) return Config.InvalidMessage;
                    else return Config.ValidMessage;
                }

                return Config.ValidMessage;
            }

            catch (FormatException)
            {
                return "Invalid timespan";
            }
        }

        public override void Load(bool startup)
        {
            Directory.CreateDirectory("./plugins/StaffEligibility");
            if (!File.Exists("./plugins/StaffEligibility/config.properties")) MakeConfig();

            // Initialize config
            cfg.Load();

            if (Config.ForceTimeSpent) ChatTokens.Standard.Add(new ChatToken("$apply_timespent", "apply_timespent", TimeSpentToken));
            if (Config.ForceMaxNotes) ChatTokens.Standard.Add(new ChatToken("$apply_maxnotes", "apply_maxnotes", MaxNotesToken));
            if (Config.ForceRecentNotes) ChatTokens.Standard.Add(new ChatToken("$apply_recentnotes", "apply_recentnotes", RecentNotesToken));
        }

        public override void Unload(bool shutdown) { }
    }
}