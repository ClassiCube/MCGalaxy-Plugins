using System;

namespace MCGalaxy.Commands
{

    public sealed class CmdChevify : Command
    {
        public override string name { get { return "Chevify"; } }
        public override string type { get { return CommandTypes.Building; } }

        public override void Use(Player p, string message)
        {
            int[] blocks = { 185, 138, 118, 113, 217, 90, 49, 147, 93, 268, 144, 164, 68, 192, 180, 176, 89, 60, 137, 114, 173, 136, 62, 41, 146 };

            var random = new Random();

            // 724 global blocks currently
            for (int i = 1; i < 724; i++)
            {

                int index = random.Next(blocks.Length);
                Command.Find("ReplaceAll").Use(p, i + " " + blocks[index]);
            }
            p.Message("Successfully Chevified the map.");
        }

        public override void Help(Player p) { }
    }
}