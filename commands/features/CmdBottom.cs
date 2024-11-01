// Sourced from https://github.com/UnknownShadow200/MCGalaxy/issues/523

using System;
using System.Collections.Generic;
using MCGalaxy.DB;
using MCGalaxy.SQL;

namespace MCGalaxy.Commands.Info
{
    public sealed class CmdBottom : Command
    {
        public override string name { get { return "Bottom"; } }
        public override string type { get { return CommandTypes.Information; } }

        public override void Use(Player p, string message)
        {
            string[] args = message.SplitSpaces();
            if (args.Length < 2) { Help(p); return; }

            int maxResults = 0, offset = 0;
            if (!CommandParser.GetInt(p, args[0], "Max results", ref maxResults, 1, 15)) return;

            TopStat stat = FindTopStat(args[1]);
            if (stat == null)
            {
                p.Message("&WUnrecognised type \"{0}\".", args[1]); return;
            }

            if (args.Length > 2)
            {
                if (!CommandParser.GetInt(p, args[2], "Offset", ref offset, 0)) return;
            }

            string limit = " LIMIT " + offset + "," + maxResults;
            string orderBy = stat.OrderBy.Replace("desc", "asc"); // Always show least results first
            List<string[]> stats = Database.GetRows(stat.Table, "DISTINCT Name, " + stat.Column,
                                                    "WHERE " + stat.Column + " != 0 " + // Ignore results with '0' values
                                                    "ORDER BY" + orderBy + limit);

            string title = stat.Title().Replace("Most", "Least");
            p.Message("&a{0}:", title);
            for (int i = 0; i < stats.Count; i++)
            {
                string name = PlayerInfo.GetColoredName(p, stats[i][0]);
                string value = stat.Formatter(stats[i][1]);
                p.Message("{0}) {1} &S- {2}", offset + (i + 1), name, value);
            }
        }

        static TopStat FindTopStat(string input)
        {
            foreach (TopStat stat in TopStat.Stats)
            {
                if (stat.Identifier.CaselessEq(input)) return stat;
            }
            return null;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Bottom [max results] [stat] <offset>");
            p.Message("&HPrints a list of players who have the " +
                           "least of a particular stat. Available stats:");
            p.Message("&f" + TopStat.Stats.Join(stat => stat.Identifier));
        }
    }
}
