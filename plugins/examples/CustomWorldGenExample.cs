using System;
using MCGalaxy.Generator;

namespace MCGalaxy
{
    public class CustomWorldGenExample : Plugin
    {
        public override string name { get { return "CustomWorldGenExample"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.1"; } }
        public override string creator { get { return "Venk"; } }

        public override void Load(bool startup)
        {
            string help = "&HUseful info for things like seeds should go here";
            MapGen.Register("Example", GenType.Advanced, GenerateWorld, help);
        }

        // All of your world generation should go in this method
        unsafe static bool GenerateWorld(Player p, Level lvl, string seed)
        {
            int surfaceHeight = lvl.Height / 2, v;
            if (int.TryParse(seed, out v) && v >= 0 && v < lvl.Height) surfaceHeight = v;
            lvl.Config.EdgeLevel = surfaceHeight + 1;

            fixed (byte* ptr = lvl.blocks)
            {
                if (surfaceHeight < lvl.Height)
                    MapSet(lvl.Width, lvl.Length, ptr, surfaceHeight, surfaceHeight, Block.White);
            }
            return true;
        }

        // Not mandatory, feel free to remove if you add your own generation above
        unsafe static void MapSet(int width, int length, byte* ptr,
                                  int yBeg, int yEnd, byte block)
        {
            int beg = (yBeg * length) * width;
            int end = (yEnd * length + (length - 1)) * width + (width - 1);
            Utils.memset((IntPtr)ptr, block, beg, end - beg + 1);
        }


        public override void Unload(bool shutdown)
        {
        }

        public override void Help(Player p)
        {
        }
    }
}
