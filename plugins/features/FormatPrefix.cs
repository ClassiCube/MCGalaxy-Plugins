//reference System.dll

/*
 * Omegabuild Development
 * Author: SpicyCombo
 */

using System;
using System.Diagnostics;
using System.Collections.Generic;

using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Tasks;
using MCGalaxy.SQL;
using System.IO;

namespace External
{
    public class FormatPrefix : Plugin
    {
        public override string name { get { return "FormatPrefix"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.2"; } }
        public override string creator { get { return "SpicyCombo"; } }

        // yeah
        public const string referee_tag = "&2[Ref] ";


        static int GetLevel(string name)
        {
            List<string[]> rows = Database.GetRows("Levels", "Name, XP, Level", "WHERE Name=@0", name);
            int level = rows.Count == 0 ? 0 : int.Parse(rows[0][2]);
            return level;
        }

        static string Path; const string key = "HIDDEN_PREFIX_KEY";

        public override void Load(bool load)
        {
            AddAllData();

            Command.Register(new CmdHidePrefix());
            OnSettingPrefixEvent.Register(FormatName, Priority.Normal);
            OnPlayerConnectEvent.Register(HandlePlayerConnection, Priority.High);

            Path = "plugins/" + name + "/";
        }

        public override void Unload(bool unload)
        {
            RemoveAllData();

            Command.Unregister(Command.Find("HidePrefix"));
            OnSettingPrefixEvent.Unregister(FormatName);
            OnPlayerConnectEvent.Unregister(HandlePlayerConnection);
        }

        void AddAllData()
        {
            foreach (Player p in PlayerInfo.Online.Items)
            {
                PlayerPrefixSetting pps = new PlayerPrefixSetting();
                pps.Load(p);

                p.Extras[key] = pps;
            }
        }

        void RemoveAllData()
        {
            foreach (Player p in PlayerInfo.Online.Items)
            {
                p.Extras.Remove(key);
            }
        }

        static PlayerPrefixSetting Get(Player p)
        {
            if (!p.Extras.Contains(key))
            {
                PlayerPrefixSetting pps = new PlayerPrefixSetting();
                pps.Load(p);
                p.Extras[key] = pps;
                return pps;
            }

            object data = p.Extras[key];

            if (data is PlayerPrefixSetting)
            {
                return (PlayerPrefixSetting)data;
            }
            else
            {
                PlayerPrefixSetting pps = new PlayerPrefixSetting();
                pps.Load(p);
                p.Extras[key] = pps;
                return pps;
            }
        }

        class PlayerPrefixSetting
        {
            public bool Level;

            public void Load(Player p, bool connect = false)
            {
                string path = Path + p.name + ".txt";
                if (!File.Exists(path)) return;

                try
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string line in lines)
                    {
                        if (line == "&level") { Level = true; continue; }
                    }
                }
                catch (IOException ex)
                {
                    Logger.LogError("Error loading prefix settings for " + p.name, ex);
                }

                bool hidingPrefix = Level;
                if (connect && hidingPrefix)
                    p.Message("&cType &a/HidePrefix list &cto see hidden chat prefixes");
            }

            public void Save(Player p)
            {
                string path = Path + p.name + ".txt";
                if (!Directory.Exists(Path))
                    Directory.CreateDirectory(Path);

                try
                {
                    using (StreamWriter w = new StreamWriter(path))
                    {
                        if (Level) w.WriteLine("&level");
                    }
                }
                catch (IOException ex)
                {
                    Logger.LogError("Error saving prefix settings for " + p.name, ex);
                }
            }

            public void Output(Player p)
            {
                if (Level) p.Message("&cHiding level prefixes");
            }
        }

        void HandlePlayerConnection(Player p)
        {
            PlayerPrefixSetting pps = new PlayerPrefixSetting();
            pps.Load(p, true);

            p.Extras[key] = pps;
        }

        public void FormatName(Player p, List<string> list)
        {
            PlayerPrefixSetting pps = Get(p);
            bool change = false;

            if (p.Game.Referee && list.Contains(referee_tag))
            {
                change = true;
                int index = list.IndexOf(referee_tag);
                list.Remove(referee_tag);
                list.Insert(index, "&2(R) ");
            }

            if (!pps.Level)
            {
                change = true;
                int userLevel = GetLevel(p.name);

                list.Insert(0, "&a" + userLevel.ToString() + " " + p.color);
            }

            if (change)
                p.prefix = list.Join(" ");
        }

        public class CmdHidePrefix : Command2
        {
            public override string name { get { return "HidePrefix"; } }

            public override string type { get { return CommandTypes.Chat; } }

            public override void Use(Player p, string message, CommandData data)
            {
                string[] args = message.SplitSpaces();
                string action = args[0].ToLower();

                if (message.Length == 0) { Help(p); return; }


                if (action == "level") { Toggle(p, ref Get(p).Level, "{0} hiding level prefixes"); return; }
                if (action == "list")
                {
                    PlayerPrefixSetting pps = Get(p);
                    pps.Output(p);
                }
                else
                {
                    Help(p);
                }
            }
            static void Toggle(Player p, ref bool hide, string format)
            {
                hide = !hide;
                if (format.StartsWith("{0}"))
                {
                    p.Message(format, hide ? "&cNow" : "&aNo longer");
                }
                else
                {
                    p.Message(format, hide ? "no longer" : "now", hide ? "&c" : "&a");
                }

                Get(p).Save(p);

                p.SetPrefix();
            }

            public override void Help(Player p)
            {
                p.Message("&T/HidePrefix list");
                p.Message("&HShows your currently hidden prefixes");
                p.Message("&T/HidePrefix [prefix]");
                p.Message("&HHides/Unhides [prefix] from being displayed on your name");
                p.Message("&HUse &T/Help HidePrefix special&H for available prefixes to hide.");
            }

            public override void Help(Player p, string message)
            {
                if (message.CaselessEq("special"))
                {
                    p.Message("&HAvailable prefixes to hide for &T/HidePrefix");
                    p.Message("&H level - Hides your level prefix");
                }
                else
                {
                    Help(p);
                }
            }

        }
    }
}
