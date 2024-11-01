//reference System.Core.dll

/* 
    You will need to replace all "secretcode" values with a random code.
	
	- To reward XP from an external plugin/command, use 'Command.Find("XP").Use(p, "secretcode " + [player] + " [xp amount]");'
    - To make it easier/harder to level up, modify the "0.02" value accordingly.

*/

using System;
using System.Collections.Generic;

using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.SQL;

namespace MCGalaxy
{
    public class XP : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.0"; } }
        public override string name { get { return "XP"; } }

        public override void Load(bool startup)
        {
            Command.Register(new CmdXP());
            InitDB();
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("XP"));
        }

        ColumnDesc[] createLevels = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("XP", ColumnType.Int32),
            new ColumnDesc("Level", ColumnType.Int32),
        };


        void InitDB()
        {
            Database.CreateTable("Levels", createLevels);
        }
    }

    public sealed class CmdXP : Command2
    {
        public override string name { get { return "XP"; } }
        public override string type { get { return "economy"; } }

        int GetInt(string s) { return s == "" ? 0 : int.Parse(s); }

        /// <summary>
        /// The amount of XP required until the player reaches the next level.
        /// </summary>

        int nextLevel(int userLevel)
        {
            return calculateLevel(userLevel + 1);
        }

        /// <summary>
        /// Calculates the amount of XP required to reach a specific level.
        /// </summary>

        int calculateLevel(int level)
        {
            // XP = (Level / 0.02) ^ 2
            return (int)(Math.Pow(level / 0.02, 2) / 100);
        }

        /// <summary>
        /// Checks to see whether or not the player has levelled up.
        /// </summary>

        int checkLevelUp(int curXP, int number)
        {
            // level = floor((0.02 * √(curXP + number)) * 10)
            double xp = (0.02 * Math.Sqrt(curXP + number)) * 10;
            return (int)Math.Floor(xp);
        }

        /// <summary>
        /// Checks to see whether or not the player has levelled down.
        /// </summary>

        int checkLevelDown(int curXP, int number)
        {
            if ((curXP - number) <= 0) return 0;

            // level = floor((0.02 * √(curXP + number)) * 10)
            double xp = (0.02 * Math.Sqrt(curXP - number)) * 10;
            return (int)Math.Floor(xp);
        }

        public override void Use(Player p, string message, CommandData data)
        {
            p.lastCMD = "secret";

            string[] args = message.SplitSpaces();

            if (args[0] == "secretcode")
            {
                // To add XP: /xp secretcode [name] [xp]
                if (args.Length < 3) { Help(p); return; }
                if (PlayerInfo.FindMatchesPreferOnline(p, args[1]) == null) return;
                List<string[]> rows = Database.GetRows("Levels", "Name, XP, Level", "WHERE Name=@0", args[1]);

                int number = int.Parse(args[2]);

                if (rows.Count == 0)
                {
                    int curXP = 0;
                    int newLevel = checkLevelUp(curXP, number);

                    Player pl = PlayerInfo.FindExact(args[1]); // Find person receiving XP
                    int curLevel = 0;
                    if (pl != null && curLevel != newLevel) pl.Message("You are now level &b" + newLevel);
                    Database.AddRow("Levels", "Name, XP, Level", args[1], args[2], newLevel);
                    return;
                }
                else
                {
                    int curXP = int.Parse(rows[0][1]); // First row, second column
                    int newLevel = checkLevelUp(curXP, number);

                    Player pl = PlayerInfo.FindExact(args[1]); // Find person receiving XP
                    int curLevel = GetInt(rows[0][2]);
                    if (pl != null && curLevel != newLevel) pl.Message("You are now level &b" + newLevel);

                    Database.UpdateRows("Levels", "XP=@1", "WHERE NAME=@0", args[1], curXP + number); // Give XP
                    Database.UpdateRows("Levels", "Level=@1", "WHERE NAME=@0", args[1], newLevel); // Give level
                }
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
        }
    }
}
