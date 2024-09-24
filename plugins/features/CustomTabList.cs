using System;
using System.IO;

using MCGalaxy;
using MCGalaxy.Config;
using MCGalaxy.Events.EntityEvents;

namespace MCGalaxy
{
    public class CustomTabList : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.4"; } }
        public override string name { get { return "CustomTabList"; } }

        public static string path = "./plugins/CustomTabList";

        public class Config
        {
            [ConfigString("syntax", "Extra", "[username]")]
            public static string Syntax = "[username]";

            static ConfigElement[] cfg;
            public void Load()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.ParseFile(cfg, path + "/config.properties", this);
            }

            public void Save()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.SerialiseSimple(cfg, path + "/config.properties", this);
            }
        }

        public static void MakeConfig()
        {
            using (StreamWriter w = new StreamWriter(path + "/config.properties"))
            {
                w.WriteLine("# Edit the settings below to modify how the plugin operates.");
                w.WriteLine("# The syntex you wish to use for the tab list.");
                w.WriteLine("# Use &[colour code] to use colour codes.");
                w.WriteLine("# Valid variables are: [nick], [username], [color], [title], [titlecolor] [money], [team], [teamcolor], [muted], [afk]");
                w.WriteLine("syntax = [username]");
                w.WriteLine();
            }
        }

        public static Config cfg = new Config();

        public override void Load(bool startup)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            if (!File.Exists(path + "/config.properties")) MakeConfig();

            // Initialize config
            cfg.Load();

            OnTabListEntryAddedEvent.Register(HandleTabListEntryAdded, Priority.High);
        }

        void HandleTabListEntryAdded(Entity entity, ref string name, ref string group, Player p)
        {
            Player pl = entity as Player;
            if (pl == null) return;

            string pingColor = "&7";

            if (pl.Session.Ping.AveragePing() > 0 && pl.Session.Ping.AveragePing() < 50) pingColor = "&a";
            if (pl.Session.Ping.AveragePing() >= 50 && pl.Session.Ping.AveragePing() < 100) pingColor = "&e";
            if (pl.Session.Ping.AveragePing() > 100 && pl.Session.Ping.AveragePing() < 200) pingColor = "&6";
            if (pl.Session.Ping.AveragePing() >= 200) pingColor = "&c";

            name = Config.Syntax
                .Replace("[nick]", pl.ColoredName)
                .Replace("[username]", pl.truename)
                .Replace("[color]", pl.color)
                .Replace("[title]", pl.title)
                .Replace("[titlecolor]", pl.titlecolor)
                .Replace("[money]", pl.money.ToString())
                .Replace("[team]", pl.Game.Team != null ? pl.Game.Team.Name : "")
                .Replace("[teamcolor]", pl.Game.Team != null ? pl.Game.Team.Color : "")
                .Replace("[muted]", pl.muted ? "(muted)" : "")
                .Replace("[afk]", pl.IsAfk ? "(afk)" : "")
                .Replace("[ping]", pl.Session.Ping.AveragePing().ToString())
                .Replace("[pingcolor]", pingColor);
        }

        public override void Unload(bool shutdown)
        {
            OnTabListEntryAddedEvent.Unregister(HandleTabListEntryAdded);
        }
    }
}
