using System;
using System.IO;
using MCGalaxy;

namespace MCGalaxy.Commands.Info
{
    public sealed class CmdQuote : Command2
    {
        public override string name { get { return "Quote"; } }
        public override string shortcut { get { return "qu"; } }
        public override string type { get { return "information"; } }

        public override void Use(Player p, string message, CommandData data)
        {
            string path = "./text/quotes.txt";

            string[] args = message.SplitSpaces(2);

            Player[] players = PlayerInfo.Online.Items;

            if (args.Length > 0)
            {
                if (args[0].ToLower() == "add")
                {
                    if (args.Length == 1)
                    {
                        p.Message("You need to specify a quote.");
                        return;
                    }

                    else if (args.Length > 1)
                    {
                        File.AppendAllText(path, args[1] + Environment.NewLine);

                        foreach (Player pl in players) pl.Message("%b" + p.truename + " %Squoted \"" + args[1] + "\"");
                        return;
                    }
                }
            }

            string[] allLines = File.ReadAllLines(path);
            Random rnd1 = new Random();
            string quote = allLines[rnd1.Next(allLines.Length)];
            foreach (Player pl in players) pl.Message("%d[Quote] " + quote);
        }

        public override void Help(Player p)
        {
            p.Message("%T/Quote %H- Selects a random quote from the quotes list.");
            p.Message("%T/Quote add [args] %H- Adds [args] into the quote list.");
            //p.Message("%T/Quotes %H- Shows all quotes.");
        }
    }
}
