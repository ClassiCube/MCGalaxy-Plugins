using System;
using System.IO;
using MCGalaxy;

namespace MCGalaxy.Commands.Info
{
    public sealed class CmdBestMaps : Command2
    {
        public override string name { get { return "BestMaps"; } }
        public override string shortcut { get { return "bm"; } }
        public override string type { get { return "information"; } }

        public override void Use(Player p, string message, CommandData data)
        {
            string path = "./text/bestmaps.txt";

            string[] args = message.SplitSpaces();

            if (message.Length == 0)
            {
                string[] maps = File.ReadAllLines(path); // Retrieve all maps in the list

                if (maps.Length == 0)
                {
                    p.Message("There are no maps in the BestMaps list. Add some via &b/BestMaps add [name]&S.");
                    return;
                }

                Random rnd = new Random();
                PlayerActions.ChangeMap(p, maps[rnd.Next(maps.Length)]); // Send player to a randomly-selected map from the list
            }

            else if (args[0].CaselessEq("add"))
            {
                if (args.Length < 2)
                {
                    p.Message("You need to specify a map name to add to the list.");
                    return;
                }

                string map = args[1].ToLower();

                if (!LevelInfo.MapExists(map))
                {
                    p.Message("Specified map does not exist.");
                    return;
                }

                File.AppendAllText(path, map + Environment.NewLine); // Add new map into the list
                p.Message("Added &b" + map + " &Sinto the BestMaps list.");
            }

            else
            {
                Help(p);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/BestMaps &H- Teleports you to one of the best maps on the server.");
            p.Message("&T/BestMaps add [map] &H- Adds [map] into the list of best maps.");
        }
    }
}
