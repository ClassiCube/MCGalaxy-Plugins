using System;
using System.IO;

namespace MCGalaxy.Commands
{
    public sealed class CmdFileManager : Command
    {
        public override string name { get { return "FileManager"; } }
        public override string shortcut { get { return "fm"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message)
        {
            string[] args = message.SplitSpaces(2);
            if (message.Length == 0) { Help(p); return; }

            if (args[0].CaselessEq("show") || args[0].CaselessEq("list")) ListFiles(p, args);
            if (args[0].CaselessEq("del") || args[0].CaselessEq("delete")) DeleteFiles(p, args);
        }

        void ListFiles(Player p, string[] args)
        {
            if (args.Length < 2) { Help(p); return; }

            if (!Directory.Exists(args[1]))
            {
                p.Message("&cDirectory: &b" + args[1] + " &cnot found.");
                return;
            }

            string[] files = Directory.GetFiles(args[1], "*", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                // Make structure look neater by showing relative file paths
                string currentDirectory = Environment.CurrentDirectory;
                DirectoryInfo directory = new DirectoryInfo(currentDirectory);

                FileInfo info = new FileInfo(file);

                string fullDirectory = directory.FullName;
                string fullFile = info.FullName;

                p.Message(fullFile.Substring(fullDirectory.Length + 1));
            }
        }

        void DeleteFiles(Player p, string[] args)
        {
            if (args.Length < 2) { Help(p); return; }

            if (!File.Exists(args[1]))
            {
                p.Message("&cFile: &b" + args[1] + " &cnot found.");
                return;
            }

            File.Delete(args[1]);
            p.Message("&aFile: &b" + args[1] + " &asuccessfully deleted.");
        }

        public override void Help(Player p)
        {
            p.Message("&T/FileManager list [folder]");
            p.Message("&HLists all files in a specified folder. E.g, &b/filemanager list ./plugins/");
            p.Message("&T/FileManager delete [file name]");
            p.Message("&HDeletes a file in a specified folder. E.g, &b/filemanager delete ./plugins/Useless.dll");
        }
    }
}
