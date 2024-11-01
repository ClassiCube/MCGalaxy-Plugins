using System;
using System.IO;

using MCGalaxy;
using MCGalaxy.Commands;

namespace MCGalaxy {

    public class CustomSoftware : Plugin {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
        public override string name { get { return "CustomSoftware"; } }

        public override void Load(bool startup) {
            Command.Register(new CmdSetSoftwareName());
            
            string file = "text/softwarename.txt";
            if (File.Exists(file)) {
                string contents = File.ReadAllText(file);
                Server.SoftwareName = contents;

            }
        }

        public override void Unload(bool shutdown) {
            Command.Unregister(Command.Find("SetSoftwareName"));
        }
    }

    public sealed class CmdSetSoftwareName : Command {
        public override string name { get { return "SetSoftwareName"; } }
        public override string type { get { return "other"; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message) {
            string[] args = message.SplitSpaces(1);
            
            string file = "text/softwarename.txt";
            
            if (args[0].Length == 0) {
                Help(p);
            }
            
            else {
                if (!File.Exists(file)) {
                    File.Create(file).Dispose();
                    File.WriteAllText(file, args[0]);
                    Server.SoftwareName = args[0];
                    p.Message("Set server software name to %b{0}", args[0]);
                }
                
                else {
                    File.Create(file).Dispose();
                    File.WriteAllText(file, args[0]);
                    Server.SoftwareName = args[0];
                    p.Message("Set server software name to %b{0}", args[0]);
                }
            }
        }

        public override void Help(Player p) {
            p.Message("&T/SetSoftwareName [software] - %HSets software name to [software].");
        }
    }
}
