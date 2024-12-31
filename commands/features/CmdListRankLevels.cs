using System;
using System.Collections.Generic;

namespace MCGalaxy.Commands.Info
{
    public sealed class CmdListLevels : Command2
    {
        public override string name { get { return "ListRankLevels"; } }
        public override string type { get { return CommandTypes.Information; } }
        public override bool UseableWhenFrozen { get { return true; } }

        public override void Use(Player p, string message, CommandData data)
        {
            string[] maps = LevelInfo.AllMapNames();

            List<string> levels = new List<string>();

            string[] args = message.SplitSpaces();

            Group grp = null;

            if (args.Length > 0)
            {
                grp = Matcher.FindRanks(p, args[0]);
                if (grp == null) return;


                foreach (string map in maps)
                {
                    LevelConfig cfg = LevelInfo.GetConfig(map);
                    if (cfg == null) continue;

                    if (cfg.BuildMin == grp.Permission || cfg.VisitMin == grp.Permission)
                    {
                        levels.Add(map);
                    }
                }
            }

            else Help(p);

            if (levels.Count == 0)
            {
                p.Message("There are no levels with permission level: " + grp.ColoredName + ".");
                return;
            }

            p.Message("Levels with permission level: " + grp.ColoredName);
            string modifier = args.Length == 2 ? args[1] : "";
            Paginator.Output(p, levels, (lvlname) => grp.Color + lvlname, "/ListLevels " + grp.ColoredName, "levels", modifier);
        }

        public override void Help(Player p)
        {
            p.Message("&T/ListRankLevels [rank]");
            p.Message("&HShows a list of levels with perbuild/pervisit matching [rank].");
        }
    }
}