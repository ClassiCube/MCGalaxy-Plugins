//reference System.Core.dll
//reference mscorlib.dll

/* 
    A secret code is generated upon plugin load. You can view the code by opening plugins/XP/secretcode.txt, or by typing '/xp code' in game.
    If you wish to update the secret code in the text file, please do not make new lines. You can type '/server reload' to update the changes without unloading the plugin.	

	- To reward XP from an external plugin/command, use 'Command.Find("XP").Use(p, "secretcode " + [player] + " [xp amount]");'. 
    - For the above, 'secretcode' must be replaced with the secret code.
    - To make it easier/harder to level up, modify the "0.02" value accordingly.
*/

using System;
using System.Collections.Generic;
using System.IO;

using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.DB;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.SQL;

namespace MCGalaxy
{
    public class XP : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.0"; } }
        public override string name { get { return "_XP"; } }

        #region Variables
        // DO NOT MODIFY.
        public const string PATH                = "plugins/_XP/";
        public const string PATH_SECRET_CODE    = PATH + "secretcode.txt";
        public static string SECRET_CODE        = "notset";

        private TopStat levelStat, xpStat;
        #endregion

        public override void Load(bool startup)
        {
            if (!Directory.Exists(PATH)) Directory.CreateDirectory(PATH);
            SetSecretCode();

            OnConfigUpdatedEvent.Register(HandleConfigUpdated, Priority.Low);

            Command.Register(new CmdXP());
            InitDB();

            xpStat = new DBTopStat("XP", "Most XP", "Levels", "XP", TopStat.FormatInteger);
            levelStat = new DBTopStat("Levels", "Highest level", "Levels", "Level", TopStat.FormatInteger);
            TopStat.Register(xpStat);
            TopStat.Register(levelStat);
        }

        public override void Unload(bool shutdown)
        {
            OnConfigUpdatedEvent.Unregister(HandleConfigUpdated);

            Command.Unregister(Command.Find("XP"));
            
            TopStat.Unregister(xpStat);
            TopStat.Unregister(levelStat);
        }

        private void HandleConfigUpdated()
        {
            SetSecretCode();
        }

        private void SetSecretCode()
        {
            if (!File.Exists(PATH_SECRET_CODE))
            {
                SECRET_CODE = GenerateSecretCode(10);
                File.WriteAllText(PATH_SECRET_CODE, SECRET_CODE);
            }
            else
            {
                SECRET_CODE = File.ReadAllText(PATH_SECRET_CODE);
            }
        }

        public static string GenerateSecretCode(int length)
        {
            char[] chArray = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ/*\\_+-=$#@!()^".ToCharArray();
            string str = string.Empty;
            Random random = new Random();
            for (int i = 0; i < length; i++)
            {
                int index = random.Next(1, chArray.Length);
                if (!str.Contains(chArray.GetValue(index).ToString()))
                {
                    str = str + chArray.GetValue(index);
                }
                else
                {
                    i--;
                }
            }
            return str;
        }

        #region Database Related Methods
        ColumnDesc[] createXP = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("XP", ColumnType.Int32),
            new ColumnDesc("Level", ColumnType.Int32),
        };


        void InitDB()
        {
            Database.CreateTable("Levels", createXP);
        }

        public static void GetXPData(string name, out int level, out int xp)
        {
            List<string[]> rows = Database.GetRows("Levels", "Name, XP, Level", "WHERE Name=@0", name);
            level = rows.Count == 0 ? 0 : int.Parse(rows[0][2]);
            xp = rows.Count == 0 ? 0 : int.Parse(rows[0][1]);
        }
        #endregion
    }

    public sealed class CmdXP : Command2
    {
        public override string name { get { return "XP"; } }
        public override string type { get { return CommandTypes.Economy; } }
        public override CommandPerm[] ExtraPerms
        {
            get
            {
                return new[] { new CommandPerm(LevelPermission.Operator, "can manage the command") };
            }
        }

        static int GetInt(string s) { return s == "" ? 0 : int.Parse(s); }

        /// <summary>
        /// The amount of XP required until the player reaches the next level.
        /// </summary>

        static int nextLevel(int userLevel)
        {
            return calculateLevel(userLevel + 1);
        }

        /// <summary>
        /// Calculates the amount of XP required to reach a specific level.
        /// </summary>

        static int calculateLevel(int level)
        {
            // XP = (Level / 0.02) ^ 2
            return (int)(Math.Pow(level / 0.02, 2) / 100);
        }

        /// <summary>
        /// Checks to see whether or not the player has levelled up.
        /// </summary>

        static int checkLevelUp(int curXP, int number)
        {
            // level = floor((0.02 * √(curXP + number)) * 10)
            double xp = (0.02 * Math.Sqrt(curXP + number)) * 10;
            return (int)Math.Floor(xp);
        }

        /// <summary>
        /// Checks to see whether or not the player has levelled down.
        /// </summary>

        static int checkLevelDown(int curXP, int number)
        {
            if ((curXP - number) <= 0) return 0;

            // level = floor((0.02 * √(curXP + number)) * 10)
            double xp = (0.02 * Math.Sqrt(curXP - number)) * 10;
            return (int)Math.Floor(xp);
        }

        public static void UpdateXP(string player, int number, Player actor = null)
        {
            List<string[]> rows = Database.GetRows("Levels", "Name, XP, Level", "WHERE Name=@0", player);

            const string MANUAL_GIVE_MSG = "{actor} &Sgave you &b{number} &SXP.";
            const string MANUAL_TAKE_MSG = "{actor} &Stook &b{number}&S XP from you.";

            const string MANUAL_GIVE_MSG_ACTOR = "You gave {receiver} &b{number} &SXP.";
            const string MANUAL_TAKE_MSG_ACTOR = "You took &b{number} &SXP from {receiver}&S.";

            if (actor != null)
            {
                string message = 0 > number ? MANUAL_TAKE_MSG_ACTOR : MANUAL_GIVE_MSG_ACTOR;
                actor.Message(message.Replace("{receiver}", actor.FormatNick(player)).Replace("{number}", Math.Abs(number).ToString()));
            }

            if (rows.Count == 0)
            {
                int curXP = 0;
                int newLevel = checkLevelUp(curXP, number);

                Player pl = PlayerInfo.FindExact(player); // Find person receiving XP
                int curLevel = 0;

                Database.AddRow("Levels", "Name, XP, Level", player, number, newLevel);
                if (pl != null && curLevel != newLevel)
                {
                    if (actor != null)
                    {
                        string message = 0 > number ? MANUAL_TAKE_MSG : MANUAL_GIVE_MSG;
                        pl.Message(
                            message
                            .Replace("{actor}", pl.FormatNick(actor))
                            .Replace("{number}", Math.Abs(number).ToString())
                            );
                    }

                    pl.SetPrefix();
                    pl.Message("You are now level &b" + newLevel);
                }
                return;
            }
            else
            {
                int curXP = int.Parse(rows[0][1]); // First row, second column
                int newLevel = checkLevelUp(curXP, number);

                Player pl = PlayerInfo.FindExact(player); // Find person receiving XP
                int curLevel = GetInt(rows[0][2]);

                Database.UpdateRows("Levels", "XP=@1, Level=@2", "WHERE NAME=@0", player, curXP + number, newLevel); // Update XP and level
                if (pl != null && curLevel != newLevel)
                {
                    if (actor != null)
                    {
                        string message = 0 > number ? MANUAL_TAKE_MSG : MANUAL_GIVE_MSG;
                        pl.Message(message = message
                            .Replace("{actor}", pl.FormatNick(actor))
                            .Replace("{number}", Math.Abs(number).ToString())
                        );
                    }

                    pl.SetPrefix();
                    pl.Message("You are now level &b" + newLevel);
                }
            }
        }


        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces();

            if (args[0] == XP.SECRET_CODE)
            {
                p.lastCMD = "nothing";

                // To add XP: /xp secretcode [name] [xp]
                if (args.Length < 3) { Help(p); return; }
                if (PlayerInfo.FindMatchesPreferOnline(p, args[1]) == null) return;

                int number = int.Parse(args[2]);

                UpdateXP(args[1], number);
            }
            else if (args[0].CaselessEq("code"))
            {
                if (!CheckExtraPerm(p, data, 1)) return;

                p.Message("The secret code is:");
                p.Message("&f" + XP.SECRET_CODE);
                p.Message("Open the chat and click the above code, then press &bCtrl + C&S to copy it.");
            }
            else if (args[0].CaselessEq("give"))
            {
                if (!CheckExtraPerm(p, data, 1)) return;
                if (3 > args.Length) { Help(p); return; }
                string matched = PlayerInfo.FindMatchesPreferOnline(p, args[1]);
                if (matched == null) return;

                int number = 0;
                if (!CommandParser.GetInt(p, args[2], "XP", ref number, 1)) return;

                UpdateXP(matched, number, p);
            }
            else if (args[0].CaselessEq("take"))
            {
                if (!CheckExtraPerm(p, data, 1)) return;
                if (!CheckExtraPerm(p, data, 1)) return;
                if (3 > args.Length) { Help(p); return; }
                string matched = PlayerInfo.FindMatchesPreferOnline(p, args[1]);
                if (matched == null) return;

                int number = 0;
                if (!CommandParser.GetInt(p, args[2], "XP", ref number, 1)) return;

                UpdateXP(matched, -number, p);
            }

            else
            {
                string pl = message.Length == 0 ? p.truename : args[0];
                List<string[]> rows = Database.GetRows("Levels", "Name,XP,Level", "WHERE Name=@0", pl);

                int userLevel = rows.Count == 0 ? 0 : int.Parse(rows[0][2]);  // User level
                int curXP = rows.Count == 0 ? 0 : int.Parse(rows[0][1]);  // User XP

                if (message.Length == 0 || args[0] == p.name)
                {
                    p.Message("&eYour Information:");
                }
                else
                {
                    if (PlayerInfo.FindMatchesPreferOnline(p, args[0]) == null) return;
                    p.Message("&b" + args[0] + "&e's Information:");
                }


                if (userLevel == 100)
                {
                    p.Message("&5Level: &6" + userLevel + " (&bmax level)");
                }

                else
                {
                    p.Message("&5Level: &6" + userLevel + " (&b" + curXP + "xp/" + nextLevel(userLevel) + "xp&6)");
                }
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/XP - &HShows your level and current XP needed to level up.");
            p.Message("&T/XP [player] - &HShows [player]'s level and current XP needed to level up.");
            p.Message("&T/XP [secret code] [player] [xp] - &HGives [player] XP.");
            if (HasExtraPerm(p, p.Rank, 1))
            {
                p.Message("&T/XP code - &HViews the secret code.");
                p.Message("&T/XP give [player] [xp] - &HIssues a player XP.");
                p.Message("&T/XP take [player] [xp] - &HTakes XP away from player.");
            }
        }
    }
}
