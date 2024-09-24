/* 
  PvP Plugin created by Venk and Sirvoid.
  
  PLEASE NOTE:

  This plugin requires my VenkLib plugin. Install it from here: https://github.com/ddinan/ClassiCube-Stuff/blob/master/MCGalaxy/Plugins/VenkLib.cs.

  1. PING MAY AFFECT PVP.
  2. THE CODE (╫) IS USED FOR THE HALF-HEART EMOJI, YOU MAY NEED TO CHANGE THIS.
  
  TO SET UP PVP:
  1. Put this file into your plugins folder.
  2. Type /pcompile PvP.
  3. Type /pload PvP.
  4. Type /pvp add [name of map].
  
  IF YOU WANT TOOLS (DO THIS FOR EACH TOOL):
  1. Type /tool add [id] [speed] [durability] [type].
  
  IF YOU WANT MINEABLE BLOCKS (DO THIS FOR EACH BLOCK):
  1. Type /block add [id] [type] [durability].
  2. Add "mining=true" to the map's MOTD. (/map motd mining=true)
  
  IF YOU WANT POTIONS:
  1. Type /potion [secret code] [potion type] [amount].
  2. To use the potion, type /potion [potion type]
  
  IF YOU WANT SPRINTING:
  1. Include "-speed maxspeed=1.47" in /map motd.
  2. To sprint, hold shift while running.

  IF YOU WANT HUNGER:
  1. Include "+hunger" in /map motd.
  
  IF YOU WANT TO SHOW THE BLOCK YOU'RE HOLDING TO OTHER PLAYERS:
  1. Download my HoldBlocks plugin: https://github.com/ddinan/ClassiCube-Stuff/blob/master/MCGalaxy/Plugins/HoldBlocks.cs.

  IF YOU WANT MOB INSTRUCTIONS:
  1. Download my MobAI plugin: https://github.com/ddinan/ClassiCube-Stuff/blob/master/MCGalaxy/Plugins/MobAI.cs.

  IF YOU WANT A DAY-NIGHT CYCLE:
  1. Download my DayNightCycle plugin: https://github.com/ddinan/ClassiCube-Stuff/blob/master/MCGalaxy/Plugins/DayNightCycle.cs.
  2. Include "+daynightcycle" in /map motd.

  IF YOU WANT TO USE THE CUSTOM PlayerKilledByPlayer EVENT (for developers):
  1. Rename 'VenkLib.cs' to '_VenkLib.cs' and 'VenksSurvival.cs' to '_VenksSurvival.cs'.
  2. Also rename any .dll files if there are any.
  3. Register the event in your plugin's Load() method OnPlayerKilledByPlayerEvent.Register(HandlePlayerKilledByPlayer, Priority.Low);
  4. Create a handler method static void HandlePlayerKilledByPlayer(Player p, Player killer) { ... }
  5. Unregister the event in your plugin's Unload() method OnPlayerKilledByPlayerEvent.Unregister(HandlePlayerKilledByPlayer);
  
  TODO:
  1. Can still hit twice occasionally... let's disguise that as a critical hit for now?

 */

//reference System.Core.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MCGalaxy;
using MCGalaxy.Blocks;
using MCGalaxy.Blocks.Extended;
using MCGalaxy.Blocks.Physics;
using MCGalaxy.Bots;
using MCGalaxy.Commands;
using MCGalaxy.Config;
using MCGalaxy.Drawing.Ops;
using MCGalaxy.Events;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Games;
using MCGalaxy.Generator.Foliage;
using MCGalaxy.Maths;
using MCGalaxy.Network;
using MCGalaxy.Scripting;
using MCGalaxy.SQL;
using MCGalaxy.Tasks;

using BlockID = System.UInt16;

namespace MCGalaxy
{
    public class PvP : Plugin
    {
        public override string name { get { return "&aVenk's Survival%S"; } } // To unload /punload Survival
        public override string MCGalaxy_Version { get { return "1.9.4.1"; } }
        public override string creator { get { return "Venk and Sirvoid"; } }

        public class Config
        {
            [ConfigBool("gamemode-only", "Survival", true)]
            public static bool GamemodeOnly = false;

            [ConfigString("allowed-map-prefixes", "Survival", "example1_,example2_")]
            public static string AllowedMapPrefixes = "example1_,example2_";

            [ConfigBool("survival-damage", "Survival", true)]
            public static bool SurvivalDamage = true;

            [ConfigBool("regeneration", "Survival", true)]
            public static bool Regeneration = true;

            [ConfigBool("drowning", "Survival", true)]
            public static bool Drowning = true;

            [ConfigBool("fall-damage", "Survival", true)]
            public static bool FallDamage = true;

            [ConfigBool("hunger", "Survival", true)]
            public static bool Hunger = true;

            [ConfigBool("void-kills", "Survival", true)]
            public static bool VoidKills = true;

            [ConfigBool("mining", "Survival", true)]
            public static bool Mining = true;

            [ConfigBool("economy", "Survival", true)]
            public static bool Economy = true;

            [ConfigInt("bounty", "Survival", 1)]
            public static int Bounty = 1;

            [ConfigBool("mobs", "Survival", false)]
            public static bool Mobs = false;

            [ConfigInt("max-health", "Survival", 20)]
            public static int MaxHealth = 20;

            [ConfigString("path", "Extra", "./plugins/VenksSurvival/")]
            public static string Path = "./plugins/VenksSurvival/";

            [ConfigString("secret-code", "Extra", "unused")]
            public static string SecretCode = "unused";

            [ConfigBool("use-goodly-effects", "Extra", false)]
            public static bool UseGoodlyEffects = false;

            [ConfigString("hit-particle", "Extra", "pvp")]
            public static string HitParticle = "pvp";

            [ConfigString("break-particle", "Extra", "smokesmall")]
            public static string BreakParticle = "smokesmall";

            [ConfigBool("custom-physics", "Extra", false)]
            public static bool CustomPhysics = false;

            [ConfigInt("custom-water-block", "Extra", 102)]
            public static int CustomWaterBlock = 102;

            static ConfigElement[] cfg;
            public void Load()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.ParseFile(cfg, "./plugins/VenksSurvival/config.properties", this);
            }

            public void Save()
            {
                if (cfg == null) cfg = ConfigElement.GetAll(typeof(Config));
                ConfigElement.SerialiseSimple(cfg, "./plugins/VenksSurvival/config.properties", this);
            }
        }

        public static void MakeConfig()
        {
            using (StreamWriter w = new StreamWriter("./plugins/VenksSurvival/config.properties"))
            {
                w.WriteLine("# Edit the settings below to modify how the plugin operates.");
                w.WriteLine("# Whether or not this plugin should be controlled by other gamemode plugins. E.g, SkyWars.");
                w.WriteLine("gamemode-only = false");
                w.WriteLine("# If gamemode-only is enabled, all maps with this prefix will not have their inventories wiped when a player leaves.");
                w.WriteLine("allowed-map-prefixes = example1_,example2_");
                w.WriteLine("# Whether or not the player can die from natural causes. E.g, drowning.");
                w.WriteLine("survival-damage = true");
                w.WriteLine("# Whether or not players can drown.");
                w.WriteLine("drowning = true");
                w.WriteLine("# Whether or not players get hungry.");
                w.WriteLine("hunger = true");
                w.WriteLine("# Whether or not players take damage when falling from great heights.");
                w.WriteLine("fall-damage = true");
                w.WriteLine("# Whether or not players will die if their Y coordinate is below 0.");
                w.WriteLine("void-kills = false");
                w.WriteLine("# Whether or not players regenerate health.");
                w.WriteLine("regeneration = true");
                w.WriteLine("# Whether or not mining is enabled.");
                w.WriteLine("mining = true");
                w.WriteLine("# Whether or not players gain money for killing other players.");
                w.WriteLine("economy = true");
                w.WriteLine("# If economy is enabled, the amount of money players get for killing other players.");
                w.WriteLine("bounty = 1");
                //w.WriteLine("mobs = false # Whether or not mobs are toggled.");
                w.WriteLine("# The amount of health players start with.");
                w.WriteLine("max-health = 20");
                w.WriteLine("# Whether or not to use Goodly's effects plugin for particles. Note: Needs GoodlyEffects to work.");
                w.WriteLine("use-goodly-effects = false");
                w.WriteLine("# Whether or not to allow custom liquid physics.");
                w.WriteLine("custom-liquid-physics = false");
                w.WriteLine("# If custom-liquid-physics is enabled, the ID of the block to trigger the physics.");
                w.WriteLine("custom-physics-block = 102");
                w.WriteLine();
            }
        }

        public static Config cfg = new Config();

        public static List<string> maplist = new List<string>();
        public static string[,] recipes = new string[255, 2];

        public Dictionary<string, VenkBlock> blocks;
        public Dictionary<string, Tool> tools;

        public static SchedulerTask drownTask;
        public static SchedulerTask guiTask;
        public static SchedulerTask hungerTask;
        public static SchedulerTask regenTask;

        public override void Load(bool startup)
        {
            // Ensure directories exist before trying to read from them
            CreateDirectories();

            if (!File.Exists("./plugins/VenksSurvival/config.properties")) MakeConfig();

            // Initialize config
            cfg.Load();

            // Load data files

            blocks = LoadBlocks();
            tools = LoadTools();

            loadMaps();
            loadRecipes();
            initDB();

            // Register events
            if (Config.CustomPhysics)
            {
                OnBlockHandlersUpdatedEvent.Register(OnBlockHandlersUpdated, Priority.Low);

                Level[] levels = LevelInfo.Loaded.Items;
                foreach (Level lvl in levels)
                {
                    if (maplist.Contains(lvl.name)) AddCustomPhysics(lvl);
                }
            }

            if (Config.FallDamage || Config.Hunger || Config.VoidKills) OnPlayerMoveEvent.Register(HandlePlayerMove, Priority.High);

            OnBlockChangingEvent.Register(HandleBlockChanged, Priority.Low);
            OnGettingMotdEvent.Register(HandleGettingMotd, Priority.High);
            OnJoinedLevelEvent.Register(HandleOnJoinedLevel, Priority.Low);
            OnLevelLoadedEvent.Register(HandleOnLevelLoaded, Priority.Low);
            OnPlayerClickEvent.Register(HandleBlockClick, Priority.Low);
            OnPlayerClickEvent.Register(HandlePlayerClick, Priority.Low);
            OnPlayerDyingEvent.Register(HandlePlayerDying, Priority.High);

            // Queue tasks
            if (Config.Drowning) Server.MainScheduler.QueueRepeat(HandleDrown, null, TimeSpan.FromSeconds(1));
            if (Config.Hunger) Server.MainScheduler.QueueRepeat(HandleHunger, null, TimeSpan.FromSeconds(1));
            if (Config.Regeneration) Server.MainScheduler.QueueRepeat(HandleRegeneration, null, TimeSpan.FromSeconds(4));

            Server.MainScheduler.QueueRepeat(HandleGUI, null, TimeSpan.FromMilliseconds(50));

            // Register commands
            Command.Register(new CmdCraft());
            Command.Register(new CmdPvP());
            Command.Register(new CmdRecipes());
            Command.Register(new CmdSafeZone());
            Command.Register(new CmdTool());
            Command.Register(new CmdBlock());
            Command.Register(new CmdPotion());
            Command.Register(new CmdDropBlock());
            Command.Register(new CmdPickupBlock());
            Command.Register(new CmdInventory());

            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (!maplist.Contains(pl.level.name)) continue;

                ResetPlayerState(pl);
            }
        }

        public override void Unload(bool shutdown)
        {
            // Unload events
            if (Config.CustomPhysics)
            {
                OnBlockHandlersUpdatedEvent.Unregister(OnBlockHandlersUpdated);
            }

            if (Config.FallDamage || Config.Hunger || Config.VoidKills) OnPlayerMoveEvent.Unregister(HandlePlayerMove);

            OnBlockChangingEvent.Unregister(HandleBlockChanged);
            OnGettingMotdEvent.Unregister(HandleGettingMotd);
            OnJoinedLevelEvent.Unregister(HandleOnJoinedLevel);
            OnLevelLoadedEvent.Unregister(HandleOnLevelLoaded);
            OnPlayerClickEvent.Unregister(HandleBlockClick);
            OnPlayerClickEvent.Unregister(HandlePlayerClick);
            OnPlayerDyingEvent.Unregister(HandlePlayerDying);

            // Unload commands
            Command.Unregister(Command.Find("Craft"));
            Command.Unregister(Command.Find("PvP"));
            Command.Unregister(Command.Find("Recipes"));
            Command.Unregister(Command.Find("SafeZone"));
            Command.Unregister(Command.Find("Weapon"));
            Command.Unregister(Command.Find("Tool"));
            Command.Unregister(Command.Find("Block"));
            Command.Unregister(Command.Find("Potion"));
            Command.Unregister(Command.Find("DropBlock"));
            Command.Unregister(Command.Find("PickupBlock"));
            Command.Unregister(Command.Find("Inventory"));

            // Unload tasks
            Server.MainScheduler.Cancel(guiTask);
            Server.MainScheduler.Cancel(drownTask);
            Server.MainScheduler.Cancel(hungerTask);
            Server.MainScheduler.Cancel(regenTask);
        }

        void CreateDirectories()
        {
            Directory.CreateDirectory("./plugins/VenksSurvival");
            Directory.CreateDirectory("./plugins/VenksSurvival/tools/");
        }

        public class VenkBlock
        {
            // Properties
            public string Name { get; set; }
            public float Hardness { get; set; }
            public int Tool { get; set; }
            public float DefaultSpeed { get; set; }
            public float WoodenSpeed { get; set; }
            public float StoneSpeed { get; set; }
            public float IronSpeed { get; set; }

            // Constructor
            public VenkBlock(string name, float hardness, int tool, float defaultSpeed, float woodenSpeed, float stoneSpeed, float ironSpeed)
            {
                Name = name;
                Hardness = hardness;
                Tool = tool;
                DefaultSpeed = defaultSpeed;
                WoodenSpeed = woodenSpeed;
                StoneSpeed = stoneSpeed;
                IronSpeed = ironSpeed;
            }
        }

        private Dictionary<string, VenkBlock> LoadBlocks()
        {
            Dictionary<string, VenkBlock> blocks = new Dictionary<string, VenkBlock>();

            try
            {
                if (!File.Exists(Config.Path + "blocks.txt")) File.Create(Config.Path + "blocks.txt").Dispose();

                string[] lines = File.ReadAllLines(Config.Path + "blocks.txt");

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("//")) continue; // Ignore invalid lines

                    string[] data = line.Split(';');

                    if (data.Length == 7) // Assuming there are 7 properties in the file
                    {
                        VenkBlock block = new VenkBlock(
                            data[0], // ID
                            float.Parse(data[1]), // Hardness
                            int.Parse(data[2]), // Optimal tool type
                            float.Parse(data[3]), // Default speed
                            float.Parse(data[4]), // Wooden speed
                            float.Parse(data[5]), // Stone speed
                            float.Parse(data[6]) // Iron speed
                        );

                        blocks[block.Name] = block; // Add block to the dictionary using the name as the key
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error reading file: " + ex.Message);
            }

            return blocks;
        }

        public class Tool
        {
            // Properties
            public string Name { get; set; }
            public float Durability { get; set; }
            public int Type { get; set; }
            public int Damage { get; set; }

            // Constructor
            public Tool(string name, float durability, int type, int damage)
            {
                Name = name;
                Durability = durability;
                Type = type;
                Damage = damage;
            }
        }

        private Dictionary<string, Tool> LoadTools()
        {
            Dictionary<string, Tool> tools = new Dictionary<string, Tool>();

            try
            {
                if (!File.Exists(Config.Path + "tools.txt")) File.Create(Config.Path + "tools.txt").Dispose();

                string[] lines = File.ReadAllLines(Config.Path + "tools.txt");

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("//")) continue; // Ignore invalid lines

                    string[] data = line.Split(';');

                    if (data.Length == 4) // Assuming there are 8 properties in the file
                    {
                        Tool tool = new Tool(
                            data[0], // ID
                            float.Parse(data[1]), // Durability
                            int.Parse(data[2]), // Optimal block type
                            int.Parse(data[3]) // Amount of damage inflicted
                        );

                        tools[tool.Name] = tool; // Add tool to the dictionary using the name as the key
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error reading file: " + ex.Message);
            }

            return tools;
        }

        static void AddCustomPhysics(Level lvl)
        {
            if (!maplist.Contains(lvl.name)) return;

            lvl.PhysicsHandlers[Block.FromRaw((ushort)Config.CustomWaterBlock)] = CustomLiquidPhysics.DoFlood;
            lvl.PhysicsHandlers[Block.Leaves] = CustomLeafPhysics.DoLeaf;
            lvl.PhysicsHandlers[Block.Log] = CustomLeafPhysics.DoLog;
            lvl.PhysicsHandlers[Block.Sapling] = CustomSaplingPhysics.DoSapling;
            lvl.PhysicsHandlers[Block.FromRaw(83)] = SugarCanePhysics.DoSugarCane;
        }

        static void OnBlockHandlersUpdated(Level lvl, BlockID block)
        {
            if (maplist.Contains(lvl.name)) AddCustomPhysics(lvl);
        }

        static void HandleOnLevelLoaded(Level lvl)
        {
            if (maplist.Contains(lvl.name)) AddCustomPhysics(lvl);
        }

        /// <summary>
        /// Resets the specified player's variables. Commonly used for player deaths or when a player joins a level.
        /// </summary>
        /// <param name="p"></param>

        public static void ResetPlayerState(Player p)
        {
            p.Extras["SURVIVAL_HEALTH"] = Config.MaxHealth;
            p.Extras["HUNGER"] = 1000;
            p.Extras["DROWNING"] = 20;
            p.SendCpeMessage(CpeMessageType.BottomRight1, GetHealthBar(20));
        }

        #region Database management

        ColumnDesc[] createPotions = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
                new ColumnDesc("Health", ColumnType.Int32),
                new ColumnDesc("Speed", ColumnType.Int32),
                new ColumnDesc("Invisible", ColumnType.Int32),
                new ColumnDesc("Jump", ColumnType.Int32),
                new ColumnDesc("Waterbreathing", ColumnType.Int32),
                new ColumnDesc("Damage", ColumnType.Int32),
                new ColumnDesc("Strength", ColumnType.Int32),
                new ColumnDesc("Slowness", ColumnType.Int32),
                new ColumnDesc("Blindness", ColumnType.Int32),
        };

        ColumnDesc[] createInventories = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
                new ColumnDesc("Slot1", ColumnType.VarChar, 16),
                new ColumnDesc("Slot2", ColumnType.VarChar, 16),
                new ColumnDesc("Slot3", ColumnType.VarChar, 16),
                new ColumnDesc("Slot4", ColumnType.VarChar, 16),
                new ColumnDesc("Slot5", ColumnType.VarChar, 16),
                new ColumnDesc("Slot6", ColumnType.VarChar, 16),
                new ColumnDesc("Slot7", ColumnType.VarChar, 16),
                new ColumnDesc("Slot8", ColumnType.VarChar, 16),
                new ColumnDesc("Slot9", ColumnType.VarChar, 16),
                new ColumnDesc("Slot10", ColumnType.VarChar, 16),
                new ColumnDesc("Slot11", ColumnType.VarChar, 16),
                new ColumnDesc("Slot12", ColumnType.VarChar, 16),
                new ColumnDesc("Slot13", ColumnType.VarChar, 16),
                new ColumnDesc("Slot14", ColumnType.VarChar, 16),
                new ColumnDesc("Slot15", ColumnType.VarChar, 16),
                new ColumnDesc("Slot16", ColumnType.VarChar, 16),
                new ColumnDesc("Slot17", ColumnType.VarChar, 16),
                new ColumnDesc("Slot18", ColumnType.VarChar, 16),
                new ColumnDesc("Slot19", ColumnType.VarChar, 16),
                new ColumnDesc("Slot20", ColumnType.VarChar, 16),
                new ColumnDesc("Slot21", ColumnType.VarChar, 16),
                new ColumnDesc("Slot22", ColumnType.VarChar, 16),
                new ColumnDesc("Slot23", ColumnType.VarChar, 16),
                new ColumnDesc("Slot24", ColumnType.VarChar, 16),
                new ColumnDesc("Slot25", ColumnType.VarChar, 16),
                new ColumnDesc("Slot26", ColumnType.VarChar, 16),
                new ColumnDesc("Slot27", ColumnType.VarChar, 16),
                new ColumnDesc("Slot28", ColumnType.VarChar, 16),
                new ColumnDesc("Slot29", ColumnType.VarChar, 16),
                new ColumnDesc("Slot30", ColumnType.VarChar, 16),
                new ColumnDesc("Slot31", ColumnType.VarChar, 16),
                new ColumnDesc("Slot32", ColumnType.VarChar, 16),
                new ColumnDesc("Slot33", ColumnType.VarChar, 16),
                new ColumnDesc("Slot34", ColumnType.VarChar, 16),
                new ColumnDesc("Slot35", ColumnType.VarChar, 16),
                new ColumnDesc("Slot36", ColumnType.VarChar, 16),
                new ColumnDesc("Level", ColumnType.VarChar, 32),
        };

        ColumnDesc[] createChests = new ColumnDesc[] {
            new ColumnDesc("Level", ColumnType.VarChar, 16),
                new ColumnDesc("Coords", ColumnType.VarChar, 16),
                new ColumnDesc("Slot1", ColumnType.VarChar, 16),
                new ColumnDesc("Slot2", ColumnType.VarChar, 16),
                new ColumnDesc("Slot3", ColumnType.VarChar, 16),
                new ColumnDesc("Slot4", ColumnType.VarChar, 16),
                new ColumnDesc("Slot5", ColumnType.VarChar, 16),
                new ColumnDesc("Slot6", ColumnType.VarChar, 16),
                new ColumnDesc("Slot7", ColumnType.VarChar, 16),
                new ColumnDesc("Slot8", ColumnType.VarChar, 16),
                new ColumnDesc("Slot9", ColumnType.VarChar, 16),
                new ColumnDesc("Slot10", ColumnType.VarChar, 16),
                new ColumnDesc("Slot11", ColumnType.VarChar, 16),
                new ColumnDesc("Slot12", ColumnType.VarChar, 16),
                new ColumnDesc("Slot13", ColumnType.VarChar, 16),
                new ColumnDesc("Slot14", ColumnType.VarChar, 16),
                new ColumnDesc("Slot15", ColumnType.VarChar, 16),
                new ColumnDesc("Slot16", ColumnType.VarChar, 16),
                new ColumnDesc("Slot17", ColumnType.VarChar, 16),
                new ColumnDesc("Slot18", ColumnType.VarChar, 16),
                new ColumnDesc("Slot19", ColumnType.VarChar, 16),
                new ColumnDesc("Slot20", ColumnType.VarChar, 16),
                new ColumnDesc("Slot21", ColumnType.VarChar, 16),
                new ColumnDesc("Slot22", ColumnType.VarChar, 16),
                new ColumnDesc("Slot23", ColumnType.VarChar, 16),
                new ColumnDesc("Slot24", ColumnType.VarChar, 16),
                new ColumnDesc("Slot25", ColumnType.VarChar, 16),
                new ColumnDesc("Slot26", ColumnType.VarChar, 16),
                new ColumnDesc("Slot27", ColumnType.VarChar, 16),
                new ColumnDesc("Slot28", ColumnType.VarChar, 16),
                new ColumnDesc("Slot29", ColumnType.VarChar, 16),
                new ColumnDesc("Slot30", ColumnType.VarChar, 16),
                new ColumnDesc("Slot31", ColumnType.VarChar, 16),
                new ColumnDesc("Slot32", ColumnType.VarChar, 16),
                new ColumnDesc("Slot33", ColumnType.VarChar, 16),
                new ColumnDesc("Slot34", ColumnType.VarChar, 16),
                new ColumnDesc("Slot35", ColumnType.VarChar, 16),
                new ColumnDesc("Slot36", ColumnType.VarChar, 16),
        };

        void initDB()
        {
            Database.CreateTable("Potions", createPotions);
            Database.CreateTable("Inventories4", createInventories);
            Database.CreateTable("Chests", createChests);
        }

        #endregion

        void loadMaps()
        {
            if (File.Exists(Config.Path + "maps.txt"))
            {
                using (var maplistreader = new StreamReader(Config.Path + "maps.txt"))
                {
                    string line;
                    while ((line = maplistreader.ReadLine()) != null)
                    {
                        maplist.Add(line);
                    }
                }
            }
            else File.Create(Config.Path + "maps.txt").Dispose();
        }

        void HandleGettingMotd(Player p, ref string motd)
        {
            p.Extras["MOTD"] = motd;
        }

        #region GUI

        void HandleGUI(SchedulerTask task)
        {
            guiTask = task;

            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (pl.Extras.GetBoolean("SURVIVAL_HIDE_HUD")) continue;

                if (maplist.Contains(pl.level.name))
                {
                    BlockID block = pl.GetHeldBlock();
                    string held = Block.GetName(pl, block);

                    List<string[]> pRows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", pl.truename, pl.level.name);

                    int column = 0;
                    int amount = 0;

                    if (pRows.Count == 0) amount = 0;
                    else
                    {
                        column = FindActiveSlot(pRows[0], GetID(block));

                        if (column == 0) amount = 0;
                        else
                        {
                            amount = pRows[0][column].ToString().StartsWith("0") ? 0 : Int32.Parse(pRows[0][column].ToString().Replace(GetID(block), "").Replace("(", "").Replace(")", ""));
                        }
                    }

                    decimal hunger = Math.Floor((decimal)(pl.Extras.GetInt("HUNGER") / 50));

                    pl.SendCpeMessage(CpeMessageType.BottomRight3, "%a" + held + " %8| %7x" + amount);
                    pl.SendCpeMessage(CpeMessageType.BottomRight2, "%f╒ %720 %bo %7" + pl.Extras.GetInt("DROWNING") + " %f▀ %7" + hunger);

                    // Health is typically handled in this plugin but we need to show this for support in other plugins
                    pl.SendCpeMessage(CpeMessageType.BottomRight1, GetHealthBar(pl.Extras.GetInt("SURVIVAL_HEALTH")));
                }
            }
        }

        void HandlePlayerDying(Player p, BlockID deathblock, ref bool cancel)
        {
            if (!maplist.Contains(p.level.name)) return;
            ResetPlayerState(p);
        }

        #endregion

        #region Drowning

        public static string GetHealthBar(int health)
        {
            if (health == 20) return "%f♥♥♥♥♥♥♥♥♥♥";
            if (health == 19) return "%f♥♥♥♥♥♥♥♥♥╫";
            if (health == 18) return "%f♥♥♥♥♥♥♥♥♥%0♥";
            if (health == 17) return "%f♥♥♥♥♥♥♥♥╫%0♥";
            if (health == 16) return "%f♥♥♥♥♥♥♥♥%0♥♥";
            if (health == 15) return "%f♥♥♥♥♥♥♥╫%0♥♥";
            if (health == 14) return "%f♥♥♥♥♥♥♥%0♥♥♥";
            if (health == 13) return "%f♥♥♥♥♥♥╫%0♥♥♥";
            if (health == 12) return "%f♥♥♥♥♥♥%0♥♥♥♥";
            if (health == 11) return "%f♥♥♥♥♥╫%0♥♥♥♥";
            if (health == 10) return "%f♥♥♥♥♥%0♥♥♥♥♥";
            if (health == 9) return "%f♥♥♥♥╫%0♥♥♥♥♥";
            if (health == 8) return "%f♥♥♥♥%0♥♥♥♥♥♥";
            if (health == 7) return "%f♥♥♥╫%0♥♥♥♥♥♥";
            if (health == 6) return "%f♥♥♥%0♥♥♥♥♥♥♥";
            if (health == 5) return "%f♥♥╫%0♥♥♥♥♥♥♥";
            if (health == 4) return "%f♥♥%0♥♥♥♥♥♥♥♥";
            if (health == 3) return "%f♥╫%0♥♥♥♥♥♥♥♥";
            if (health == 2) return "%f♥%0♥♥♥♥♥♥♥♥♥";
            if (health == 1) return "%f╫%0♥♥♥♥♥♥♥♥♥";
            if (health == 0) return "%0♥♥♥♥♥♥♥♥♥♥";
            return "";
        }

        public static void DoDamage(Player p, int damage, string type, Player killer)
        {
            int health = p.Extras.GetInt("SURVIVAL_HEALTH");
            p.Extras["SURVIVAL_HEALTH"] = health - damage;
            health = p.Extras.GetInt("SURVIVAL_HEALTH");

            p.SendCpeMessage(CpeMessageType.BottomRight1, GetHealthBar(health));

            if (health <= 0) KillPlayer(p, type, killer);

            else if (p.Session.ClientName().CaselessContains("cef"))
            {
                if (type == "drown") p.Message("cef resume -n hit"); // Play hit sound effect
                if (type == "fall") p.Message("cef resume -n fall"); // Play fall sound effect
            }
        }

        public static void KillPlayer(Player p, string type, Player killer)
        {
            if (type == "drown") p.HandleDeath(Block.Water);
            if (type == "fall") p.HandleDeath(Block.Red); // Horrible hack to display custom fall death message
            if (type == "void") p.HandleDeath(Block.Orange); // Horrible hack to display custom void death message
            else p.HandleDeath(Block.Cobblestone);

            if (type == "pvp" & killer != null)
            {
                OnPlayerKilledByPlayerEvent.Call(p, killer);
            }

            if (p.Session.ClientName().CaselessContains("cef")) p.Message("cef resume -n death"); // Play death sound effect
        }

        void HandleDrown(SchedulerTask task)
        {
            drownTask = task;

            if (!Config.SurvivalDamage) return;

            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (maplist.Contains(pl.level.name))
                {
                    if (pl.invincible) continue;

                    ushort x = (ushort)(pl.Pos.X / 32);
                    ushort y = (ushort)((pl.Pos.Y - Entities.CharacterHeight) / 32);
                    ushort y2 = (ushort)(((pl.Pos.Y - Entities.CharacterHeight) / 32) + 1);
                    ushort z = (ushort)(pl.Pos.Z / 32);

                    BlockID block = pl.level.GetBlock((ushort)x, ((ushort)y), (ushort)z);
                    BlockID block2 = pl.level.GetBlock((ushort)x, ((ushort)y2), (ushort)z);

                    string body = Block.GetName(pl, block);
                    string head = Block.GetName(pl, block2);

                    if (body == "Water" && head == "Water")
                    {
                        int number = pl.Extras.GetInt("DROWNING");
                        pl.Extras["DROWNING"] = number - 1;
                        int air = pl.Extras.GetInt("DROWNING");
                        // (10 - number) + 1)

                        // If player is out of air, start doing damage
                        if (air < 0) DoDamage(pl, 1, "drown", null);
                    }
                    else
                    {
                        pl.Extras["DROWNING"] = 20;
                    }
                }
            }
        }

        #endregion

        #region Regeneration

        void HandleRegeneration(SchedulerTask task)
        {
            regenTask = task;

            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (!maplist.Contains(pl.level.name)) continue;

                int hunger = pl.Extras.GetInt("HUNGER");
                int health = pl.Extras.GetInt("SURVIVAL_HEALTH");

                if (health == Config.MaxHealth) continue; // No need to regenerate health if player is already at max health
                if (Math.Floor((decimal)(hunger / 50)) < 18) continue; // Only regenerate health if player has 18+ hunger points

                pl.Extras["SURVIVAL_HEALTH"] = health + 1;
            }
        }

        #endregion

        #region Hunger

        void HandleHunger(SchedulerTask task)
        {
            hungerTask = task;

            if (!Config.SurvivalDamage || !Config.Hunger) return;

            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (maplist.Contains(pl.level.name) && pl.level.Config.MOTD.ToLower().Contains("+hunger"))
                {
                    if (pl.invincible) continue;

                    int hunger = pl.Extras.GetInt("HUNGER");

                    // Start depleting health if hunger is 0 (this isn't possible currently since hunger stops at 6)
                    if (hunger == 0)
                    {
                        int health = pl.Extras.GetInt("SURVIVAL_HEALTH");

                        // If player has 5 or less hearts left, don't bother doing damage
                        if (health <= 10) continue;

                        if (pl.Session.ClientName().CaselessContains("cef")) pl.Message("cef resume -n hit"); // Play hit sound effect
                        pl.Extras["SURVIVAL_HEALTH"] = health - 1;
                    }
                }
            }
        }

        public bool DetectSprint(Player p, Position newPos)
        {
            if (p.invincible || p.Game.Referee) return false;
            int dx = Math.Abs(p.Pos.X - newPos.X), dz = Math.Abs(p.Pos.Z - newPos.Z);

            bool speeding = dx >= 8 || dz >= 8;

            //p.Message((speeding ? "%a" : "%c")  + speeding + " %edx " + dx + " dz " + dz);
            return speeding;
        }

        #endregion

        #region Fall damage

        void HandlePlayerMove(Player p, Position next, byte rotX, byte rotY, ref bool cancel)
        {
            if (maplist.Contains(p.level.name))
            {
                if (Config.VoidKills && next.Y < 0) KillPlayer(p, "void", null); // Player fell out of the world

                if (Config.Hunger && p.level.Config.MOTD.ToLower().Contains("+hunger"))
                {
                    int hunger = p.Extras.GetInt("HUNGER");
                    // Check to see if the player is sprinting and has at least 6 hunger points
                    if (DetectSprint(p, next) && hunger > 300)
                    {
                        p.Extras["SPRINTING"] = true;
                        p.Extras["SPRINT_TIME"] = p.Extras.GetInt("SPRINT_TIME") + 1;

                        // Deplete hunger by 1 until the player is starving
                        if (hunger > 0)
                        {
                            p.Extras["HUNGER"] = p.Extras.GetInt("HUNGER") - 1;

                            // If player has 6 (6 * 50 = 300) hunger points, stop sprinting
                            if (p.Extras.GetInt("HUNGER") <= 300)
                            {
                                int heldFor = p.Extras.GetInt("SPRINT_TIME");

                                TimeSpan duration = TimeSpan.FromSeconds(heldFor);

                                p.Extras["SPRINTING"] = false;
                                p.Extras["SPRINT_TIME"] = 0;

                                // Disallow sprinting. 'nospeed' is a temporary flag for replacing later on, should a player replenish their hunger

                                string motd = p.Extras.GetString("MOTD").Replace("maxspeed=", "nospeed=");
                                p.Extras["MOTD"] = motd;

                                p.Send(Packet.Motd(p, motd));
                            }
                        }
                    }

                    else if (!DetectSprint(p, next) && p.Extras.GetBoolean("SPRINTING"))
                    {
                        int heldFor = p.Extras.GetInt("SPRINT_TIME");

                        // The client's click speed is ~4 times/second
                        TimeSpan duration = TimeSpan.FromSeconds(heldFor);

                        p.Extras["SPRINTING"] = false;
                        p.Extras["SPRINT_TIME"] = 0;
                    }
                }

                if (Config.FallDamage && p.level.Config.SurvivalDeath)
                {
                    if (p.invincible || Hacks.CanUseFly(p)) return;

                    ushort x = (ushort)(p.Pos.X / 32);
                    ushort y = (ushort)(((p.Pos.Y - Entities.CharacterHeight) / 32) - 1);
                    ushort y2 = (ushort)(((p.Pos.Y - Entities.CharacterHeight) / 32) - 2);
                    ushort z = (ushort)(p.Pos.Z / 32);

                    BlockID block = p.level.GetBlock((ushort)x, ((ushort)y), (ushort)z);
                    BlockID block2 = p.level.GetBlock((ushort)x, ((ushort)y2), (ushort)z);

                    string below = Block.GetName(p, block);
                    string below2 = Block.GetName(p, block2);

                    // Don't do fall damage if player lands in deep water (2+ depth)

                    if (below.ToLower().Contains("water") && below2.ToLower().Contains("water"))
                    {
                        int fall = p.Extras.GetInt("FALL_START") - y;
                        if (fallDamage(fall) > 0 && p.Session.ClientName().CaselessContains("cef")) p.Message("cef resume -n splash"); // Play splash sound effect
                        p.Extras["FALLING"] = false;
                        p.Extras["FALL_START"] = y;
                        return;
                    }

                    if (!p.Extras.GetBoolean("FALLING") && below.ToLower() == "air")
                    {
                        p.Extras["FALLING"] = true;
                        p.Extras["FALL_START"] = y;
                    }

                    else if (p.Extras.GetBoolean("FALLING") && below.ToLower() != "air")
                    {
                        if (p.Extras.GetBoolean("FALLING"))
                        {
                            int fall = p.Extras.GetInt("FALL_START") - y;

                            if (fallDamage(fall) > 0) DoDamage(p, fallDamage(fall), "fall", null);

                            // Reset extra variables
                            p.Extras["FALLING"] = false;
                            p.Extras["FALL_START"] = y;
                        }
                    }
                }
            }
        }

        int fallDamage(int fallBlocks)
        {
            int fb = fallBlocks;
            if (fb < 4) return 0;
            if (fb == 4) return 1;
            if (fb == 5) return 2;
            if (fb == 6) return 3;
            if (fb == 7) return 4;
            if (fb == 8) return 5;
            if (fb == 9) return 6;
            if (fb == 10) return 7;
            if (fb == 11) return 8;
            if (fb == 12) return 9;
            if (fb == 13) return 10;
            if (fb == 14) return 11;
            if (fb == 15) return 12;
            if (fb == 16) return 13;
            if (fb == 17) return 14;
            if (fb == 18) return 15;
            if (fb == 19) return 16;
            if (fb == 20) return 17;
            if (fb == 21) return 18;
            if (fb == 22) return 19;
            if (fb >= 23) return 20;
            return 0;
        }

        #endregion

        #region Mining blocks

        string RemoveExcess(string text)
        {
            string stopAt = "(";
            if (!String.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }

            return String.Empty;
        }

        static int FindSlotFor(string[] row, string name)
        {
            for (int col = 1; col <= 36; col++)
            {
                string contents = row[col];
                if (contents == "0" || contents.StartsWith(name + "(")) return col;
            }

            return 0;
        }

        public static int FindActiveSlot(string[] row, string name)
        {
            for (int col = 1; col <= 36; col++)
            {
                string contents = row[col];
                if (contents.StartsWith(name + "(")) return col;
            }

            return 0;
        }

        public static string GetID(BlockID block)
        {
            string id = block.ToString();
            if (Convert.ToInt32(block) >= 66) id = (block - 256).ToString(); // Need to convert block if ID is over 66

            return "b" + id;
        }

        public static void UpdateBlockList(Player p, int column)
        {
            List<string[]> pRows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);

            if (pRows.Count > 0)
            {
                if (pRows[0][column].ToString().StartsWith("0"))
                {
                    p.Send(Packet.SetInventoryOrder(Block.Air, (BlockID)column, p.Session.hasExtBlocks));
                }

                else
                {
                    // Need to trim raw code to get the ID of the block. The example below is for the ID 75:
                    // ID of block = 75, amount of block = 22
                    // b75(22) -> 75
                    string raw = pRows[0][column].ToString();

                    int from = raw.IndexOf("b") + "b".Length;
                    int to = raw.LastIndexOf("(");

                    string id = raw.Substring(from, to - from);
                    p.Send(Packet.SetInventoryOrder((BlockID)Convert.ToUInt16(id), (BlockID)column, p.Session.hasExtBlocks));
                }
            }
        }

        public static void SaveBlock(Player p, BlockID block, int amount)
        {
            string name = Block.GetName(p, block);

            if (name.ToLower().Contains("air") || name.ToLower().Contains("unknown") || name.ToLower().Contains("water") || name.ToLower().Contains("lava")) return;

            /*if (name.StartsWith("Chest-"))
            {
                RemoveChest(p, x, y, z);
            }*/

            List<string[]> pRows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);

            if (pRows.Count == 0)
            {
                Database.AddRow("Inventories4", "Name, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7, Slot8, Slot9, Slot10, Slot11, Slot12, Slot13, Slot14," +
                "Slot15, Slot16, Slot17, Slot18, Slot19, Slot20, Slot21, Slot22, Slot23, Slot24, Slot25, Slot26, Slot27, Slot28, Slot29," +
                "Slot30, Slot31, Slot32, Slot33, Slot34, Slot35, Slot36, Level", p.truename, GetID(block) + "(" + amount + ")", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", p.level.name);

                UpdateBlockList(p, 1);
                return;
            }

            else
            {
                int column = FindSlotFor(pRows[0], GetID(block));

                if (column == 0)
                {
                    p.Message("Your inventory is full.");
                    return;
                }

                int newCount = pRows[0][column].ToString().StartsWith("0") ? amount : Int32.Parse(pRows[0][column].ToString().Replace(GetID(block), "").Replace("(", "").Replace(")", "")) + amount;

                Database.UpdateRows("Inventories4", "Slot" + column.ToString() + "=@1", "WHERE Name=@0 AND Level=@2", p.truename, GetID(block) + "(" + newCount.ToString() + ")", p.level.name);

                UpdateBlockList(p, column);
                return;
            }
        }

        /// <summary>
        /// Replaces directional suffixes (-N, -S, -W, -E, -U, -D) for generalization.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>

        public static string ReplaceSuffixes(string name)
        {
            // Remove directional suffixes (-N, -S, -W, -E, -U, -D)
            // We will use '-D' as the destination block for vertical suffixes (slabs) and -N for horizontal suffixes (walls/stairs/chests etc)

            if (name.EndsWith("-U") || name.EndsWith("-D"))
            {
                var suffix = name.Substring(name.LastIndexOf('-'));
                name = name.Replace(suffix, "-D");
            }

            else if (name.Contains("-"))
            {
                var suffix = name.Substring(name.LastIndexOf('-'));

                // The log blocks have hardcoded behaviour due to having both vertical and horizontal variants
                if (name.Contains("Log"))
                {
                    name = name.Replace(suffix, "-UD");
                }

                else name = name.Replace(suffix, "-N");
            }

            return name;
        }

        bool HasBlock(Player p, BlockID block)
        {
            List<string[]> pRows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);

            if (pRows.Count == 0) return false;

            string name = Block.GetName(p, block);
            name = ReplaceSuffixes(name); // Remove suffixes such as -N, -U etc
            if (!CommandParser.GetBlock(p, name, out block)) return false;

            int column = 0;

            if (name.ToLower() == "grass") block = Block.Dirt;
            else column = FindActiveSlot(pRows[0], GetID(block));

            if (column == 0) return false;
            return true;
        }

        void HandleBlockChanged(Player p, ushort x, ushort y, ushort z, BlockID block, bool placing, ref bool cancel)
        {
            if (!maplist.Contains(p.level.name)) return;
            if (!p.level.Config.MOTD.ToLower().Contains("mining=true")) return;

            if (p.invincible || p.Game.Referee)
            {
                p.Message("%f╒ &c∩αΓ: &7You cannot modify blocks as a spectator.");
                p.RevertBlock(x, y, z);
                cancel = true;
                return;
            }

            if (placing)
            {
                List<string[]> pRows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);

                if (pRows.Count == 0)
                {
                    p.Message("%SYou do not have any of this block.");
                    p.RevertBlock(x, y, z);
                    cancel = true;
                    return;
                }

                else
                {
                    string name = Block.GetName(p, block);
                    name = ReplaceSuffixes(name); // Remove suffixes such as -N, -U etc
                    if (!CommandParser.GetBlock(p, name, out block)) return;

                    int column = 0;

                    if (name.ToLower() == "grass") block = Block.Dirt;
                    else column = FindActiveSlot(pRows[0], GetID(block));

                    if (column == 0)
                    {
                        p.Message("%SYou do not have any of this block.");
                        p.RevertBlock(x, y, z);
                        cancel = true;
                        return;
                    }

                    int newCount = pRows[0][column].ToString().StartsWith("0") ? 1 : Int32.Parse(pRows[0][column].ToString().Replace(GetID(block), "").Replace("(", "").Replace(")", "")) - 1;

                    if (newCount == 0)
                    {
                        Database.UpdateRows("Inventories4", "Slot" + column.ToString() + "=@1", "WHERE Name=@0 AND Level=@2", p.truename, "0", p.level.name);

                        p.Send(Packet.SetInventoryOrder(Block.Air, (BlockID)column, p.Session.hasExtBlocks));
                    }
                    else
                    {
                        UpdateBlockList(p, column);
                        Database.UpdateRows("Inventories4", "Slot" + column.ToString() + "=@1", "WHERE Name=@0 AND Level=@2", p.truename, GetID(block) + "(" + newCount.ToString() + ")", p.level.name);
                    }
                }
            }
        }

        float CalculateBreakingTime(bool isBestTool, float defaultSpeed, float toolMultiplier, bool canHarvest, bool toolEfficiency, int efficiencyLevel, bool hasteEffect, int hasteLevel, bool miningFatigue, int miningFatigueLevel, bool inWater, bool hasAquaAffinity, bool onGround, float blockHardness)
        {
            float speedMultiplier = defaultSpeed;

            if (isBestTool)
            {
                speedMultiplier = toolMultiplier;

                if (!canHarvest) speedMultiplier = 1;
                else if (toolEfficiency) speedMultiplier += (float)(Math.Pow(efficiencyLevel, 2) + 1);
            }

            if (hasteEffect) speedMultiplier *= 0.2f * hasteLevel + 1;

            if (miningFatigue) speedMultiplier *= (float)Math.Pow(0.3f, Math.Min(miningFatigueLevel, 4));

            if (inWater && !hasAquaAffinity) speedMultiplier /= 5;

            if (!onGround) speedMultiplier /= 5;

            float damage = speedMultiplier / blockHardness;

            if (canHarvest) damage /= 30;
            else damage /= 100;

            if (damage > 1) return 0;

            int ticks = (int)Math.Ceiling(1 / damage);
            float seconds = ticks / 20f;

            return seconds;
        }

        void MineBlock(Player p, ushort x, ushort y, ushort z, BlockID block, string name)
        {
            name = ReplaceSuffixes(name); // Remove suffixes such as -N, -U etc
            if (!CommandParser.GetBlock(p, name, out block)) return;

            if (block == Block.Grass) SaveBlock(p, Block.Dirt, 1);
            else if (block == Block.Stone) SaveBlock(p, Block.Cobblestone, 1);
            else if (block == Block.Leaves)
            {
                // Chance of dropping sapling and apples
                decimal saplingChance = CustomLeafPhysics.GetPercentage(0m, 100m);
                decimal appleChance = CustomLeafPhysics.GetPercentage(0m, 100m);

                bool dropping = false;

                if (saplingChance <= 5m)
                {
                    // Begin sapling fall
                    p.level.UpdateBlock(p, x, y, z, Block.Sapling);
                    dropping = true;
                }

                if (appleChance <= 0.5m)
                {
                    // TODO: Drop apples
                    // dropping = true;
                }

                if (!dropping)
                {
                    p.level.UpdateBlock(p, x, y, z, Block.Air);

                    if (Config.UseGoodlyEffects) Command.Find("Effect").Use(p, Config.BreakParticle + " " + x + " " + y + " " + z + " 0 0 0 false"); // If GoodlyEffects is enabled, show a break particle
                }
            }

            else SaveBlock(p, block, 1);

            if (block != Block.Leaves)
            {
                p.level.UpdateBlock(p, x, y, z, Block.Air);

                if (Config.UseGoodlyEffects) Command.Find("Effect").Use(p, Config.BreakParticle + " " + x + " " + y + " " + z + " 0 0 0 false"); // If GoodlyEffects is enabled, show a break particle
            }

            p.Extras["HOLDING_TIME"] = 0;
            p.Extras["MINING_COORDS"] = 0;

            if (Config.UseGoodlyEffects)
            {
                // Despawn break particle
                p.Send(Packet.DefineEffect(200, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1000, 0, 0, false, false, false, false, false));
            }
        }

        public VenkBlock GetBlockByName(string name)
        {
            // Case-insensitive search for the block name
            KeyValuePair<string, VenkBlock> matchingBlock = blocks.FirstOrDefault(kv => kv.Key.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (matchingBlock.Equals(default(KeyValuePair<string, VenkBlock>)))
            {
                return null;
            }

            return matchingBlock.Value;
        }

        public Tool GetToolByName(string name)
        {
            // Case-insensitive search for the tool name
            KeyValuePair<string, Tool> matchingTool = tools.FirstOrDefault(kv => kv.Key.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (matchingTool.Equals(default(KeyValuePair<string, Tool>)))
            {
                return null;
            }

            return matchingTool.Value;
        }

        void HandleBlockClick(Player p, MouseButton button, MouseAction action, ushort yaw, ushort pitch, byte entity, ushort x, ushort y, ushort z, TargetBlockFace face)
        {
            if (!maplist.Contains(p.level.name)) return;

            string message = MessageBlock.Get(p.level.MapName, x, y, z);
            if (message != null) return; // Don't try and mine message blocks

            BlockID b = p.GetHeldBlock();
            string held = Block.GetName(p, b);

            if (action == MouseAction.Pressed)
            {
                if (held.ToLower() == "goldenapple")
                {
                    if (p.Extras.GetInt("GOLDEN_APPLES") == 0)
                    {
                        p.Message("%7You have no golden apples remaining.");
                        return;
                    }

                    else
                    {
                        int health = p.Extras.GetInt("SURVIVAL_HEALTH");
                        if (health <= Config.MaxHealth - 4) p.Extras["SURVIVAL_HEALTH"] = health + 4; // Give 4 health points
                        else p.Extras["SURVIVAL_HEALTH"] = Config.MaxHealth; // Set to max health if over (max - 4)

                        int hunger = p.Extras.GetInt("HUNGER");
                        if (hunger < 800) p.Extras["HUNGER"] = hunger + 200; // Give back 4 food points
                        else p.Extras["HUNGER"] = 1000; // Set to max if over 800

                        p.Extras["GOLDEN_APPLES"] = p.Extras.GetInt("GOLDEN_APPLES") - 1; // Subtract one golden apple
                        return;
                    }
                }

                if (held.ToLower() == "food")
                {
                    if (p.Extras.GetInt("FOOD") == 0)
                    {
                        p.Message("%7You have no food remaining.");
                        return;
                    }

                    else
                    {
                        int hunger = p.Extras.GetInt("HUNGER");
                        if (hunger < 700) p.Extras["HUNGER"] = hunger + 300; // Give back 6 food points
                        else p.Extras["HUNGER"] = 1000; // Set to max if over 700

                        p.Extras["FOOD"] = p.Extras.GetInt("FOOD") - 1; // Subtract one food
                        return;
                    }
                }
            }

            BlockID clickedBlock = p.level.GetBlock(x, y, z);
            string name = Block.GetName(p, clickedBlock);

            if (name.ToLower() == "unknown") return;

            if (button == MouseButton.Left)
            {
                if (!Config.Mining) return;
                if (!p.level.Config.MOTD.ToLower().Contains("mining=true")) return;

                if (p.invincible || p.Game.Referee)
                {
                    p.Message("%f╒ &c∩αΓ: &7You cannot modify blocks as a spectator.");
                    return;
                }

                if (name.StartsWith("Chest-")) return;

                if (p.Extras.GetInt("HOLDING_TIME") == 0)
                {
                    p.Extras["MINING_COORDS"] = x + "_" + y + "_" + z;
                }

                float px = Convert.ToSingle(x), py = Convert.ToSingle(y), pz = Convert.ToSingle(z);

                // Offset particle in the center of the block

                px += 0.5f;
                pz += 0.5f;

                if (action == MouseAction.Pressed)
                {
                    string coords = p.Extras.GetString("MINING_COORDS");

                    // If the player starts mining a new block before the other one has finished, restart the mining process
                    if (coords != (x + "_" + y + "_" + z))
                    {
                        p.Extras["HOLDING_TIME"] = 0;
                        p.Extras["MINING"] = false;
                        p.Extras["MINING_COORDS"] = 0;

                        if (Config.UseGoodlyEffects)
                        {
                            // Despawn break particle
                            p.Send(Packet.DefineEffect(200, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1000, 0, 0, false, false, false, false, false));
                        }

                        return;
                    }

                    int heldFor = p.Extras.GetInt("HOLDING_TIME");

                    // The client's click speed is ~4 times/second
                    TimeSpan duration = TimeSpan.FromSeconds(heldFor / 4.0);

                    // Position particle towards respective block face

                    if (face == TargetBlockFace.AwayX) px += 0.5625f;
                    if (face == TargetBlockFace.AwayY) py += 0.5f;
                    if (face == TargetBlockFace.AwayZ) pz += 0.5625f;
                    if (face == TargetBlockFace.TowardsX) px -= 0.5625f;
                    if (face == TargetBlockFace.TowardsY) py -= 0.5f;
                    if (face == TargetBlockFace.TowardsZ) pz -= 0.5625f;

                    // If block isn't set up, just delete the block like normal and return

                    bool contains = blocks.Keys.Any(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (!contains)
                    {
                        MineBlock(p, x, y, z, clickedBlock, name);
                        return;
                    }

                    VenkBlock blockInfo = GetBlockByName(name);

                    if (blockInfo == null)
                    {
                        MineBlock(p, x, y, z, clickedBlock, name);
                        return;
                    }

                    // Placeholder properties used to calculate the breaking speed of the block

                    float blockHardness = blockInfo.Hardness;
                    float defaultSpeed = (blockHardness + 1f) / blockInfo.DefaultSpeed; // For some reason, times don't match. This is probably due to the difference in clicking speeds in both clients.
                    float toolMultiplier = 1f;

                    bool isBestTool = false;
                    bool canHarvest = true;

                    bool toolEfficiency = false; // TODO
                    int efficiencyLevel = 0; // TODO
                    bool hasteEffect = false; // TODO
                    int hasteLevel = 2; // TODO
                    bool miningFatigue = false; // TODO
                    int miningFatigueLevel = 0; // TODO
                    bool inWater = false; // TODO
                    bool hasAquaAffinity = false; // TODO
                    bool onGround = true; // TODO

                    if (held.ToLower().StartsWith("wood")) toolMultiplier = (blockHardness + 1f) / blockInfo.WoodenSpeed;
                    else if (held.ToLower().StartsWith("stone")) toolMultiplier = (blockHardness + 1f) / blockInfo.StoneSpeed;
                    else if (held.ToLower().StartsWith("iron")) toolMultiplier = (blockHardness + 1f) / blockInfo.IronSpeed;

                    // Check if player is holding a tool and if so, check if it is in their inventory

                    if (tools.ContainsKey(held) && HasBlock(p, b))
                    {
                        Tool toolInfo = GetToolByName(held);
                        if (toolInfo.Type == blockInfo.Tool) isBestTool = true; // Check if the tool is the correct type for this block
                    }

                    // TODO: Check if the player is in the air

                    // TODO: Check if they are in water

                    float breakingTime = CalculateBreakingTime(isBestTool, defaultSpeed, toolMultiplier, canHarvest, toolEfficiency, efficiencyLevel, hasteEffect, hasteLevel, miningFatigue, miningFatigueLevel, inWater, hasAquaAffinity, onGround, blockHardness);

                    breakingTime *= 1000; // s > ms

                    if (duration > TimeSpan.FromMilliseconds(breakingTime))
                    {
                        MineBlock(p, x, y, z, b, name);
                        p.Extras["HOLDING_TIME"] = 0;
                        p.Extras["MINING"] = false;
                        return;
                    }

                    p.Extras["HOLDING_TIME"] = heldFor + 1;

                    if (!p.Extras.GetBoolean("MINING"))
                    {
                        if (Config.UseGoodlyEffects)
                        {
                            // Spawn break particle
                            p.Send(Packet.DefineEffect(200, 0, 105, 15, 120, 255, 255, 255, 10, 1, 28, 0, 0, 0, 0, (breakingTime / 1000), 0, true, true, true, true, true));
                            p.Send(Packet.SpawnEffect(200, px, py, pz, px, py, pz));
                        }

                        p.Extras["MINING"] = true;
                    }
                }

                else if (action == MouseAction.Released)
                {
                    p.Extras["HOLDING_TIME"] = 0;
                    p.Extras["MINING"] = false;
                    p.Extras["MINING_COORDS"] = 0;

                    if (Config.UseGoodlyEffects)
                    {
                        // Despawn break particle
                        p.Send(Packet.DefineEffect(200, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1000, 0, 0, false, false, false, false, false));
                    }
                }
            }

            // Chests WIP

            /*else if (button == MouseButton.Right)
            {
                if (action == MouseAction.Released)
                {
                    if (name.StartsWith("Chest-"))
                    {
                        BlockID holding = p.GetHeldBlock();
                        AddChest(p, x, y, z, holding);
                    }
                }
            }

            else if (button == MouseButton.Middle)
            {
                if (action == MouseAction.Released)
                {
                    if (name.StartsWith("Chest-"))
                    {
                        OpenChest(p, x, y, z);
                    }
                }
            }*/
        }

        #endregion

        #region Chests

        void OpenChest(Player p, ushort x, ushort y, ushort z)
        {
            p.Message("Opened chest.");
        }

        void CloseChest(Player p, ushort x, ushort y, ushort z)
        {
            p.Message("Closed chest.");
        }

        void CreateChest(Player p, ushort x, ushort y, ushort z)
        {
            string coords = x + ";" + y + ";" + z;
            Database.AddRow("Chests", "Level, Coords, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7, Slot8, Slot9, Slot10, Slot11, Slot12, Slot13, Slot14," +
                "Slot15, Slot16, Slot17, Slot18, Slot19, Slot20, Slot21, Slot22, Slot23, Slot24, Slot25, Slot26, Slot27, Slot28, Slot29," +
                "Slot30, Slot31, Slot32, Slot33, Slot34, Slot35, Slot36", p.level.name, coords, "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0");

            p.Message("Created chest.");
        }

        void RemoveChest(Player p, ushort x, ushort y, ushort z)
        {
            string coords = x + ";" + y + ";" + z;
            List<string[]> rows = Database.GetRows("Chests", "*", "WHERE Level=\"" + p.level.name + "\" AND Coords=\"" + coords + "\"", "");
            if (rows.Count > 0) Database.Execute("DELETE FROM Chests WHERE Level =\"" + p.level.name + "\" AND Coords=\"" + coords + "\"", "");
            p.Message("Removed chest.");
        }

        void AddChest(Player p, ushort x, ushort y, ushort z, BlockID block)
        {
            p.Message("Added to chest.");
        }

        void TakeChest(Player p, ushort x, ushort y, ushort z)
        {
            p.Message("Taken from chest.");
        }

        #endregion

        #region PvP

        public static bool inSafeZone(Player p, string map)
        {
            if (File.Exists(Config.Path + "safezones" + map + ".txt"))
            {
                using (var r = new StreamReader(Config.Path + "safezones" + map + ".txt"))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                    {
                        string[] temp = line.Split(';');
                        string[] coord1 = temp[0].Split(',');
                        string[] coord2 = temp[1].Split(',');

                        if ((p.Pos.BlockX <= int.Parse(coord1[0]) && p.Pos.BlockX >= int.Parse(coord2[0])) || (p.Pos.BlockX >= int.Parse(coord1[0]) && p.Pos.BlockX <= int.Parse(coord2[0])))
                        {
                            if ((p.Pos.BlockZ <= int.Parse(coord1[2]) && p.Pos.BlockZ >= int.Parse(coord2[2])) || (p.Pos.BlockZ >= int.Parse(coord1[2]) && p.Pos.BlockZ <= int.Parse(coord2[2])))
                            {
                                if ((p.Pos.BlockY <= int.Parse(coord1[1]) && p.Pos.BlockY >= int.Parse(coord2[1])) || (p.Pos.BlockY >= int.Parse(coord1[1]) && p.Pos.BlockY <= int.Parse(coord2[1])))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
            }
            return false;
        }

        void HandlePlayerClick(Player p, MouseButton button, MouseAction action, ushort yaw, ushort pitch, byte entity, ushort x, ushort y, ushort z, TargetBlockFace face)
        {
            if (!maplist.Contains(p.level.name)) return;

            if (button == MouseButton.Left)
            {
                if (action != MouseAction.Released) return;

                Player victim = null; // If not null, the player that is being hit

                Player[] players = PlayerInfo.Online.Items;

                foreach (Player pl in players)
                {
                    // Clicked on a player

                    if (pl.EntityID == entity)
                    {
                        victim = pl;
                        break;
                    }
                }

                // If the player didn't click on anyone, start a cooldown

                if (victim == null)
                {
                    int ping = p.Session.Ping.AveragePing();
                    int penalty = 0;

                    if (ping == 0) penalty = 0; // "lagged-out"
                    if (ping > 0 && ping <= 29) penalty = 50; // "great"
                    if (ping > 29 && ping <= 59) penalty = 100; // "good"
                    if (ping > 59 && ping <= 119) penalty = 150; // "okay"
                    if (ping > 119 && ping <= 180) penalty = 200; // "bad"
                    if (ping > 180) penalty = 250; // "horrible"

                    p.Extras["PVP_HIT_COOLDOWN"] = DateTime.UtcNow.AddMilliseconds(550 - penalty);
                    return;
                }

                else
                {
                    // If the player doesn't have an active cooldown, hit the victim and set cooldown

                    if (!p.Extras.Contains("PVP_HIT_COOLDOWN"))
                    {
                        if (CanHitPlayer(p, victim))
                        {
                            DoHit(p, victim);

                            int ping = p.Session.Ping.AveragePing();
                            int penalty = 0;

                            if (ping == 0) penalty = 0; // "lagged-out"
                            if (ping > 0 && ping <= 29) penalty = 50; // "great"
                            if (ping > 29 && ping <= 59) penalty = 100; // "good"
                            if (ping > 59 && ping <= 119) penalty = 150; // "okay"
                            if (ping > 119 && ping <= 180) penalty = 200; // "bad"
                            if (ping > 180) penalty = 250; // "horrible"

                            p.Extras["PVP_HIT_COOLDOWN"] = DateTime.UtcNow.AddMilliseconds(400 - penalty);
                        }
                    }

                    else
                    {
                        DateTime lastClickTime = (DateTime)p.Extras.Get("PVP_HIT_COOLDOWN");

                        if (lastClickTime > DateTime.UtcNow) return;

                        if (CanHitPlayer(p, victim))
                        {
                            DoHit(p, victim);

                            int ping = p.Session.Ping.AveragePing();
                            int penalty = 0;

                            if (ping == 0) penalty = 0; // "lagged-out"
                            if (ping > 0 && ping <= 29) penalty = 50; // "great"
                            if (ping > 29 && ping <= 59) penalty = 100; // "good"
                            if (ping > 59 && ping <= 119) penalty = 150; // "okay"
                            if (ping > 119 && ping <= 180) penalty = 200; // "bad"
                            if (ping > 180) penalty = 250; // "horrible"

                            p.Extras["PVP_HIT_COOLDOWN"] = DateTime.UtcNow.AddMilliseconds(400 - penalty);
                        }
                    }
                }
            }
        }

        static bool CanHitPlayer(Player p, Player victim)
        {
            Vec3F32 delta = p.Pos.ToVec3F32() - victim.Pos.ToVec3F32();
            float reachSq = 12f; // 3.46410161514 block reach distance

            int ping = p.Session.Ping.AveragePing();

            if (ping > 59 && ping <= 119) reachSq = 16f; // "okay"
            if (ping > 119 && ping <= 180) reachSq = 16f; // "bad"
            if (ping > 180) reachSq = 16f; // "horrible"

            // Don't allow clicking on players further away than their reach distance
            if (delta.LengthSquared > (reachSq + 1)) return false;

            // Check if they can kill players, determined by gamemode plugins
            bool canKill = PvP.Config.GamemodeOnly == false ? true : p.Extras.GetBoolean("PVP_CAN_KILL");
            if (!canKill) return false;

            if (p.Game.Referee || victim.Game.Referee || p.invincible || victim.invincible) return false; // Ref or invincible
            if (inSafeZone(p, p.level.name) || inSafeZone(victim, victim.level.name)) return false; // Either player is in a safezone

            if (!string.IsNullOrWhiteSpace(p.Extras.GetString("TEAM")) && (p.Extras.GetString("TEAM") == victim.Extras.GetString("TEAM")))
            {
                return false; // Players are on the same team
            }

            BlockID b = p.GetHeldBlock();

            if (Block.GetName(p, b).ToLower() == "bow") return false; // Bow damage comes from arrows, not player click

            // If all checks are complete, return true to allow knockback and damage
            return true;
        }

        static void PushPlayer(Player p, Player victim)
        {
            if (p.level.Config.MOTD.ToLower().Contains("-damage")) return;

            int srcHeight = ModelInfo.CalcEyeHeight(p);
            int dstHeight = ModelInfo.CalcEyeHeight(victim);
            int dx = p.Pos.X - victim.Pos.X, dy = (p.Pos.Y + srcHeight) - (victim.Pos.Y + dstHeight), dz = p.Pos.Z - victim.Pos.Z;

            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            if (dir.Length > 0) dir = Vec3F32.Normalise(dir);

            float mult = 1 / ModelInfo.GetRawScale(victim.Model);
            float victimScale = ModelInfo.GetRawScale(victim.Model);

            if (victim.Supports(CpeExt.VelocityControl) && p.Supports(CpeExt.VelocityControl))
            {
                // Intensity of force is in part determined by model scale
                victim.Send(Packet.VelocityControl((-dir.X * mult) * 0.5f, 0.87f * mult, (-dir.Z * mult) * 0.5f, 0, 1, 0));

                // If GoodlyEffects is enabled, show particles whenever a player is hit
                if (Config.UseGoodlyEffects)
                {
                    // Spawn effect when victim is hit
                    Command.Find("Effect").Use(victim, Config.HitParticle + " " + (victim.Pos.X / 32) + " " + (victim.Pos.Y / 32) + " " + (victim.Pos.Z / 32) + " 0 0 0 true");
                }
            }
            else
            {
                p.Message("You can left and right click on players to hit them if you update your client!");
            }
        }

        void DoHit(Player p, Player victim)
        {
            PushPlayer(p, victim); // Knock the victim back

            // TODO: Weapons
            int damage = 1;

            BlockID block = p.GetHeldBlock();
            string held = Block.GetName(p, block);

            // If the player is holding a tool and that tool is in their inventory, set damage to the tool's damage
            if (tools.ContainsKey(held) && HasBlock(p, block))
            {
                Tool toolInfo = GetToolByName(held);
                if (toolInfo == null) return;

                damage = toolInfo.Damage;
            }

            if (!p.level.Config.MOTD.CaselessContains("-damage")) DoDamage(victim, damage, "pvp", p);
        }

        void HandleOnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
        {
            // Initialize extras. We're not using ResetPlayerState() here since we are resetting variables whenever the player
            // changes levels and we don't want to show health HUD if they join a non-survival world.

            p.Extras["SURVIVAL_HEALTH"] = 20;
            p.Extras["HUNGER"] = 1000;
            p.Extras["DROWNING"] = 20;

            // Clear HUD if player isn't in a survival world
            if (!maplist.Contains(level.name))
            {
                p.SendCpeMessage(CpeMessageType.BottomRight3, "");
                p.SendCpeMessage(CpeMessageType.BottomRight2, "");
                p.SendCpeMessage(CpeMessageType.BottomRight1, "");
                return;
            }

            p.SendCpeMessage(CpeMessageType.BottomRight1, GetHealthBar(20));

            // If player has the CEF plugin, add sound effects
            if (Config.FallDamage)
            {
                if (p.Session.ClientName().CaselessContains("cef"))
                {
                    p.Message("cef create -n fall -gasq https://youtu.be/uUkuYsl5JSY");
                    p.Message("cef create -n death -gasq https://youtu.be/D-wx2WQsmLU");
                    p.Message("cef create -n splash -gasq https://youtu.be/I-41y1WyGjI");
                    p.Message("cef create -n hit -gasq https://youtu.be/OLJbtULNOaM");
                }
            }

            if (Config.Mining)
            {
                if (Config.GamemodeOnly)
                {
                    // Additional support for allowing inventory saving between sessions
                    string[] prefixes = Config.AllowedMapPrefixes.Split(',');

                    bool match = false;

                    foreach (string prefix in prefixes)
                    {
                        if (p.level.name.StartsWith(prefix)) match = true;
                    }

                    if (!match)
                    {
                        // Delete inventories after round ends
                        List<string[]> pRows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);
                        if (pRows.Count > 0) Database.Execute("DELETE FROM Inventories4 WHERE Name=@0 AND Level=@1", p.truename, p.level.name);
                    }
                }

                if (p.level.Config.MOTD.ToLower().Contains("mining=true"))
                {
                    List<string[]> rows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);

                    if (rows.Count == 0)
                    {
                        // Make block menu (inventory) appear completely empty
                        for (int i = 0; i < 767; i++)
                        {
                            p.Send(Packet.SetInventoryOrder(Block.Air, (BlockID)i, p.Session.hasExtBlocks));
                        }
                    }

                    else
                    {
                        for (int i = 1; i <= 767; i++)
                        {
                            if (i <= 36)
                            {
                                if (rows[0][i].ToString().StartsWith("0"))
                                {
                                    //p.Message(i + " empty");
                                    p.Send(Packet.SetInventoryOrder(Block.Air, (BlockID)i, p.Session.hasExtBlocks));
                                    continue;
                                }

                                else
                                {
                                    // Need to trim raw code to get the ID of the block. The example below is for the ID 75:
                                    // ID of block = 75, amount of block = 22
                                    // b75(22) -> 75
                                    string raw = rows[0][i].ToString();

                                    int from = raw.IndexOf("b") + "b".Length;
                                    int to = raw.LastIndexOf("(");

                                    string id = raw.Substring(from, to - from);

                                    //p.Message((BlockID)Convert.ToUInt16(id) + " " + (BlockID)i);
                                    p.Send(Packet.SetInventoryOrder((BlockID)Convert.ToUInt16(id), (BlockID)i, p.Session.hasExtBlocks));
                                    continue;
                                }
                            }

                            else
                            {
                                //p.Message(i + " order");
                                p.Send(Packet.SetInventoryOrder(Block.Air, (BlockID)i, p.Session.hasExtBlocks));
                            }
                        }
                    }
                }
            }

            if (p.Supports(CpeExt.TextHotkey))
            {
                // Drop blocks hotkeys (del and backspace)
                p.Send(Packet.TextHotKey("DropBlocks", "/DropBlock◙", 211, 0, true));
                p.Send(Packet.TextHotKey("DropBlocks", "/DropBlock◙", 14, 0, true));
            }
        }

        #endregion

        #region Blocks

        bool hasBlock(string world, Player p, string block)
        {
            string filepath = Config.Path + "blocks/" + world + "/" + p.truename + ".txt";
            if (File.Exists(filepath))
            {
                using (var r = new StreamReader(filepath))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                    {
                        if (line == block) return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region Recipes

        public static string getRecipe(string item)
        {
            for (int i = 0; i < recipes.Length; i++)
            {
                if (recipes[i, 0] == null) continue;

                if (recipes[i, 0].Split(':')[0].ToLower() == item.ToLower())
                {
                    return recipes[i, 0] + " " + recipes[i, 1];
                }
            }
            return "0 0";
        }

        void loadRecipes()
        {
            if (File.Exists(Config.Path + "recipes.txt"))
            {
                using (var r = new StreamReader(Config.Path + "recipes.txt"))
                {
                    string line;

                    while ((line = r.ReadLine()) != null)
                    {
                        if (line.StartsWith("//")) continue;
                        string[] recipeStats = line.Split(';');

                        for (int i = 0; i < 255; i++)
                        {
                            if (recipes[i, 0] == null)
                            {
                                recipes[i, 0] = recipeStats[0];
                                recipes[i, 1] = recipeStats[1];
                                break;
                            }
                        }
                    }
                }
            }

            else File.Create(Config.Path + "recipes.txt").Dispose();
        }

        #endregion

        #region Potions

        public static void CheckInvisible(SchedulerTask task)
        {
            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (!pl.Extras.GetBoolean("POTION_IS_INVISIBLE")) continue;

                string time = pl.Extras.GetString("POTION_INV_TIMER");

                DateTime date1 = DateTime.Parse(time);
                DateTime date2 = date1.AddSeconds(10);

                if (DateTime.UtcNow > date2)
                {
                    pl.Extras["POTION_IS_INVISIBLE"] = false;

                    Entities.GlobalSpawn(pl, true);
                    Server.hidden.Remove(pl.truename);
                    pl.Message("The invisibility potion has worn off, you are now visible again.");
                }
            }
        }

        public static void CheckSpeed(SchedulerTask task)
        {
            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (!pl.Extras.GetBoolean("POTION_IS_FAST")) continue;
                if (pl.Extras.GetBoolean("POTION_IS_JUMP")) pl.Send(Packet.Motd(pl, pl.level.Config.MOTD.Replace("jumpheight=", "").Replace("horspeed=", "") + " jumpheight=2 horspeed=3"));
                else pl.Send(Packet.Motd(pl, pl.level.Config.MOTD.Replace("horspeed=", "") + " horspeed=3"));

                string time = pl.Extras.GetString("POTION_SPEED_TIMER");

                DateTime date1 = DateTime.Parse(time);
                DateTime date2 = date1.AddSeconds(30);

                if (DateTime.UtcNow > date2)
                {
                    pl.Extras["POTION_IS_FAST"] = false;
                    pl.SendMapMotd();
                    pl.Message("The speed potion has worn off, you are now at normal speed again.");
                }
            }
        }

        public static void CheckJump(SchedulerTask task)
        {
            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (!pl.Extras.GetBoolean("POTION_IS_JUMP")) continue;
                if (pl.Extras.GetBoolean("POTION_IS_FAST")) pl.Send(Packet.Motd(pl, pl.level.Config.MOTD.Replace("jumpheight=", "").Replace("horspeed=", "") + " jumpheight=2 horspeed=3"));
                else pl.Send(Packet.Motd(pl, pl.level.Config.MOTD.Replace("jumpheight=", "") + " jumpheight=2"));

                string time = pl.Extras.GetString("POTION_JUMP_TIMER");

                DateTime date1 = DateTime.Parse(time);
                DateTime date2 = date1.AddSeconds(30);

                if (DateTime.UtcNow > date2)
                {
                    pl.Extras["POTION_IS_JUMP"] = false;
                    pl.SendMapMotd();
                    pl.Message("The jump potion has worn off, you are now at normal jump height again.");
                }
            }
        }
        #endregion
    }

    public delegate void OnPlayerKilledByPlayer(Player p, Player killer);

    /// <summary> Called whenever a player is killed by another player. </summary>
    public sealed class OnPlayerKilledByPlayerEvent : IEvent<OnPlayerKilledByPlayer>
    {
        public static void Call(Player p, Player killer)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p, killer));
        }
    }

    #region Physics

    #region Leaf physics

    public unsafe static class CustomLeafPhysics
    {
        public static decimal GetPercentage(decimal min, decimal max)
        {
            Random rand = new Random();
            return ((decimal)rand.Next((int)(min * 100.0M), (int)(max * 100.0M))) / 100.0M;
        }

        internal static void CheckNeighbours(Level lvl, ushort x, ushort y, ushort z)
        {
            CheckAt(lvl, (ushort)(x + 1), y, z);
            CheckAt(lvl, (ushort)(x - 1), y, z);
            CheckAt(lvl, x, y, (ushort)(z + 1));
            CheckAt(lvl, x, y, (ushort)(z - 1));
            CheckAt(lvl, x, (ushort)(y + 1), z);
            // NOTE: omission of y-1 to match original behaviour
        }

        // TODO: Stop checking block type and just always call lvl.AddCheck
        internal static void CheckAt(Level lvl, ushort x, ushort y, ushort z)
        {
            int index;
            BlockID block = lvl.GetBlock(x, y, z, out index);

            switch (block)
            {
                case Block.Sapling:
                case Block.Sand:
                case Block.Gravel:
                case Block.Log:
                case Block.Leaves:
                case Block.FloatWood:
                    lvl.AddCheck(index);
                    break;
                default:
                    block = Block.Convert(block);
                    if (block == Block.Water || block == Block.Lava || (block >= Block.Red && block <= Block.White))
                    {
                        lvl.AddCheck(index);
                    }
                    break;
            }
        }

        public static void DoLeaf(Level lvl, ref PhysInfo C)
        {
            // Decaying disabled? Then just remove from the physics list

            // Delay checking for leaf decay for a random amount of time
            if (C.Data.Data < 5)
            {
                Random rand = lvl.physRandom;
                if (rand.Next(10) == 0) C.Data.Data++;
                return;
            }

            // Perform actual leaf decay, then remove from physics list
            if (DoLeafDecay(lvl, ref C))
            {
                // Chance of dropping sapling and apples
                decimal saplingChance = GetPercentage(0m, 100m);
                decimal appleChance = GetPercentage(0m, 100m);

                bool dropping = false;

                if (saplingChance <= 5m)
                {
                    // Begin sapling fall
                    lvl.AddUpdate(C.Index, Block.Sapling, default(PhysicsArgs));
                    dropping = true;
                }

                if (appleChance <= 0.5m)
                {
                    // TODO: Drop apples
                    //dropping = true;
                }

                if (!dropping) lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));

                if (lvl.physics > 1) CheckNeighbours(lvl, C.X, C.Y, C.Z);
            }

            C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }

        // Radius of box around the given leaf block that is checked for logs
        const int range = 4;
        const int blocksPerAxis = range * 2 + 1;

        const int oneX = 1; // index + oneX = (X + 1, Y, Z)
        const int oneY = blocksPerAxis; // index + oneY = (X, Y + 1, Z)
        const int oneZ = blocksPerAxis * blocksPerAxis;

        static bool DoLeafDecay(Level lvl, ref PhysInfo C)
        {
            int* dists = stackalloc int[blocksPerAxis * blocksPerAxis * blocksPerAxis];
            ushort x = C.X, y = C.Y, z = C.Z;
            int idx = 0;

            // The general overview of this algorithm is that it finds all log blocks
            //  from (x - range, y - range, z - range) to (x + range, y + range, z + range),
            //  and then tries to find a path from any of those logs to the block at (x, y, z).
            // Note that these paths can only travel through leaf blocks

            for (int xx = -range; xx <= range; xx++)
                for (int yy = -range; yy <= range; yy++)
                    for (int zz = -range; zz <= range; zz++, idx++)
                    {
                        int index = lvl.PosToInt((ushort)(x + xx), (ushort)(y + yy), (ushort)(z + zz));
                        byte type = index < 0 ? Block.Air : lvl.blocks[index];

                        if (type == Block.Log || type == Block.Wood)
                            dists[idx] = 0;
                        else if (type == Block.Leaves)
                            dists[idx] = -2;
                        else
                            dists[idx] = -1;
                    }

            for (int dist = 1; dist <= range; dist++)
            {
                idx = 0;

                for (int xx = -range; xx <= range; xx++)
                    for (int yy = -range; yy <= range; yy++)
                        for (int zz = -range; zz <= range; zz++, idx++)
                        {
                            if (dists[idx] != dist - 1) continue;
                            // If this block is X-1 dist away from a log, potentially update neighbours as X blocks away from a log

                            if (xx > -range) PropagateDist(dists, dist, idx - oneX);
                            if (xx < range) PropagateDist(dists, dist, idx + oneX);

                            if (yy > -range) PropagateDist(dists, dist, idx - oneY);
                            if (yy < range) PropagateDist(dists, dist, idx + oneY);

                            if (zz > -range) PropagateDist(dists, dist, idx - oneZ);
                            if (zz < range) PropagateDist(dists, dist, idx + oneZ);
                        }
            }

            // Calculate index of (0, 0, 0)
            idx = range * oneX + range * oneY + range * oneZ;
            return dists[idx] < 0;
        }

        static void PropagateDist(int* dists, int dist, int index)
        {
            // Distances can only propagate through leaf blocks
            if (dists[index] == -2) dists[index] = dist;
        }

        public static void DoLog(Level lvl, ref PhysInfo C)
        {
            ushort x = C.X, y = C.Y, z = C.Z;

            for (int xx = -range; xx <= range; xx++)
                for (int yy = -range; yy <= range; yy++)
                    for (int zz = -range; zz <= range; zz++)
                    {
                        int index = lvl.PosToInt((ushort)(x + xx), (ushort)(y + yy), (ushort)(z + zz));
                        if (index < 0 || lvl.blocks[index] != Block.Leaves) continue;

                        lvl.AddCheck(index);
                    }
            C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }
    }

    #endregion

    #region Liquid physics

    public static class CustomLiquidPhysics
    {
        public static void DoFlood(Level lvl, ref PhysInfo C)
        {
            ushort x = C.X, y = C.Y, z = C.Z;

            BlockID block = C.Block;

            if (y < lvl.Height - 1)
            {
                CheckFallingBlocks(lvl, C.Index + lvl.Width * lvl.Length);
            }

            PhysWater(lvl, (ushort)(x + 1), y, z, block, C.Data, x, y, z);
            PhysWater(lvl, (ushort)(x - 1), y, z, block, C.Data, x, y, z);
            PhysWater(lvl, x, y, (ushort)(z + 1), block, C.Data, x, y, z);
            PhysWater(lvl, x, y, (ushort)(z - 1), block, C.Data, x, y, z);
            PhysWater(lvl, x, (ushort)(y - 1), z, block, C.Data, x, y, z);

            //if (!C.Data.HasWait) C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }

        public static void PhysWater(Level lvl, ushort x, ushort y, ushort z, BlockID b, PhysicsArgs args, ushort curX, ushort curY, ushort curZ)
        {
            int curIndex;
            BlockID current = lvl.GetBlock(curX, curY, curZ, out curIndex);

            int index;
            BlockID block = lvl.GetBlock(x, y, z, out index);

            switch (block)
            {
                case Block.FastLava:
                case Block.Deadly_ActiveLava:
                case Block.Lava:
                case Block.StillLava:
                    lvl.Blockchange(Player.Console, curX, curY, curZ, Block.Stone);
                    args.Value2 = 1;
                    break;

                //case 102 + 256:
                case Block.Air:
                    byte spread = args.Value1;
                    byte hit = args.Value2;

                    if (hit == 1) break;
                    if (spread > 7) break;

                    args = default(PhysicsArgs);
                    args.Value1 = (byte)(spread + 1);
                    args.ExtBlock = 1;
                    lvl.AddUpdate(index, b, args);
                    break;

                default:
                    // Don't do anything if block is not air or liquid
                    break;
            }
        }

        static void CheckFallingBlocks(Level lvl, int b)
        {
            switch (lvl.blocks[b])
            {
                case Block.Sand:
                case Block.Gravel:
                case Block.FloatWood:
                    lvl.AddCheck(b); break;
                default:
                    break;
            }
        }
    }

    #endregion

    #region Sapling physics

    public static class CustomSaplingPhysics
    {
        public static void DoSapling(Level lvl, ref PhysInfo C)
        {
            ushort x = C.X, y = C.Y, z = C.Z;
            int index = C.Index;
            bool movedDown = false;
            ushort yCur = y;

            Random rand = lvl.physRandom;

            do
            {
                index = lvl.IntOffset(index, 0, -1, 0); yCur--; // Get block below each loop
                BlockID cur = lvl.GetBlock(x, yCur, z);
                if (cur == Block.Invalid) break;
                bool hitBlock = false;

                switch (cur)
                {
                    case Block.Air:
                        movedDown = true;
                        break;
                    default:
                        hitBlock = true;
                        break;
                }
                if (hitBlock || lvl.physics > 1) break;
            } while (true);

            if (movedDown)
            {
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
                if (lvl.physics > 1)
                    lvl.AddUpdate(index, C.Block);

                else
                    lvl.AddUpdate(lvl.IntOffset(index, 0, -1, 0), C.Block);
            }

            BlockID ground = lvl.GetBlock(x, (ushort)(y - 1), z);

            if (ground != Block.Grass && ground != Block.Dirt) return; // Only apply physics if block is on soil

            if (C.Data.Data < 20)
            {
                if (rand.Next(20) == 0) C.Data.Data++;
                return;
            }

            if (C.Data.Data == 20)
            {
                lvl.SetTile(x, y, z, Block.Air);
                Tree tree = Tree.Find(lvl.Config.TreeType);
                if (tree == null) tree = new NormalTree();

                tree.SetData(rand, tree.DefaultSize(rand));
                tree.Generate(x, y, z, (xT, yT, zT, bT) =>
                {
                    if (!lvl.IsAirAt(xT, yT, zT)) return;
                    lvl.Blockchange(xT, yT, zT, (ushort)bT);
                });
            }

            C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }
    }

    #endregion

    #region Sugar cane physics

    public static class SugarCanePhysics
    {
        public static void DoSugarCane(Level lvl, ref PhysInfo C)
        {
            ushort x = C.X, y = C.Y, z = C.Z;

            Random rand = lvl.physRandom;

            BlockID ground = lvl.GetBlock(x, (ushort)(y - 1), z);

            if (ground != Block.Grass && ground != Block.Dirt && ground != Block.Sand) return; // Only apply physics to the root block

            // Water blocks surrounding the ground block
            BlockID g1 = lvl.GetBlock((ushort)(x + 1), (ushort)(y - 1), z);
            BlockID g2 = lvl.GetBlock((ushort)(x - 1), (ushort)(y - 1), z);
            BlockID g3 = lvl.GetBlock(x, (ushort)(y - 1), (ushort)(z + 1));
            BlockID g4 = lvl.GetBlock(x, (ushort)(y - 1), (ushort)(z - 1));

            // Sugar cane blocks above root block
            BlockID a1 = lvl.GetBlock(x, (ushort)(y + 1), z);
            BlockID a2 = lvl.GetBlock(x, (ushort)(y + 2), z);

            // Don't grow if ground is not watered
            if (!(g1 == Block.Water || g1 == Block.StillWater || g1 == Block.FromRaw((ushort)PvP.Config.CustomWaterBlock))
            && !(g2 == Block.Water || g2 == Block.StillWater || g2 == Block.FromRaw((ushort)PvP.Config.CustomWaterBlock))
            && !(g3 == Block.Water || g3 == Block.StillWater || g3 == Block.FromRaw((ushort)PvP.Config.CustomWaterBlock))
            && !(g4 == Block.Water || g4 == Block.StillWater || g4 == Block.FromRaw((ushort)PvP.Config.CustomWaterBlock))) return;

            int height = 1;

            if (a1 == Block.FromRaw(83) || a2 == Block.FromRaw(83)) height++;

            // Height stops at 3 blocks
            if (height < 3)
            {
                if (rand.Next(25) == 0)
                {
                    lvl.Blockchange(Player.Console, x, (ushort)(y + height), z, Block.FromRaw(83));
                }
                return;
            }

            C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }
    }

    #endregion

    #endregion

    public class DropItem
    {
        public string Name { get { return "DropItem"; } }

        public class DropItemData
        {
            public BlockID block;
            public Vec3F32 pos, vel;
            public Vec3U16 last, next;
            public Vec3F32 drag;
            public float gravity;
        }

        static Vec3U16 Round(Vec3F32 v)
        {
            unchecked { return new Vec3U16((ushort)Math.Round(v.X), (ushort)Math.Round(v.Y), (ushort)Math.Round(v.Z)); }
        }

        public static DropItemData MakeArgs(Player p, Vec3F32 dir, BlockID block)
        {
            DropItemData args = new DropItemData();

            args.drag = new Vec3F32(0.9f, 0.9f, 0.9f);
            args.gravity = 0.08f;

            args.pos = new Vec3F32(p.Pos.X / 32, p.Pos.Y / 32, p.Pos.Z / 32);
            args.last = Round(args.pos);
            args.next = Round(args.pos);
            args.vel = new Vec3F32(dir.X * 0.9f, dir.Y * 0.9f, dir.Z * 0.9f);
            return args;
        }

        public static void UpdateNext(Player p, DropItemData data)
        {
            string name = p.Extras.GetString("DROPPING_ITEM");
            p.Message(name);
            PlayerBot bot = Matcher.FindBots(p, name);
            bot.Pos = new Position(data.next.X * 32, data.next.Y * 32, data.next.Z * 32);
            //p.level.BroadcastChange(data.next.X, data.next.Y, data.next.Z, data.block);
        }

        public static void OnHitBlock(Player p, DropItemData args, Vec3U16 pos, BlockID block)
        {
            string name = p.Extras.GetString("DROPPING_ITEM");
            p.Message(name);
            PlayerBot bot = Matcher.FindBots(p, name);
            bot.Pos = new Position(pos.X * 32, (pos.Y * 32) + 84, pos.Z * 32);
        }

        public static void DropItemCallback(SchedulerTask task)
        {
            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (pl.Extras.GetString("DROPPING_ITEM") == "none") continue;
                DropItemData data = (DropItemData)task.State;
                if (TickDropItem(pl, data)) return;

                // Done
                task.Repeating = false;
                pl.Extras["DROPPING_ITEM"] = "none";
            }
        }

        static bool TickDropItem(Player p, DropItemData data)
        {
            Vec3U16 pos = data.next;
            BlockID cur = p.level.GetBlock(pos.X, pos.Y, pos.Z);

            // Hit a block
            if (cur == Block.Invalid) return false;
            if (cur != Block.Air) { OnHitBlock(p, data, pos, cur); return false; }

            // Apply physics
            data.pos += data.vel;
            data.vel.X *= data.drag.X; data.vel.Y *= data.drag.Y; data.vel.Z *= data.drag.Z;
            data.vel.Y -= data.gravity;

            data.next = Round(data.pos);
            if (data.last == data.next) return true;

            // Moved a block, update in world
            UpdateNext(p, data);
            data.last = data.next;
            return true;
        }
    }

    #region Commands

    public sealed class CmdCraft : Command2
    {
        public override string name { get { return "Craft"; } }
        public override string type { get { return "Other"; } }
        public override bool museumUsable { get { return false; } }

        public static bool HasIngredients(Player p, string[] ingredientList, int amount)
        {
            List<string[]> pRows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);

            foreach (string ingredient in ingredientList)
            {
                string ingredientName = ingredient.Split(':')[0];
                int ingredientAmount = (int.Parse(ingredient.Split(':')[1])) * amount;

                if (pRows.Count == 0) return false;

                else
                {
                    ingredientName = PvP.ReplaceSuffixes(ingredientName); // Remove suffixes such as -N, -U etc

                    BlockID block;
                    if (!CommandParser.GetBlock(p, ingredientName, out block)) return false;

                    int column = PvP.FindActiveSlot(pRows[0], PvP.GetID(block));

                    if (column == 0) return false;

                    // Check how many of this material the player has
                    int number = Int32.Parse(pRows[0][column].ToString().Replace(PvP.GetID(block), "").Replace("(", "").Replace(")", ""));

                    // If player has sufficient materials, update inventory
                    if (number >= ingredientAmount)
                    {
                        int newCount = number - ingredientAmount;

                        if (newCount == 0)
                        {
                            Database.UpdateRows("Inventories4", "Slot" + column.ToString() + "=@1", "WHERE Name=@0 AND Level=@2", p.truename, "0", p.level.name);
                            p.Send(Packet.SetInventoryOrder(Block.Air, (BlockID)column, p.Session.hasExtBlocks));
                        }

                        else
                        {
                            PvP.UpdateBlockList(p, column);
                            Database.UpdateRows("Inventories4", "Slot" + column.ToString() + "=@1", "WHERE Name=@0 AND Level=@2", p.truename, PvP.GetID(block) + "(" + newCount.ToString() + ")", p.level.name);
                        }

                        p.Message("%c-" + ingredientAmount + " %7" + ingredientName);
                        return true;
                    }

                    else return false;
                }
            }

            return false;
        }

        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces();

            if (message.Length == 0)
            {
                p.Message("You need to specify an item name. E.g, 'stick'.");
                return;
            }

            HandleMake(p, args);
        }

        void HandleMake(Player p, string[] args)
        {
            int amount = args.Length > 1 ? int.Parse(args[1]) : 1; // Only craft 1 unless the player specified not to

            string[] recipe = PvP.getRecipe(args[0]).Split(' ');

            if (recipe[0] == "0 0")
            {
                p.Message("%SInvalid item. See %b/Recipes %Sfor more information.");
                return;
            }

            string item = recipe[0].Split(':')[0];
            int amountProduced = (int.Parse(recipe[0].Split(':')[1])) * amount;

            string[] ingredientList = recipe[1].Split(','); // ingredient1:amount,ingredient2:amount,ingredient3:amount

            if (HasIngredients(p, ingredientList, amount))
            {
                item = PvP.ReplaceSuffixes(item); // Remove suffixes such as -N, -U etc
                BlockID block;
                if (!CommandParser.GetBlock(p, item, out block)) return;

                PvP.SaveBlock(p, block, amountProduced);
                p.Message("%a+" + amountProduced + " %7" + Block.GetName(p, block));
            }

            else
            {
                p.Message("%SYou do not have the materials required to craft this item.");
            }
        }

        public override void Help(Player p)
        {
            p.Message("%T/Craft [item] <amount> %H- Crafts [amount] of [item].");
            p.Message("%T<amount> defaults to 1.");
            p.Message("%HFor a list of recipes, type %b/Recipes%H.");
        }
    }

    public sealed class CmdInventory : Command2
    {
        public override string name { get { return "Inventory"; } }
        public override string shortcut { get { return "backpack"; } }
        public override string type { get { return "Other"; } }

        public override void Use(Player p, string message, CommandData data)
        {

            string[] args = message.SplitSpaces();

            if (args.Length == 0)
            {
                List<string[]> pRows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);

                if (pRows.Count == 0)
                {
                    p.Message("Your inventory is empty.");
                    return;
                }

                else
                {
                    List<string> slots = new List<string>();

                    for (int i = 1; i < 37; i++)
                    {
                        if (pRows[0][i].ToString().StartsWith("0")) continue; // Don't bother with air
                        slots.Add("%2[" + i + "%2] " + pRows[0][i].ToString());
                    }

                    string inventory = String.Join(" %8| ", slots.ToArray());

                    p.Message(inventory);
                }
            }

            else
            {
                if (args[0].ToLower() == "clear")
                {
                    List<string[]> rows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);
                    if (rows.Count > 0) Database.Execute("DELETE FROM Inventories4 WHERE Name=@0 AND Level=@1", p.truename, p.level.name);

                    p.Message("Inventory cleared.");
                }
            }
        }

        public override void Help(Player p)
        {

        }
    }

    public sealed class CmdPvP : Command2
    {
        public override string name { get { return "PvP"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandPerm[] ExtraPerms { get { return new[] { new CommandPerm(LevelPermission.Admin, "can manage PvP") }; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0)
            {
                Help(p);
                return;
            }
            string[] args = message.SplitSpaces();

            switch (args[0].ToLower())
            {
                case "add":
                    HandleAdd(p, args, data);
                    return;
                case "del":
                    HandleDelete(p, args, data);
                    return;
            }
        }

        void HandleAdd(Player p, string[] args, CommandData data)
        {
            if (args.Length == 1)
            {
                p.Message("You need to specify a map to add.");
                return;
            }

            if (!HasExtraPerm(p, data.Rank, 1)) { p.Message("%cNo permission."); return; };

            string pvpMap = args[1];

            PvP.maplist.Add(pvpMap);
            p.Message("The map %b" + pvpMap + " %Shas been added to the PvP map list.");

            // Add the map to the map list
            using (StreamWriter maplistwriter =
                new StreamWriter(PvP.Config.Path + "maps.txt"))
            {
                foreach (String s in PvP.maplist)
                {
                    maplistwriter.WriteLine(s);
                }
            }

            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (pl.level.name.ToLower() == args[1].ToLower())
                {
                    PvP.ResetPlayerState(pl);
                }
            }
        }

        void HandleDelete(Player p, string[] args, CommandData data)
        {
            if (args.Length == 1)
            {
                p.Message("You need to specify a map to remove.");
                return;
            }

            if (!HasExtraPerm(p, data.Rank, 1)) return;

            string pvpMap = args[1];

            PvP.maplist.Remove(pvpMap);
            p.Message("The map %b" + pvpMap + " %Shas been removed from the PvP map list.");
        }

        public override void Help(Player p)
        {
            p.Message("%T/PvP add <map> %H- Adds a map to the PvP map list.");
            p.Message("%T/PvP del <map> %H- Deletes a map from the PvP map list.");
        }
    }

    public sealed class CmdRecipes : Command2
    {
        public override string name { get { return "Recipes"; } }
        public override string type { get { return "Other"; } }
        public override bool museumUsable { get { return false; } }

        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces();

            if (message.Length == 0)
            {
                HandleList(p);
                return;
            }

            if (args[0].CaselessEq("all"))
            {
                HandleAll(p);
                return;
            }

            Help(p);
        }

        void HandleAll(Player p)
        {
            p.Message("%7All items:");
            p.Message("%7syntax: %3[item]:[# produced] %2[ingredients]:[# required]");

            for (int i = 0; i < PvP.recipes.GetLength(0); i++)
            {
                if (PvP.recipes[i, 0] == null) return;

                p.Message("%7- %b" + PvP.recipes[i, 0] + " %a" + PvP.recipes[i, 1]);
            }
        }

        public static bool GetBlock(Player p, string input, out BlockID block)
        {
            block = Block.Parse(p, input);
            return block != Block.Invalid;
        }

        void HandleList(Player p)
        {
            // Check what materials the player has
            List<string[]> pRows = Database.GetRows("Inventories4", "*", "WHERE Name=@0 AND Level=@1", p.truename, p.level.name);

            p.Message("%7Craftable items:");
            p.Message("%7syntax: %3[item]:[# produced] %2[ingredients]:[# required]");

            for (int i = 0; i < PvP.recipes.GetLength(0); i++)
            {
                string recipe = PvP.recipes[i, 0];
                if (recipe == null) continue;

                string[] ingredientList = PvP.recipes[i, 1].Split(','); // ingredient1:amount,ingredient2:amount,ingredient3:amount

                bool hasAllIngredients = true; // Assume player has all ingredients initially

                foreach (string ingredient in ingredientList)
                {
                    string ingredientName = ingredient.Split(':')[0];
                    int ingredientAmount = (int.Parse(ingredient.Split(':')[1]));

                    if (pRows.Count > 0)
                    {
                        ingredientName = PvP.ReplaceSuffixes(ingredientName); // Remove suffixes such as -N, -U etc
                        BlockID block;

                        if (!GetBlock(p, ingredientName, out block))
                        {
                            hasAllIngredients = false;
                            break;
                        }

                        int column = PvP.FindActiveSlot(pRows[0], PvP.GetID(block));

                        if (column == 0)
                        {
                            hasAllIngredients = false;
                            break;
                        }

                        // Check how many of this material the player has
                        int number = Int32.Parse(pRows[0][column].ToString().Replace(PvP.GetID(block), "").Replace("(", "").Replace(")", ""));

                        // If player doesn't have sufficient materials, set the flag to false and break
                        if (number < ingredientAmount)
                        {
                            hasAllIngredients = false;
                            break;
                        }
                    }
                }

                if (hasAllIngredients)
                {
                    p.Message("%7- %b" + recipe + " %a" + PvP.recipes[i, 1]);
                }
            }
        }

        public override void Help(Player p)
        {
            p.Message("%T/Recipes %H- Lists all items that you can currently craft.");
            p.Message("%T/Recipes all %H- Lists all possible items that can be crafted.");
        }
    }

    public sealed class CmdSafeZone : Command2
    {
        public override string name { get { return "SafeZone"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandPerm[] ExtraPerms { get { return new[] { new CommandPerm(LevelPermission.Admin, "can manage safe zones") }; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0)
            {
                Help(p);
                return;
            }
            string[] args = message.SplitSpaces(2);

            switch (args[0].ToLower())
            {
                case "add":
                    HandleAdd(p, args, data);
                    return;
            }
        }

        void HandleAdd(Player p, string[] args, CommandData data)
        {
            p.Message("Place or break two blocks to determine the edges.");
            p.MakeSelection(2, null, addSafeZone);
        }

        bool addSafeZone(Player p, Vec3S32[] marks, object state, BlockID block)
        {
            FileInfo filedir = new FileInfo(PvP.Config.Path + "safezones" + p.level.name + ".txt");
            filedir.Directory.Create();

            using (StreamWriter file = new StreamWriter(PvP.Config.Path + "safezones" + p.level.name + ".txt", true))
            {
                file.WriteLine(marks.GetValue(0) + ";" + marks.GetValue(1));
            }

            p.Message("Successfully added a safezone.");
            return true;
        }

        public override void Help(Player p)
        {
            p.Message("%T/SafeZone [add] %H- Adds a safe zone to the current PvP map.");
            //p.Message("%T/SafeZone [del] %H- Removes a safe zone from the current PvP map.");
        }
    }

    public sealed class CmdTool : Command2
    {
        public override string name { get { return "Tool"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandPerm[] ExtraPerms { get { return new[] { new CommandPerm(LevelPermission.Admin, "can manage tools") }; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0)
            {
                Help(p);
                return;
            }
            string[] args = message.SplitSpaces(5);

            switch (args[0].ToLower())
            {
                case "add":
                    HandleAdd(p, args);
                    return;
                case "give":
                    HandleGive(p, args);
                    return;
                case "take":
                    HandleTake(p, args);
                    return;
            }
        }

        void HandleAdd(Player p, string[] args)
        {
            if (args.Length == 1)
            {
                p.Message("You need to specify the name of the tool. E.g, 'StonePickaxe' (block with this name must exist).");
                return;
            }

            BlockID block;
            if (!CommandParser.GetBlock(p, args[1], out block)) return; // Ensure block exists

            if (args.Length == 2)
            {
                p.Message("You need to specify the speed that the weapon mines at. E.g, '1' for normal speed, '2' for 2x speed.");
                return;
            }

            if (args.Length == 3)
            {
                p.Message("You need to specify how many clicks before it breaks. 0 for infinite clicks.");
                return;
            }

            if (args.Length == 4)
            {
                p.Message("%H[type] can be either 0 for none, 1 for axe, 2 for pickaxe, 3 for sword or 4 for shovel.");
                return;
            }

            // TODO: Add tool
        }

        void createTool(string id, string damage, string durability, string type)
        {
            FileInfo filedir = new FileInfo(PvP.Config.Path + "tools.txt");
            filedir.Directory.Create();

            using (StreamWriter file = new StreamWriter(PvP.Config.Path + "tools.txt", true))
            {
                file.WriteLine(id + ";" + damage + ";" + durability + ";" + type);
            }
        }

        void HandleGive(Player p, string[] args)
        {
            if (args.Length == 1)
            {
                p.Message("You need to specify a username to give the tool to.");
                return;
            }
            if (args.Length == 2)
            {
                p.Message("You need to specify the world to allow them to use the tool on.");
                return;
            }
            if (args.Length == 3)
            {
                p.Message("You need to specify the name of the tool.");
                return;
            }

            string filepath = PvP.Config.Path + "tools/" + args[2] + "/" + args[1] + ".txt";
            FileInfo filedir = new FileInfo(filepath);
            filedir.Directory.Create();
            using (StreamWriter file = new StreamWriter(filepath, true))
            {
                file.WriteLine(args[3]);
            }
        }

        void HandleTake(Player p, string[] args)
        {
            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (pl.truename == args[1])
                {
                    string filepath = PvP.Config.Path + "tools/" + args[2] + "/" + args[1] + ".txt";

                    if (File.Exists(filepath))
                    {
                        File.WriteAllText(filepath, string.Empty);
                    }
                }
            }
        }

        public override void Help(Player p)
        {
            p.Message("%T/Tool add [id] [speed] [durability] [type] %H- Adds an tool to the current PvP map.");
            p.Message("%T/Tool del %H- Removes an tool from current PvP map.");
            p.Message("%T/Tool give [player] [world] [tool] %H- Gives a player an tool.");
            p.Message("%T/Tool take [player] [world] %H- Takes all tools from a player away.");
        }
    }

    public sealed class CmdBlock : Command2
    {
        public override string name { get { return "Block"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandPerm[] ExtraPerms { get { return new[] { new CommandPerm(LevelPermission.Admin, "can manage blocks") }; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0)
            {
                Help(p);
                return;
            }
            string[] args = message.SplitSpaces(5);

            switch (args[0].ToLower())
            {
                case "add":
                    HandleAdd(p, args);
                    return;
            }
        }

        void HandleAdd(Player p, string[] args)
        {
            if (args.Length == 1)
            {
                p.Message("You need to specify an ID for the block. E.g, '1' for stone.");
                return;
            }

            if (args.Length == 2)
            {
                p.Message("You need to specify the tool that makes mining faster. %T[tool] can be either 0 for none, 1 for axe, 2 for pickaxe, 3 for sword or 4 for shovel.");
                return;
            }

            if (args.Length == 3)
            {
                p.Message("You need to specify how many clicks before it breaks. 0 for infinite clicks.");
                return;
            }

            // TODO: Add blocks
        }

        void createBlock(string id, string tool, string durability)
        {
            FileInfo filedir = new FileInfo(PvP.Config.Path + "blocks.txt");
            filedir.Directory.Create();

            using (StreamWriter file = new StreamWriter(PvP.Config.Path + "blocks.txt", true))
            {
                file.WriteLine(id + ";" + tool + ";" + durability);
            }
        }

        public override void Help(Player p)
        {
            p.Message("%T/Block add [id] [tool] [durability] %H- Adds a block to the current PvP map.");
            p.Message("%H[tool] can be either 0 for none, 1 for axe, 2 for pickaxe, 3 for sword or 4 for shovel.");
        }
    }

    public sealed class CmdPotion : Command2
    {
        public override string name { get { return "Potion"; } }
        public override bool SuperUseable { get { return false; } }
        public override string type { get { return "fun"; } }

        public override void Use(Player p, string message, CommandData data)
        {
            p.lastCMD = "Secret";

            string[] args = message.SplitSpaces(3);

            List<string[]> rows = Database.GetRows("Potions", "*", "WHERE Name=@0", p.truename);

            if (args[0].Length == 0)
            {
                p.Message("You need to specify a potion to use.");
                p.Message("%T/Potion health %b- Sets your health to full.");
                p.Message("%T/Potion speed %b- Gives you a 3x speed boost for 30 seconds.");
                p.Message("%T/Potion jump %b- Gives you a 3x jump boost for 30 seconds.");
                p.Message("%T/Potion invisible %b- Makes you invisible for 10 seconds.");
            }
            else
            {
                List<string[]> pRows = Database.GetRows("Potions", "*", "WHERE Name=@0", p.truename);
                if (args[0] == PvP.Config.SecretCode && args.Length >= 3)
                { // Used for getting potions
                    string item = args[1].ToLower();
                    int quantity = Int32.Parse(args[2]);

                    if (pRows.Count == 0)
                    {
                        if (item == "health") Database.AddRow("Potions", "Name, Health, Speed, Invisible, Jump, Waterbreathing, Strength, Slowness, Blindness", p.truename, quantity, 0, 0, 0, 0, 0, 0, 0);
                        if (item == "speed") Database.AddRow("Potions", "Name, Health, Speed, Invisible, Jump, Waterbreathing, Strength, Slowness, Blindness", p.truename, 0, quantity, 0, 0, 0, 0, 0, 0);
                        if (item == "invisible") Database.AddRow("Potions", "Name, Health, Speed, Invisible, Jump, Waterbreathing, Strength, Slowness, Blindness", p.truename, 0, 0, quantity, 0, 0, 0, 0, 0);
                        if (item == "jump") Database.AddRow("Potions", "Name, Health, Speed, Invisible, Jump, Waterbreathing, Strength, Slowness, Blindness", p.truename, 0, 0, 0, quantity, 0, 0, 0, 0);

                        p.Message("You now have: %b" + quantity + " %S" + item + " potions");
                        return;
                    }
                    else
                    {
                        int h = int.Parse(pRows[0][1]);
                        int s = int.Parse(pRows[0][2]);
                        int i = int.Parse(pRows[0][3]);
                        int j = int.Parse(pRows[0][4]);

                        int newH = quantity + h;
                        int newS = quantity + s;
                        int newI = quantity + i;
                        int newJ = quantity + j;

                        if (item == "health")
                        {
                            Database.UpdateRows("Potions", "Health=@1", "WHERE NAME=@0", p.truename, newH);
                            p.Message("You now have: %b" + newH + " %S" + item + " potions");
                        }

                        if (item == "speed")
                        {
                            Database.UpdateRows("Potions", "Speed=@1", "WHERE NAME=@0", p.truename, newS);
                            p.Message("You now have: %b" + newS + " %S" + item + " potions");
                        }

                        if (item == "invisible")
                        {
                            Database.UpdateRows("Potions", "Invisible=@1", "WHERE NAME=@0", p.truename, newI);
                            p.Message("You now have: %b" + newI + " %S" + item + " potions");
                        }

                        if (item == "jump")
                        {
                            Database.UpdateRows("Potions", "Jump=@1", "WHERE NAME=@0", p.truename, newJ);
                            p.Message("You now have: %b" + newJ + " %S" + item + " potions");
                        }
                        return;
                    }
                }

                if (args[0] == "list")
                {
                    if (pRows.Count == 0)
                    {
                        p.Message("%SYou do not have any potions.");
                        return;
                    }
                    int h = int.Parse(rows[0][1]);
                    int s = int.Parse(rows[0][2]);
                    int i = int.Parse(rows[0][3]);
                    int j = int.Parse(rows[0][4]);

                    p.Message("%aYour potions:");
                    p.Message("%7Health %ex{0}%7, Speed %ex{1}%7, Invisible %ex{2}%7, Jump %ex{3}", h, s, i, j);
                }

                if (args[0] == "health")
                {
                    if (pRows.Count == 0)
                    {
                        p.Message("%SYou do not have any potions.");
                        return;
                    }

                    int h = int.Parse(rows[0][1]);

                    if (h == 0)
                    {
                        p.Message("You don't have any health potions.");
                        return;
                    }

                    // Use potion
                    Database.UpdateRows("Potions", "Health=@1", "WHERE NAME=@0", p.truename, h - 1);
                    p.Extras["SURVIVAL_HEALTH"] = PvP.Config.MaxHealth;
                    p.Message("Your health has been replenished.");
                    p.Message("You have " + (h - 1) + " health potions remaining.");
                }

                if (args[0] == "speed")
                {
                    if (pRows.Count == 0)
                    {
                        p.Message("%SYou do not have any potions.");
                        return;
                    }
                    int s = int.Parse(rows[0][2]);
                    if (s == 0)
                    {
                        p.Message("You don't have any speed potions.");
                        return;
                    }

                    // Use potion
                    Database.UpdateRows("Potions", "Speed=@1", "WHERE NAME=@0", p.truename, s - 1);
                    p.Extras["POTION_IS_FAST"] = true;
                    p.Extras["POTION_SPEED_TIMER"] = DateTime.UtcNow;
                    p.Message("You have " + (s - 1) + " speed potions remaining.");
                    Server.MainScheduler.QueueRepeat(PvP.CheckSpeed, null, TimeSpan.FromMilliseconds(10));
                }

                if (args[0] == "invisible")
                {
                    if (pRows.Count == 0)
                    {
                        p.Message("%SYou do not have any potions.");
                        return;
                    }
                    int i = int.Parse(rows[0][3]);
                    if (i == 0)
                    {
                        p.Message("You don't have any invisible potions.");
                        return;
                    }

                    // Use potion
                    Database.UpdateRows("Potions", "Invisible=@1", "WHERE NAME=@0", p.truename, i - 1);
                    p.Extras["POTION_IS_INVISIBLE"] = true;

                    Entities.GlobalDespawn(p, true); // Remove from tab list
                    Server.hidden.Add(p.truename);
                    p.Extras["POTION_INV_TIMER"] = DateTime.UtcNow;
                    p.Message("%aYou are now invisible.");
                    p.Message("You have " + (i - 1) + " invisible potions remaining.");
                    Server.MainScheduler.QueueRepeat(PvP.CheckInvisible, null, TimeSpan.FromSeconds(1));
                }

                if (args[0] == "jump")
                {
                    if (pRows.Count == 0)
                    {
                        p.Message("%SYou do not have any potions.");
                        return;
                    }
                    int j = int.Parse(rows[0][4]);
                    if (j == 0)
                    {
                        p.Message("You don't have any jump potions.");
                        return;
                    }

                    // Use potion
                    Database.UpdateRows("Potions", "Jump=@1", "WHERE NAME=@0", p.truename, j - 1);
                    p.Extras["POTION_IS_JUMP"] = true;
                    p.Extras["POTION_JUMP_TIMER"] = DateTime.UtcNow;
                    p.Message("You have " + (j - 1) + " jump potions remaining.");
                    Server.MainScheduler.QueueRepeat(PvP.CheckJump, null, TimeSpan.FromMilliseconds(10));
                }
            }
        }

        public override void Help(Player p) { }
    }

    public sealed class CmdDropBlock : Command2
    {
        public override string name { get { return "DropBlock"; } }
        public override string shortcut { get { return "db"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }

        private readonly Random _random = new Random();

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        void AddBot(Player p, string botName)
        {
            botName = botName.Replace(' ', '_');
            PlayerBot bot = new PlayerBot(botName, p.level);
            bot.Owner = p.truename;
            TryAddBot(p, bot);
        }

        void TryAddBot(Player p, PlayerBot bot)
        {
            if (BotExists(p.level, bot.name, null))
            {
                p.Message("A bot with that name already exists.");
                return;
            }
            if (p.level.Bots.Count >= Server.Config.MaxBotsPerLevel)
            {
                p.Message("Reached maximum number of bots allowed on this map.");
                return;
            }

            Position pos = new Position(p.Pos.X, p.Pos.Y + 32, p.Pos.Z);
            bot.SetInitialPos(pos);
            bot.SetYawPitch(p.Rot.RotY, 0);
            PlayerBot.Add(bot);
        }

        static bool BotExists(Level lvl, string name, PlayerBot skip)
        {
            PlayerBot[] bots = lvl.Bots.Items;
            foreach (PlayerBot bot in bots)
            {
                if (bot == skip) continue;
                if (bot.name.CaselessEq(name)) return true;
            }
            return false;
        }

        static string ParseModel(Player dst, Entity e, string model)
        {
            // Reset entity's model
            if (model.Length == 0)
            {
                e.ScaleX = 0;
                e.ScaleY = 0;
                e.ScaleZ = 0;
                return "humanoid";
            }

            model = model.ToLower();
            model = model.Replace(':', '|'); // Since users assume : is for scale instead of |.

            float max = ModelInfo.MaxScale(e, model);
            // Restrict player model scale, but bots can have unlimited model scale
            if (ModelInfo.GetRawScale(model) > max)
            {
                dst.Message("%WScale must be {0} or less for {1} model",
                    max, ModelInfo.GetRawModel(model));
                return null;
            }
            return model;
        }

        public override void Use(Player p, string message, CommandData data)
        {
            if (!PvP.maplist.Contains(p.level.name)) return;

            // Create the bot
            BlockID block = p.GetHeldBlock();
            string holding = Block.GetName(p, block);
            if (holding == "Air") return;
            string code = RandomNumber(1000, 1000000).ToString();
            AddBot(p, "block_" + code);
            PlayerBot bot = Matcher.FindBots(p, "block_" + code);
            p.Extras["DROPPING_ITEM"] = "block_" + code;

            bot.DisplayName = "";
            bot.GlobalDespawn();
            bot.GlobalSpawn();

            BotsFile.Save(p.level);

            // Convert blocks over ID 65
            int convertedBlock = block;
            if (convertedBlock >= 66) convertedBlock = block - 256; // Need to convert block if ID is over 66

            string model = ParseModel(p, bot, convertedBlock + "|0.5");
            if (model == null) return;
            bot.UpdateModel(model);
            bot.ClickedOnText = "/pickupblock " + code + " " + convertedBlock;
            if (!ScriptFile.Parse(p, bot, "spin")) return;
            BotsFile.Save(p.level);

            // Drop item physics
            Vec3F32 dir = DirUtils.GetDirVector(p.Rot.RotY, p.Rot.HeadX);
            DropItem.DropItemData itemData = DropItem.MakeArgs(p, dir, block);
            DropItem.UpdateNext(p, itemData);

            SchedulerTask task = new SchedulerTask(DropItem.DropItemCallback, itemData, TimeSpan.FromMilliseconds(50), true);
            p.CriticalTasks.Add(task);

            // Adjust inventory

            Command.Find("SilentHold").Use(p, "air");
            p.lastCMD = "Secret";
        }

        public override void Help(Player p)
        {
            p.Message("%T/DropBlock - %HDrops a block at your feet.");
        }
    }

    public sealed class CmdPickupBlock : Command2
    {
        public override string name { get { return "PickupBlock"; } }
        public override string shortcut { get { return "pickup"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, CommandData data)
        {
            // /PickupBlock [bot name] [block]
            if (!PvP.maplist.Contains(p.level.name)) return;
            if (message.Length == 0) return;
            string[] args = message.SplitSpaces(2);

            if (p.Supports(CpeExt.HeldBlock)) Command.Find("SilentHold").Use(p, args[1]);

            p.lastCMD = "Secret";
            PlayerBot bot = Matcher.FindBots(p, "block_" + args[0]);
            PlayerBot.Remove(bot);

            // TODO: Add blocks to inventory
        }

        public override void Help(Player p) { }
    }

    #endregion
}