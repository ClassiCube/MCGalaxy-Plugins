using System.IO;
using MCGalaxy;
using MCGalaxy.Config;

namespace MCGalaxy
{
    public class PasswordManager : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.2"; } }
        public override string name { get { return "PasswordManager"; } }

        public class Config
        {
            [ConfigString("xp-password", "XP", "password")]
            public static string XPPassword = "password";

            [ConfigString("cosmetics-password", "Cosmetics", "password")]
            public static string CosmeticsPassword = "password";

            [ConfigString("crate-reward-password", "Cosmetics", "password")]
            public static string CrateRewardPassword = "password";

            [ConfigString("stopwatch-password", "Stopwatch", "password")]
            public static string StopwatchPassword = "password";

            static ConfigElement[] cfg;
            public void Load()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.ParseFile(cfg, "./plugins/PasswordManager/config.properties", this);
            }

            public void Save()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.SerialiseSimple(cfg, "./plugins/PasswordManager/config.properties", this);
            }
        }

        public static void MakeConfig()
        {
            using (StreamWriter w = new StreamWriter("./plugins/PasswordManager/config.properties"))
            {
                w.WriteLine("# Edit the settings below to modify how the plugin operates.");
                w.WriteLine("# The password used to give players XP.");
                w.WriteLine("xp-password = password");
                w.WriteLine("# The password used by the cosmetic plugins e.g, pets, titles, trails etc.");
                w.WriteLine("cosmetics-password = password");
                w.WriteLine("# The password used by the XP plugin to give players crates.");
                w.WriteLine("crates-reward-password = password");
                w.WriteLine("# The password used to start/stop forced stopwatches.");
                w.WriteLine("stopwatch-password = password");
                w.WriteLine();
            }
        }

        public static Config cfg = new Config();

        public override void Load(bool startup)
        {
            Directory.CreateDirectory("./plugins/PasswordManager");
            if (!File.Exists("./plugins/PasswordManager/config.properties")) MakeConfig();

            // Initialize config
            cfg.Load();
        }

        public override void Unload(bool shutdown) { }
    }
}