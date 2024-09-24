using System;
using MCGalaxy;
using MCGalaxy.DB;

namespace Core
{
    public class CustomStats : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.2"; } }
        public override string name { get { return "CustomStats"; } }

        TopStat stat;
        TopStat stat2;

        public override void Load(bool startup)
        {
            stat = new TopStat("Name", "Table", "Column", () => "Description", TopStat.FormatInteger);
            stat2 = new TopStat("Name2", "Table", "Column", () => "Description", TopStat.FormatInteger);

            TopStat.Stats.Add(stat);
            TopStat.Stats.Add(stat);
        }

        public override void Unload(bool shutdown)
        {
            TopStat.Stats.Remove(stat);
            TopStat.Stats.Remove(stat2);
        }
    }
}
