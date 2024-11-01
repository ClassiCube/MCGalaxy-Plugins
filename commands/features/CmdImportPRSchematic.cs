//reference System.dll
//reference System.IO.Compression.dll
//reference System.Net.Requests.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using MCGalaxy;
using BlockID = System.UInt16;

namespace Core
{
    public class CmdImportSchematic : Command2
    {
        public override string name { get { return "ImportPRSchematic"; } }
        public override string type { get { return "other"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public override void Use(Player p, string message)
        {
            string[] args = message.SplitSpaces();

            if (args.Length == 0) { p.Message("You need to specify a URL to import."); return; }
            if (args.Length == 1) { p.Message("You need to specify the name you want the level to be called."); return; }

            if (File.Exists("./levels/" + args[1] + ".lvl"))
            {
                p.Message("That is already a level name, try something else.");
                return;
            }

            NbtReader reader = new NbtReader();
            Dictionary<string, NbtTag> tags;

            if (args[0].StartsWith("."))
            {
                Stream stream = File.OpenRead(args[0]);
                tags = reader.Load(stream);
            }

            else
            {

                var req = System.Net.WebRequest.Create(args[0]);
                using (Stream stream = req.GetResponse().GetResponseStream())
                {
                    tags = reader.Load(stream);
                }
            }

            MapConverter conv = new MapConverter();
            conv.Init(tags);

            conv.SaveAsLvl("./levels/" + args[1] + ".lvl");
            //File.SetAttributes(dest, FileAttributes.Normal);

            p.Message("Finished importing level &b" + args[1] + "&S.");
        }


        public class MapConverter
        {
            int width, length, height;
            byte[] blocks;
            int chunksX, chunksY, chunksZ;
            byte[][] customBlocks;

            public void Init(Dictionary<string, NbtTag> values)
            {
                byte[] raw = (byte[])values["Blocks"].Value;
                byte[] meta = (byte[])values["Data"].Value;
                width = (short)values["Width"].Value;
                height = (short)values["Height"].Value;
                length = (short)values["Length"].Value;

                blocks = new byte[width * height * length];
                chunksX = (width + 15) / 16;
                chunksY = (height + 15) / 16;
                chunksZ = (length + 15) / 16;
                customBlocks = new byte[chunksX * chunksY * chunksZ][];

                for (int i = 0; i < raw.Length; i++)
                {
                    SetBlock(i, Lookup.lookup[raw[i], meta[i]]);
                }
            }

            public void SaveAsLvl(string output)
            {
                using (FileStream fs = File.Create(output))
                    SaveLvl(fs);
            }

            // borrowed from MCGalaxy, with a return date of never
            void SetBlock(int index, BlockID block)
            {
                if (block >= Block.CpeCount)
                {
                    blocks[index] = Block.ExtendedClass[block >> 8];
                    int x, y, z;
                    IntToPos(index, out x, out y, out z);
                    FastSetExtTile(x, y, z, (byte)block);
                }
                else
                {
                    blocks[index] = (byte)block;
                }
            }

            void FastSetExtTile(int x, int y, int z, byte extBlock)
            {
                int cx = x >> 4, cy = y >> 4, cz = z >> 4;
                int cIndex = (cy * chunksZ + cz) * chunksX + cx;
                byte[] chunk = customBlocks[cIndex];

                if (chunk == null)
                {
                    chunk = new byte[16 * 16 * 16];
                    customBlocks[cIndex] = chunk;
                }
                chunk[(y & 0x0F) << 8 | (z & 0x0F) << 4 | (x & 0x0F)] = extBlock;
            }

            void IntToPos(int pos, out int x, out int y, out int z)
            {
                y = (pos / width / length);
                pos -= y * width * length;
                z = (pos / width);
                pos -= z * width;
                x = pos;
            }

            void SaveLvl(Stream stream)
            {
                using (Stream gs = new GZipStream(stream, CompressionMode.Compress, true))
                {
                    byte[] header = new byte[16];
                    BitConverter.GetBytes(1874).CopyTo(header, 0);
                    gs.Write(header, 0, 2);

                    BitConverter.GetBytes(width).CopyTo(header, 0);
                    BitConverter.GetBytes(length).CopyTo(header, 2);
                    BitConverter.GetBytes(height).CopyTo(header, 4);

                    gs.Write(header, 0, header.Length);
                    gs.Write(blocks, 0, blocks.Length);

                    // Write out new blockdefinitions data
                    gs.WriteByte(0xBD);
                    int index = 0;
                    for (int y = 0; y < chunksY; y++)
                        for (int z = 0; z < chunksZ; z++)
                            for (int x = 0; x < chunksX; x++)
                            {
                                byte[] chunk = customBlocks[index];
                                if (chunk == null)
                                {
                                    gs.WriteByte(0);
                                }
                                else
                                {
                                    gs.WriteByte(1);
                                    gs.Write(chunk, 0, chunk.Length);
                                }
                                index++;
                            }
                }
            }
        }

        public sealed class NbtReader
        {

            BinaryReader reader;
            NbtTag invalid = default(NbtTag);

            public Dictionary<string, NbtTag> Load(Stream stream)
            {
                using (GZipStream wrapper = new GZipStream(stream, CompressionMode.Decompress))
                {
                    reader = new BinaryReader(wrapper);
                    if (reader.ReadByte() != (byte)NbtTagType.Compound)
                        throw new InvalidDataException("Nbt file must start with Tag_Compound");

                    invalid.TagId = NbtTagType.Invalid;
                    NbtTag root = ReadTag((byte)NbtTagType.Compound, true);
                    return (Dictionary<string, NbtTag>)root.Value;
                }
            }

            // Compile with
            unsafe NbtTag ReadTag(byte typeId, bool readTagName)
            {
                if (typeId == 0) return invalid;

                NbtTag tag = default(NbtTag);
                tag.Name = readTagName ? ReadString() : null;
                tag.TagId = (NbtTagType)typeId;

                switch ((NbtTagType)typeId)
                {
                    case NbtTagType.Int8:
                        tag.Value = reader.ReadByte(); break;
                    case NbtTagType.Int16:
                        tag.Value = ReadInt16(); break;
                    case NbtTagType.Int32:
                        tag.Value = ReadInt32(); break;
                    case NbtTagType.Int64:
                        tag.Value = ReadInt64(); break;
                    case NbtTagType.Real32:
                        int temp32 = ReadInt32();
                        tag.Value = *(float*)&temp32; break;
                    case NbtTagType.Real64:
                        long temp64 = ReadInt64();
                        tag.Value = *(double*)&temp64; break;
                    case NbtTagType.Int8Array:
                        tag.Value = reader.ReadBytes(ReadInt32()); break;
                    case NbtTagType.String:
                        tag.Value = ReadString(); break;

                    case NbtTagType.List:
                        NbtList list = new NbtList();
                        list.ChildTagId = (NbtTagType)reader.ReadByte();
                        list.ChildrenValues = new object[ReadInt32()];
                        for (int i = 0; i < list.ChildrenValues.Length; i++)
                        {
                            list.ChildrenValues[i] = ReadTag((byte)list.ChildTagId, false);
                        }
                        tag.Value = list; break;

                    case NbtTagType.Compound:
                        Dictionary<string, NbtTag> children = new Dictionary<string, NbtTag>();
                        NbtTag child;
                        while ((child = ReadTag(reader.ReadByte(), true)).TagId != NbtTagType.Invalid)
                        {
                            children[child.Name] = child;
                        }
                        tag.Value = children; break;

                    case NbtTagType.Int32Array:
                        int[] array = new int[ReadInt32()];
                        for (int i = 0; i < array.Length; i++)
                            array[i] = ReadInt32();
                        tag.Value = array; break;

                    default:
                        throw new InvalidDataException("Unrecognised tag id: " + typeId);
                }
                return tag;
            }

            long ReadInt64() { return IPAddress.HostToNetworkOrder(reader.ReadInt64()); }
            int ReadInt32() { return IPAddress.HostToNetworkOrder(reader.ReadInt32()); }
            short ReadInt16() { return IPAddress.HostToNetworkOrder(reader.ReadInt16()); }

            string ReadString()
            {
                int len = (ushort)ReadInt16();
                byte[] data = reader.ReadBytes(len);
                return Encoding.UTF8.GetString(data);
            }
        }

        public struct NbtTag
        {
            public string Name;
            public object Value;
            public NbtTagType TagId;
        }

        public class NbtList
        {
            public NbtTagType ChildTagId;
            public object[] ChildrenValues;
        }

        public enum NbtTagType : byte
        {
            End, Int8, Int16, Int32, Int64,
            Real32, Real64, Int8Array, String,
            List, Compound, Int32Array,
            Invalid = 255,
        }

        public static partial class Block
        {
            public const byte CpeCount = StoneBrick + 1;
            public static byte[] ExtendedClass = new byte[3];

            static Block()
            {
                ExtendedClass[0] = custom_block;
                ExtendedClass[1] = custom_block_2;
                ExtendedClass[2] = custom_block_3;
            }

            // Original blocks
            public const byte Air = 0;
            public const byte Stone = 1;
            public const byte Grass = 2;
            public const byte Dirt = 3;
            public const byte Cobblestone = 4;
            public const byte Wood = 5;
            public const byte Sapling = 6;
            public const byte Bedrock = 7;// adminium
            public const byte Water = 8;
            public const byte StillWater = 9;
            public const byte Lava = 10;
            public const byte StillLava = 11;
            public const byte Sand = 12;
            public const byte Gravel = 13;
            public const byte GoldOre = 14;
            public const byte IronOre = 15;
            public const byte CoalOre = 16;
            public const byte Log = 17;
            public const byte Leaves = 18;
            public const byte Sponge = 19;
            public const byte Glass = 20;
            public const byte Red = 21;
            public const byte Orange = 22;
            public const byte Yellow = 23;
            public const byte Lime = 24;
            public const byte Green = 25;
            public const byte Aqua = 26;
            public const byte Cyan = 27;
            public const byte Blue = 28;
            public const byte Purple = 29;
            public const byte Indigo = 30;
            public const byte Violet = 31;
            public const byte Magenta = 32;
            public const byte Pink = 33;
            public const byte Black = 34;
            public const byte Gray = 35;
            public const byte White = 36;
            public const byte Dandelion = 37;
            public const byte Rose = 38;
            public const byte BrownMushroom = 39;
            public const byte RedMushroom = 40;
            public const byte Gold = 41;
            public const byte Iron = 42;
            public const byte DoubleSlab = 43;
            public const byte Slab = 44;
            public const byte Brick = 45;
            public const byte TNT = 46;
            public const byte Bookshelf = 47;
            public const byte MossyRocks = 48;
            public const byte Obsidian = 49;

            // CPE blocks
            public const byte CobblestoneSlab = 50;
            public const byte Rope = 51;
            public const byte Sandstone = 52;
            public const byte Snow = 53;
            public const byte Fire = 54;
            public const byte LightPink = 55;
            public const byte ForestGreen = 56;
            public const byte Brown = 57;
            public const byte DeepBlue = 58;
            public const byte Turquoise = 59;
            public const byte Ice = 60;
            public const byte CeramicTile = 61;
            public const byte Magma = 62;
            public const byte Pillar = 63;
            public const byte Crate = 64;
            public const byte StoneBrick = 65;

            public const byte custom_block = 163;
            public const byte custom_block_2 = 198;
            public const byte custom_block_3 = 199;
        }

        public static class Lookup
        {

            public static BlockID[,] lookup = new BlockID[768, 768];

            static Lookup()
            {
                for (int i = 0; i < 768; i++)
                    for (int j = 0; j < 768; j++)
                        lookup[i, j] = Block.Magma;
                // Start with ID 0 and go up by 1
                All(Block.Air);

                All(Block.Stone); // *
                lookup[1, 0] = Block.Stone; // Stone
                lookup[1, 1] = 172; // Granite
                lookup[1, 2] = 173; // Polished granite
                lookup[1, 3] = 170; // Diorite
                lookup[1, 4] = 171; // Polished diorite
                lookup[1, 5] = 168; // Andesite
                lookup[1, 6] = 169; // Polished andesite

                All(Block.Grass);
                All(Block.Dirt); // *
                lookup[3, 0] = Block.Dirt; // Dirt
                lookup[3, 1] = 81; // Coarse dirt
                lookup[3, 2] = 136; // Podzol

                All(Block.Cobblestone);

                All(Block.Wood); // *
                lookup[5, 0] = 5; // Planks
                lookup[5, 1] = 164; // Spruce planks
                lookup[5, 2] = 160; // Birch planks
                lookup[5, 3] = 162; // Jungle planks
                lookup[5, 4] = 163; // Acacia planks
                lookup[5, 5] = 161; // Dark oak planks

                All(Block.Sapling);
                All(Block.Bedrock);
                All(Block.Water);
                All(Block.Water);
                All(Block.Lava);
                All(Block.Lava);

                All(Block.Sand); // *
                lookup[12, 0] = 12; // Sand
                lookup[12, 1] = 218; // Red sand

                All(Block.Gravel);
                All(Block.GoldOre);
                All(Block.IronOre);

                All(Block.CoalOre);

                All(Block.Log); // *
                lookup[17, 0] = 17; // Log
                lookup[17, 1] = 88; // Spruce log
                lookup[17, 2] = 84; // Birch log
                lookup[17, 3] = 86; // Jungle log
                lookup[17, 4] = 87; // Acacia log
                lookup[17, 5] = 85; // Dark oak log

                All(Block.Leaves); // *
                lookup[18, 0] = 18; // Leaves
                lookup[18, 1] = 152; // Spruce leaves
                lookup[18, 2] = 148; // Birch leaves
                lookup[18, 3] = 150; // Jungle leaves
                lookup[18, 4] = 151; // Acacia leaves
                lookup[18, 5] = 149; // Dark oak leaves

                All(Block.Sponge);
                All(Block.Glass);
                All(143); // Lapis ore
                All(147); // Lapis block
                All(145); // NOT dispenser
                All(Block.Sandstone); // Variants
                All(Block.Crate); // Noteblock
                All(Block.Magma); // NOT bed
                All(145); // // NOT powered_rail
                All(145); // NOT detector_rail
                All(145); // NOT sticky_piston
                All(175); // Cobweb

                All(131); // Tallgrass *
                lookup[31, 0] = 216; // Dead bush
                lookup[31, 1] = 131; // Tall grass
                lookup[31, 2] = 130; // Fern

                All(216); // Dead bush
                All(145); // NOT piston
                All(145); // NOT piston_head
                All(145); // NOT piston_extension

                // Wool variants
                All(Block.White); // *
                lookup[35, 0] = Block.White;
                lookup[35, 1] = Block.Orange;
                lookup[35, 2] = Block.Violet;
                lookup[35, 3] = Block.Blue;
                lookup[35, 4] = Block.Yellow;
                lookup[35, 5] = Block.Green;
                lookup[35, 6] = Block.LightPink;
                lookup[35, 7] = Block.Black;
                lookup[35, 8] = Block.Gray;
                lookup[35, 9] = Block.Turquoise;
                lookup[35, 10] = Block.Indigo;
                lookup[35, 11] = Block.DeepBlue;
                lookup[35, 12] = Block.Brown;
                lookup[35, 13] = Block.ForestGreen;
                lookup[35, 14] = Block.Red;
                lookup[35, 15] = 373;

                All(Block.Dandelion);

                All(Block.Rose); // *
                lookup[38, 0] = 38;
                lookup[38, 1] = 132;
                lookup[38, 2] = 128;
                lookup[38, 3] = 124;
                lookup[38, 4] = 125;
                lookup[38, 5] = 128;
                lookup[38, 6] = 127;
                lookup[38, 7] = 129;
                lookup[38, 8] = 126;

                All(Block.BrownMushroom);
                All(Block.RedMushroom);
                All(Block.Gold);
                All(Block.Iron);

                All(Block.DoubleSlab); // *
                lookup[43, 0] = 43;
                lookup[43, 1] = 52;
                lookup[43, 2] = 5;
                lookup[43, 3] = 4;
                lookup[43, 4] = 45;
                lookup[43, 5] = 65;
                lookup[43, 6] = 92;
                lookup[43, 7] = 228;

                All(Block.Slab); // Slabs
                lookup[44, 0] = 44;
                lookup[44, 1] = 72;
                lookup[44, 2] = 69;
                lookup[44, 3] = 50;
                lookup[44, 4] = 517;
                lookup[44, 5] = 70;
                lookup[44, 6] = 696;
                lookup[44, 7] = 274;
                lookup[44, 8] = 515;
                lookup[44, 9] = 552;
                lookup[44, 10] = 271;
                lookup[44, 11] = 270;
                lookup[44, 12] = 518;
                lookup[44, 13] = 516;
                lookup[44, 14] = 697;
                lookup[44, 15] = 273;


                All(Block.Brick);
                All(Block.TNT);
                All(Block.Bookshelf);
                All(Block.MossyRocks);
                All(Block.Obsidian);
                All(707); // Torch
                All(Block.Fire);
                All(Block.Magma); // NOT mob_spawner
                All(Block.Wood); // NOT oak_stairs
                All(231); // Chest-N since no dir blocks
                All(145); // NOT redstone_wire
                All(140); // Diamond ore
                All(144); // Diamond block
                All(225); // Crafting table
                All(333); // Wheat
                All(223); // Farmland
                All(719); // NOT furnace
                All(719); // NOT lit_furnace
                All(Block.Magma); // NOT standing_sign
                All(460); // Door (bottom)
                lookup[64, 0] = 263;
                lookup[64, 1] = 460; // ?
                lookup[64, 2] = 460; // Door bottom
                lookup[64, 3] = Block.Magma; // ?
                lookup[64, 4] = Block.Magma; // ?
                lookup[64, 5] = Block.Magma; // ?
                lookup[64, 6] = Block.Magma; // ?
                lookup[64, 7] = Block.Magma; // ?
                lookup[64, 8] = 461; // Door top
                lookup[64, 9] = Block.Magma; // ?
                lookup[64, 10] = Block.Magma; // ?
                lookup[64, 11] = Block.Magma; // ?
                lookup[64, 12] = Block.Magma; // ?
                lookup[64, 13] = Block.Magma; // ?
                lookup[64, 14] = Block.Magma; // ?
                lookup[64, 15] = Block.Magma; // ?

                All(433); // Ladder-N since no dir blocks
                lookup[65, 0] = 263;
                lookup[65, 1] = Block.Magma; // ?
                lookup[65, 2] = 440; // South
                lookup[65, 3] = 433; // North
                lookup[65, 4] = 442; // East
                lookup[65, 5] = 441; // West

                All(666); // Rail-NS since no dir blocks
                All(Block.Cobblestone); // NOT cobblestone_stairs
                All(605); // NOT wall_sign
                lookup[68, 0] = Block.Magma; // ?
                lookup[68, 1] = Block.Magma; // ?
                lookup[68, 2] = 606; // South
                lookup[68, 3] = 605; // North
                lookup[68, 4] = 608; // East
                lookup[68, 5] = 607; // West

                All(Block.Air); // NOT lever
                All(Block.Air); // NOT stone_pressure_plate
                All(Block.Iron); // NOT iron_door
                All(Block.Air); // NOT wood_pressure_plate
                All(141); // Redstone ore
                All(141); // Lit redstone ore
                All(145); // NOT unlit_redtone_torch
                All(Block.Air); // NOT redstone_torch
                All(Block.Air); // NOT stone_button
                All(Block.Snow);
                All(Block.Ice);
                All(68); // Snow block
                All(138); // Cactus
                All(187); // Clay
                All(83); // Sugar cane
                All(Block.Crate); // Juke box
                All(Block.Rope); // Fence = rope since no dir blocks
                All(137); // Pumpkin

                All(89); // Netherrack
                All(193); // Soul sand
                All(167); // Glowstone
                All(Block.Purple); // NOT portal
                All(137); // NOT lit_pumpkin
                All(Block.Magma); // NOT cake
                All(145); // NOT unpowered_repeater
                All(145); // NOT powered_repeater

                All(94); // White stained glass *
                lookup[95, 0] = 94;
                lookup[95, 1] = 107;
                lookup[95, 2] = 105;
                lookup[95, 3] = 103;
                lookup[95, 4] = 101;
                lookup[95, 5] = 100;
                lookup[95, 6] = 105;
                lookup[95, 7] = 96;
                lookup[95, 8] = 108;
                lookup[95, 9] = 106;
                lookup[95, 10] = 104;
                lookup[95, 11] = 102;
                lookup[95, 12] = 443;
                lookup[95, 13] = 99;
                lookup[95, 14] = 97;
                lookup[95, 15] = 95;

                All(263); // Trapdoor-N since no dir blocks
                lookup[96, 0] = 263;
                lookup[96, 1] = Block.Air; // ?
                lookup[96, 2] = Block.Air;// ?
                lookup[96, 3] = Block.Air; // ?
                lookup[96, 4] = 262; // South
                lookup[96, 5] = 263; // North, top
                lookup[96, 6] = 260; // East
                lookup[96, 7] = 261; // West
                lookup[96, 8] = Block.Air; // ?
                lookup[96, 9] = Block.Air; // ?
                lookup[96, 10] = Block.Air; // ?
                lookup[96, 11] = Block.Air; // ?
                lookup[96, 12] = 262; // South, bottom
                lookup[96, 13] = 263; // North, bottom
                lookup[96, 14] = 260; // East bottom
                lookup[96, 15] = 261; // West, bottom

                All(Block.Stone); // Stone egg *
                All(Block.StoneBrick); // *

                All(218); // NOT brown_mushroom_block, but red sand
                All(280); // NOT red_mushroom_block, but red concrete
                All(235); // Iron Bar-NS since no dir blocks
                All(Block.Glass); // Glass pane
                All(91); // Melon
                All(91); // NOT Stem
                All(91); // NOT Stem
                All(Block.Leaves); // NOT Vine
                All(Block.Rope); // Fence = gate since no dir blocks
                All(Block.Brick); // NOT Brick stairs
                All(Block.StoneBrick); // NOT Stone brick stairs
                All(217); // Mycelium
                All(157); // Lilypad
                All(92); // Nether brick
                All(92); // NB fence
                All(92); // NB stairs
                All(155); // Nether wart
                All(Block.Obsidian); // Enchantment table
                All(Block.Magma); // Cauldron
                All(680); // Cauldron
                All(Block.Purple); // Portal
                All(220); // End frame
                All(220); // End Stone
                All(Block.Obsidian); // Dragon egg
                All(166); // Lamp on
                All(167); // Lamp off

                All(Block.Wood); // Double wood *
                lookup[125, 0] = 5;
                lookup[125, 1] = 164;
                lookup[125, 2] = 160;
                lookup[125, 3] = 162;
                lookup[125, 4] = 163;
                lookup[125, 5] = 161;

                All(69); // Wood slab
                lookup[126, 0] = 69;
                lookup[126, 1] = 196;
                lookup[126, 2] = 194;
                lookup[126, 3] = 195;
                lookup[126, 4] = 198;

                // Upper slabs
                lookup[126, 5] = 197;
                lookup[126, 6] = Block.Orange;
                lookup[126, 7] = Block.Yellow;
                lookup[126, 8] = 271;
                lookup[126, 9] = 277;
                lookup[126, 10] = 275;
                lookup[126, 11] = 276;
                lookup[126, 12] = 279;
                lookup[126, 13] = 278;

                All(Block.Magma); // NOT Cocoa
                All(Block.Sandstone); // NOT Sand stair
                All(142); // Emerald Ore
                All(231); // Chest-N instead of ender chest
                All(Block.Magma); // NOT trip
                All(Block.Magma); // NOT trip hook
                All(146); // Emerald block
                All(164); // Wood Stair
                All(160); // Wood Stair
                All(162); // Wood Stair
                All(80); // Command
                All(Block.Glass); // NOT Beacon

                All(Block.Cobblestone); // NOT Cob wall *
                lookup[139, 0] = Block.Cobblestone;
                lookup[139, 1] = Block.MossyRocks;

                All(Block.Brick); // NOT Pot
                All(115); // Carrot
                All(116); // Potato
                All(Block.Air); // NOT Button
                All(Block.Air); // NOT Head
                All(Block.Iron); // Not Anvil
                All(231); // Chest-n
                All(53); // NOT gold plate
                All(53); // NOT iron plate
                All(53); // NOT in compar
                All(53); // NOT compar
                All(53); // NOT day sensor
                All(145); // Redstone block

                All(90); // Quartz ore
                All(Block.Iron); // NOT Hopper
                All(228); // Quartz block
                All(228); ;// Quartz Stairs
                All(66); // Red rail
                All(Block.Cobblestone); // NOT Dispenser

                All(289); // White clay *
                lookup[159, 0] = 289;
                lookup[159, 1] = 281;
                lookup[159, 2] = 287;
                lookup[159, 3] = 285;
                lookup[159, 4] = 282;
                lookup[159, 5] = 283;
                lookup[159, 6] = 288;
                lookup[159, 7] = 290;
                lookup[159, 8] = 370;
                lookup[159, 9] = 343;
                lookup[159, 10] = 286;
                lookup[159, 11] = 295;
                lookup[159, 12] = 294;
                lookup[159, 13] = 293;
                lookup[159, 14] = 280;
                lookup[159, 15] = 291;

                All(94); // White stained glass pane *
                lookup[160, 0] = 94;
                lookup[160, 1] = 107;
                lookup[160, 2] = 105;
                lookup[160, 3] = 103;
                lookup[160, 4] = 101;
                lookup[160, 5] = 100;
                lookup[160, 6] = 105;
                lookup[160, 7] = 96;
                lookup[160, 8] = 108;
                lookup[160, 9] = 106;
                lookup[160, 10] = 104;
                lookup[160, 11] = 102;
                lookup[160, 12] = 443;
                lookup[160, 13] = 99;
                lookup[160, 14] = 97;
                lookup[160, 15] = 95;

                All(151); // Acacia leaves
                lookup[161, 0] = 151;
                lookup[161, 1] = 149;

                All(87); // Acacia log *
                lookup[162, 0] = 187;
                lookup[162, 1] = 85;

                All(163); // Acacia plank
                All(161); // Dark plank
                All(156); // Slime
                All(Block.Air); // Barrier
                All(Block.Iron); // NOT Iron trap

                All(178); // Prismarine *
                lookup[168, 0] = 178;
                lookup[168, 1] = 180;
                lookup[168, 2] = 179;

                All(185); // Sea lantern
                All(93); // Hay bale

                All(Block.Air); // Carpet *
                lookup[171, 0] = Block.Air;
                lookup[171, 1] = Block.Air;
                lookup[171, 2] = Block.Air;
                lookup[171, 3] = Block.Air;
                lookup[171, 4] = Block.Air;
                lookup[171, 5] = Block.Air;
                lookup[171, 6] = Block.Air;
                lookup[171, 7] = Block.Air;
                lookup[171, 8] = Block.Air;
                lookup[171, 9] = Block.Air;
                lookup[171, 10] = Block.Air;
                lookup[171, 11] = Block.Air;
                lookup[171, 12] = Block.Air;
                lookup[171, 13] = Block.Air;
                lookup[171, 14] = Block.Air;
                lookup[171, 15] = Block.Air;

                All(110); // Hardened clay
                All(109); // Coal block
                All(82); // Packed ice

                All(133); // Sunflower *
                lookup[175, 0] = 133;
                lookup[175, 1] = 122;
                lookup[175, 2] = 312;
                lookup[175, 3] = 130;
                lookup[175, 4] = 125;
                lookup[175, 5] = 122;

                All(Block.White);
                All(Block.White);
                All(Block.Magma);

                All(219); // Red sandstone *
                lookup[179, 0] = 219;
                lookup[179, 1] = 219;
                lookup[179, 2] = 219;

                All(219); // NOT smooth red sandstone
                All(219); // NOT double red sandstone
                All(535); // NOT red sandstone slab

                // Not fences

                All(51);
                All(51);
                All(51);
                All(51);
                All(51);
                All(51);
                All(51);
                All(51);
                All(51);
                All(51);

                // Doors

                All(460);
                lookup[193, 0] = 263;
                lookup[193, 1] = 460; // ?
                lookup[193, 2] = 460; // Door bottom
                lookup[193, 3] = Block.Magma; // ?
                lookup[193, 4] = Block.Magma; // ?
                lookup[193, 5] = Block.Magma; // ?
                lookup[193, 6] = Block.Magma; // ?
                lookup[193, 7] = Block.Magma; // ?
                lookup[193, 8] = 461; // Door top
                lookup[193, 9] = Block.Magma; // ?
                lookup[193, 10] = Block.Magma; // ?
                lookup[193, 11] = Block.Magma; // ?
                lookup[193, 12] = Block.Magma; // ?
                lookup[193, 13] = Block.Magma; // ?
                lookup[193, 14] = Block.Magma; // ?
                lookup[193, 15] = Block.Magma; // ?

                All(460);
                lookup[194, 0] = 263;
                lookup[194, 1] = 460; // ?
                lookup[194, 2] = 460; // Door bottom
                lookup[194, 3] = Block.Magma; // ?
                lookup[194, 4] = Block.Magma; // ?
                lookup[194, 5] = Block.Magma; // ?
                lookup[194, 6] = Block.Magma; // ?
                lookup[194, 7] = Block.Magma; // ?
                lookup[194, 8] = 461; // Door top
                lookup[194, 9] = Block.Magma; // ?
                lookup[194, 10] = Block.Magma; // ?
                lookup[194, 11] = Block.Magma; // ?
                lookup[194, 12] = Block.Magma; // ?
                lookup[194, 13] = Block.Magma; // ?
                lookup[194, 14] = Block.Magma; // ?
                lookup[194, 15] = Block.Magma; // ?

                All(460);
                lookup[195, 0] = 263;
                lookup[195, 1] = 460; // ?
                lookup[195, 2] = 460; // Door bottom
                lookup[195, 3] = Block.Magma; // ?
                lookup[195, 4] = Block.Magma; // ?
                lookup[195, 5] = Block.Magma; // ?
                lookup[195, 6] = Block.Magma; // ?
                lookup[195, 7] = Block.Magma; // ?
                lookup[195, 8] = 461; // Door top
                lookup[195, 9] = Block.Magma; // ?
                lookup[195, 10] = Block.Magma; // ?
                lookup[195, 11] = Block.Magma; // ?
                lookup[195, 12] = Block.Magma; // ?
                lookup[195, 13] = Block.Magma; // ?
                lookup[195, 14] = Block.Magma; // ?
                lookup[195, 15] = Block.Magma; // ?

                All(460);
                lookup[196, 0] = 263;
                lookup[196, 1] = 460; // ?
                lookup[196, 2] = 460; // Door bottom
                lookup[196, 3] = Block.Magma; // ?
                lookup[196, 4] = Block.Magma; // ?
                lookup[196, 5] = Block.Magma; // ?
                lookup[196, 6] = Block.Magma; // ?
                lookup[196, 7] = Block.Magma; // ?
                lookup[196, 8] = 461; // Door top
                lookup[196, 9] = Block.Magma; // ?
                lookup[196, 10] = Block.Magma; // ?
                lookup[196, 11] = Block.Magma; // ?
                lookup[196, 12] = Block.Magma; // ?
                lookup[196, 13] = Block.Magma; // ?
                lookup[196, 14] = Block.Magma; // ?
                lookup[196, 15] = Block.Magma; // ?

                All(460);
                lookup[197, 0] = 263;
                lookup[197, 1] = 460; // ?
                lookup[197, 2] = 460; // Door bottom
                lookup[197, 3] = Block.Magma; // ?
                lookup[197, 4] = Block.Magma; // ?
                lookup[197, 5] = Block.Magma; // ?
                lookup[197, 6] = Block.Magma; // ?
                lookup[197, 7] = Block.Magma; // ?
                lookup[197, 8] = 461; // Door top
                lookup[197, 9] = Block.Magma; // ?
                lookup[197, 10] = Block.Magma; // ?
                lookup[197, 11] = Block.Magma; // ?
                lookup[197, 12] = Block.Magma; // ?
                lookup[197, 13] = Block.Magma; // ?
                lookup[197, 14] = Block.Magma; // ?
                lookup[197, 15] = Block.Magma; // ?

                All(464);
                All(Block.Purple);
                All(Block.Purple);
                All(176);
                All(177);
                All(176);
                All(176);
                All(176);
                All(221);
                All(Block.Magma); // NOT betroot block
                All(120);
                All(Block.Obsidian);
                All(80);
                All(80);
                All(82);
                All(62);
                All(154);
                All(92);
                All(228);
                All(Block.Air);
                All(Block.Cobblestone);
                All(Block.White);
                All(Block.Magenta);
                All(Block.Blue);
                All(Block.Yellow);
                All(Block.Lime);
                All(Block.LightPink);
                All(34);
                All(Block.Gray);
                All(Block.Cyan);
                All(Block.Purple);
                All(Block.DeepBlue);
                All(Block.BrownMushroom);
                All(Block.ForestGreen);
                All(Block.Red);
                All(373);
                All(Block.White);
                All(Block.Magenta);
                All(Block.Blue);
                All(Block.Yellow);
                All(Block.Lime);
                All(Block.LightPink);
                All(34);
                All(Block.Gray);
                All(Block.Cyan);
                All(Block.Purple);
                All(Block.DeepBlue);
                All(Block.BrownMushroom);
                All(Block.ForestGreen);
                All(Block.Red);
                All(373);

                All(289); // White concrete *
                lookup[251, 0] = 289;
                lookup[251, 1] = 281;
                lookup[251, 2] = 287;
                lookup[251, 3] = 285;
                lookup[251, 4] = 282;
                lookup[251, 5] = 283;
                lookup[251, 6] = 288;
                lookup[251, 7] = 290;
                lookup[251, 8] = 370;
                lookup[251, 9] = 343;
                lookup[251, 10] = 286;
                lookup[251, 11] = 295;
                lookup[251, 12] = 294;
                lookup[251, 13] = 293;
                lookup[251, 14] = 280;
                lookup[251, 15] = 291;

                All(289); // White concrete powder *
                lookup[252, 0] = 289;
                lookup[252, 1] = 281;
                lookup[252, 2] = 287;
                lookup[252, 3] = 285;
                lookup[252, 4] = 282;
                lookup[252, 5] = 283;
                lookup[252, 6] = 288;
                lookup[252, 7] = 290;
                lookup[252, 8] = 370;
                lookup[252, 9] = 343;
                lookup[252, 10] = 286;
                lookup[252, 11] = 295;
                lookup[252, 12] = 294;
                lookup[252, 13] = 293;
                lookup[252, 14] = 280;
                lookup[252, 15] = 291;

                All(Block.Magma);
            }

            static int src = 0;
            static void All(BlockID dst)
            {
                for (int i = 0; i < 16; i++)
                    lookup[src, i] = dst;
                src++;
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/ImportSchematic [url] [level name] &S- Imports a .schematic file using the Puissant Royale blocks.");
        }
    }
}
