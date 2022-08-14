using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using MCGalaxy;
using MCGalaxy.Maths;
using MCGalaxy.Network;
using MCGalaxy.Config;
using MCGalaxy.Commands;
using MCGalaxy.Tasks;
using MCGalaxy.Util;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;

namespace PluginGoodlyEffects 
{
	public sealed class GoodlyEffects : Plugin 
	{
		public override string name { get { return "GoodlyEffects"; } }
		public override string MCGalaxy_Version { get { return "1.9.2.9"; } }
		public override string creator { get { return "Goodly"; } }
		const float notAllowedBelowZero = 0;
		public class EffectConfig {
			
			//NOT defined in the config file. Filled in at runtime when loaded
			public byte ID;

			[ConfigByte("pixelU1", "Effect")]
			public byte pixelU1 = 1;
			[ConfigByte("pixelV1", "Effect")]
			public byte pixelV1 = 0;
			[ConfigByte("pixelU2", "Effect")]
			public byte pixelU2 = 10;
			[ConfigByte("pixelV2", "Effect")]
			public byte pixelV2 = 10;
			
			[ConfigByte("tintRed", "Effect")]
			public byte tintRed = 255;
			[ConfigByte("tintGreen", "Effect")]
			public byte tintGreen = 255;
			[ConfigByte("tintBlue", "Effect")]
			public byte tintBlue = 255;
			
			[ConfigByte("frameCount", "Effect")]
			public byte frameCount = 1;
			[ConfigByte("particleCount", "Effect")]
			public byte particleCount = 1;
			
			[ConfigFloat("pixelSize", "Effect", 8, 0, 127.5f)]
			public float pixelSize = 8;
			[ConfigFloat("sizeVariation", "Effect", 0.0f, notAllowedBelowZero)]
			public float sizeVariation;
			
			[ConfigFloat("spread", "Effect", 0.0f, notAllowedBelowZero)]
			public float spread;
			[ConfigFloat("speed", "Effect", 0.0f)]
			public float speed;
			[ConfigFloat("gravity", "Effect", 0.0f)]
			public float gravity;
			
			[ConfigFloat("baseLifetime", "Effect", 1.0f, notAllowedBelowZero)]
			public float baseLifetime = 1.0f;
			[ConfigFloat("lifetimeVariation", "Effect", 0.0f, notAllowedBelowZero)]
			public float lifetimeVariation;
			
			[ConfigBool("expireUponTouchingGround", "Effect", true)]
			public bool expireUponTouchingGround = true;
			[ConfigBool("collidesSolid", "Effect", true)]
			public bool collidesSolid = true;
			[ConfigBool("collidesLiquid", "Effect", true)]
			public bool collidesLiquid = true;
			[ConfigBool("collidesLeaves", "Effect", true)]
			public bool collidesLeaves = true;
			
			[ConfigBool("fullBright", "Effect", true)]
			public bool fullBright = true;
			
			//Filled in when loaded. Based on pixelSize
			public float offset;
			
			static ConfigElement[] cfg;
			public void Load(string effectName) {
				if (cfg == null) cfg = ConfigElement.GetAll(typeof(EffectConfig));
				ConfigElement.ParseFile(cfg, "effects/"+effectName+".properties", this);
			}
			
			public void Save(string effectName) {
				if (cfg == null) cfg = ConfigElement.GetAll(typeof(EffectConfig));
				ConfigElement.SerialiseSimple(cfg, "effects/"+effectName+".properties", this);
			}
		}
		public const int spawnerLimit = 32;
		public static int spawnerAccum;
		public static Dictionary<Level, List<EffectSpawner>> spawnersAtLevel = new Dictionary<Level, List<EffectSpawner>>();
		public class EffectSpawner {
		    [ConfigString] public string name;
			[ConfigString] public string effectName;
			[ConfigString] public string owner;
			[ConfigFloat] public float x;
			[ConfigFloat] public float y;
			[ConfigFloat] public float z;
			[ConfigFloat] public float originX;
			[ConfigFloat] public float originY;
			[ConfigFloat] public float originZ;
			//the amount of time (in tenths of seconds) it waits between ticks to attempt to spawn a particle. 0 means it spawns 10 times a second.
			[ConfigInt] public int spawnInterval = 0;
			//the amount of time (in tenths of seconds) this spawner's interval is offset from default
			[ConfigInt] public int spawnTimeOffset = 0;
			//the percentage chance it has to actually spawn a particle when it's told to
			[ConfigFloat] public float spawnChance = 1;
            public static bool CanEditAny(Player p) {
                if (LevelInfo.IsRealmOwner(p.name, p.level.name)) { return true; }
                ItemPerms perms = CommandExtraPerms.Find("EffectSpawner", 1);
                perms = perms == null ? new ItemPerms(LevelPermission.Operator, null, null) : perms;
                if (perms.UsableBy(p.Rank)) { return true; }
                return false;
            }
            public bool EditableBy(Player p, string attemptedAction = "modify") {
                if (CanEditAny(p)) { return true; }
                if (owner == p.name) { return true; }
                
                p.Message("&WYou are not allowed to {0} spawners that you did not create.", attemptedAction);
                return false;
            }
		}
		public static void AddSpawner(EffectSpawner spawner, Level lvl, bool save) {
			if (!spawnersAtLevel.ContainsKey(lvl)) {
		        //Logger.Log(LogType.Debug, "We are adding key {0} for the first time.", lvl.MapName);
				List<EffectSpawner> spawnerList = new List<EffectSpawner>();
				spawnerList.Add(spawner);
				spawnersAtLevel.Add(lvl, spawnerList);
			} else {
				spawnersAtLevel[lvl].Add(spawner);
			}
		    if (save) { SpawnersFile.Save(lvl); }
		}
		public static void RemoveSpawner(EffectSpawner spawner, Level lvl, bool save) {
		    if (!spawnersAtLevel.ContainsKey(lvl)) { return; } 
		    spawnersAtLevel[lvl].Remove(spawner);
		    if (save) { SpawnersFile.Save(lvl); }
		}
		public static void RemoveAllSpawners(Level lvl, bool save) {
			if (!spawnersAtLevel.Remove(lvl)) { return; }
		    if (save) { SpawnersFile.Save(lvl); }
		}
		//SHAMEFULLY stolen and edited from BotsFile.cs
        public static class SpawnersFile {
		    public static ThreadSafeCache cache;
		    public static string spawnerDirectory = "effects/spawners/";
            public static string SpawnerPath(string map) { return spawnerDirectory + map + ".json"; }
            static ConfigElement[] elems;
            
            public static void Load(Level lvl) {
                object locker = cache.GetLocker(lvl.name);
                lock (locker) {
                    LoadCore(lvl);
                }
            }
            static void LoadCore(Level lvl) {
                string path = SpawnerPath(lvl.MapName);
                if (!File.Exists(path)) return;
                string json = File.ReadAllText(path);
                List<EffectSpawner> spawners = null;
                
                try {
                    spawners = ReadAll(json);
                } catch (Exception ex) {
                    Logger.LogError("Reading spawners file", ex); return;
                }
                
                foreach (EffectSpawner spawner in spawners) {
                    AddSpawner(spawner, lvl, false);
                }
            }
            
            internal static List<EffectSpawner> ReadAll(string json) {
                List<EffectSpawner> spawners = new List<EffectSpawner>();
                if (elems == null) elems = ConfigElement.GetAll(typeof(EffectSpawner));
                
                //JsonContext ctx = new JsonContext(); ctx.Val = json;
                //JsonArray array = (JsonArray)Json.ParseStream(ctx);
                JsonReader reader = new JsonReader(json);
                JsonArray array = (JsonArray)reader.Parse();
				
                if (array == null) return spawners;
                
                foreach (object raw in array) {
                    JsonObject obj = (JsonObject)raw;
                    if (obj == null) continue;
                    
                    EffectSpawner data = new EffectSpawner();
                    obj.Deserialise(elems, data);
                    
                    spawners.Add(data);
                }
                return spawners;
            }
            
            public static void Save(Level lvl) {
                object locker = cache.GetLocker(lvl.name);
                lock (locker) {
                    SaveCore(lvl);
                }
            }
            static void SaveCore(Level lvl) {
                string path = SpawnerPath(lvl.MapName);
                if (!spawnersAtLevel.ContainsKey(lvl)) {
                    //Logger.Log(LogType.Debug, "There was no key level in spawnersAtLevel");
                    if (File.Exists(path)) {
                        File.Delete(path);
                        //Logger.Log(LogType.Debug, "Deleting file");
                    }
                    return;
                }
                List<EffectSpawner> spawners = spawnersAtLevel[lvl];
                
                if (spawners.Count == 0) {
                    if (!File.Exists(path)) { return; }
                    File.Delete(path);
                    //Logger.Log(LogType.Debug, "There was a key but no spawners. Deleting file");
                    return;
                }
                
                try {
                    using (StreamWriter w = new StreamWriter(path)) { WriteAll(w, spawners); }
                } catch (Exception ex) {
                    Logger.LogError("Error saving spawners to " + path, ex);
                }
            }
            
            public static void Delete(string levelName) {
                object locker = cache.GetLocker(levelName);
                lock (locker) {
                    DeleteCore(levelName);
                }
            }
            static void DeleteCore(string levelName) {
                string path = SpawnerPath(levelName);
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            }
            
            public static void Copy(string srcMap, string dstMap) {
                string srcPath = SpawnerPath(srcMap);
                string dstPath = SpawnerPath(dstMap);
                File.Copy(srcPath, dstPath);
            }
            
            public static void Rename(string srcMap, string dstMap) {
                string srcPath = SpawnerPath(srcMap);
                string dstPath = SpawnerPath(dstMap);
				if (!File.Exists(srcPath)) { return; }
                File.Move(srcPath, dstPath);
            }
            
            internal static void WriteAll(StreamWriter w, List<EffectSpawner> props) {
                w.WriteLine("[");
                if (elems == null) elems = ConfigElement.GetAll(typeof(EffectSpawner));
                string separator = null;
                
                for (int i = 0; i < props.Count; i++) {
                    w.Write(separator);
                    Json.Serialise(w, elems, props[i]);
                    separator = ",\r\n";
                }
                w.WriteLine("]");
            }
        }
		
		public static void DefineEffect(Player p, EffectConfig effect) {	
			p.Send(Packet.DefineEffect(
										effect.ID,
										effect.pixelU1,
										effect.pixelV1,
										effect.pixelU2,
										effect.pixelV2,
										effect.tintRed,
										effect.tintGreen,
										effect.tintBlue,
										effect.frameCount,
										effect.particleCount,
										(byte)(effect.pixelSize*2), //convert pixel size to world unit size
										effect.sizeVariation,
										effect.spread,
										effect.speed,
										effect.gravity,
										effect.baseLifetime,
										effect.lifetimeVariation,
										effect.expireUponTouchingGround,
										effect.collidesSolid,
										effect.collidesLiquid,
										effect.collidesLeaves,
										effect.fullBright ));
		}
		
		static Random rnd;
		public static Dictionary<string, EffectConfig> effectAtEffectName = new Dictionary<string, EffectConfig>();
        
        static readonly object locker = new object();
        static int dependencyCount = 0;
        // TryLoad and TryUnload should *NOT* be called if this plugin is placed in the plugins folder to be loaded automatically.
        // These methods are only to be used for plugins that have a dependency on GoodlyEffects when GoodlyEffects.dll is placed in root server folder
        public static void TryLoad() {
            lock (locker) {
                if (dependencyCount == 0) {
                    new PluginGoodlyEffects().Load(false);
                }
                dependencyCount++;
            }
        }
        public static void TryUnload() {
            lock (locker) {
                dependencyCount--;
                if (dependencyCount == 0) {
                    new PluginGoodlyEffects().Unload(false);
                }
            }
        }
        
		public DateTime startTime = DateTime.UtcNow;
		public override void Load(bool startup) {
			Command.Register(new CmdReloadEffects());
			Command.Register(new CmdEffect());
			Command.Register(new CmdSpawner());
			
			rnd = new Random();
			LoadEffects();
			DefineEffectsAll();
			

			OnPlayerFinishConnectingEvent.Register(OnPlayerFinishConnecting, Priority.Low);
			OnLevelLoadedEvent.Register(OnLevelLoaded, Priority.Low);
			OnLevelUnloadEvent.Register(OnLevelUnload, Priority.Low);
			OnLevelDeletedEvent.Register(OnLevelDeleted, Priority.Low);
			OnLevelCopiedEvent.Register(OnLevelCopied, Priority.Low);
			OnLevelRenamedEvent.Register(OnLevelRenamed, Priority.Low);
			
			SpawnersFile.cache = new ThreadSafeCache();
			if (!Directory.Exists(SpawnersFile.spawnerDirectory)) {
			    Directory.CreateDirectory(SpawnersFile.spawnerDirectory);
			}
			Level[] levels = LevelInfo.Loaded.Items;
			foreach (Level level in levels) {
			    SpawnersFile.Load(level);
			}
			spawnerAccum = 0;
			ActivateSpawners();
		}
		public override void Unload(bool shutdown) {
			Command.Unregister(Command.Find("ReloadEffects"));
			Command.Unregister(Command.Find("Effect"));
			Command.Unregister(Command.Find("Spawner"));
			
			OnPlayerFinishConnectingEvent.Unregister(OnPlayerFinishConnecting);
			OnLevelLoadedEvent.Unregister(OnLevelLoaded);
			OnLevelUnloadEvent.Unregister(OnLevelUnload);
			OnLevelDeletedEvent.Unregister(OnLevelDeleted);
			OnLevelCopiedEvent.Unregister(OnLevelCopied);
			OnLevelRenamedEvent.Unregister(OnLevelRenamed);
			
			spawnersAtLevel.Clear();
			instance.Cancel(tickSpawners);
		}
		
		static void OnPlayerFinishConnecting(Player p) {
			DefineEffectsFor(p);
		}
		static void OnLevelLoaded(Level lvl) {
		    SpawnersFile.Load(lvl);
		}
		static void OnLevelUnload(Level lvl, ref bool cancel) {
			//if the level is forced to stay loaded, don't remove spawners
			if (cancel) return;
		    RemoveAllSpawners(lvl, false);
		}
		static void OnLevelDeleted(string map) {
		    SpawnersFile.Delete(map);
		}
		static void OnLevelCopied(string srcMap, string dstMap) {
		    SpawnersFile.Copy(srcMap, dstMap);
		}
		static void OnLevelRenamed(string srcMap, string dstMap) {
		    SpawnersFile.Rename(srcMap, dstMap);
		}
		
        static Scheduler instance;
        static SchedulerTask tickSpawners;
        public static void ActivateSpawners() {
            if (instance == null) instance = new Scheduler("EffectSpawnerScheduler");
            tickSpawners = instance.QueueRepeat(SpawnersTick, null, TimeSpan.FromMilliseconds(100));
        }
        static void SpawnersTick(SchedulerTask task) {
            spawnerAccum++;
            foreach (Player p in PlayerInfo.Online.Items) {
                if (!spawnersAtLevel.ContainsKey(p.level)) { continue; }
                
                List<EffectSpawner> spawners = spawnersAtLevel[p.level];
                if (spawners.Count == 0) { continue; }
				//We are going to tackle the myth that classicube is minecraft. So, why the "j"? (IDK LOL)
                for (int j = 0; j < spawners.Count; j++) {
					EffectSpawner spawner = spawners[j];
					
					if (spawner.spawnInterval == 0 || (spawnerAccum+spawner.spawnTimeOffset) % spawner.spawnInterval == 0) {
					    if (rnd.NextDouble() > spawner.spawnChance) { continue; }
					    SpawnEffectFor(p, spawner.effectName, spawner.x, spawner.y, spawner.z, spawner.originX, spawner.originY, spawner.originZ);
					}
				}
            }
        }
		
		public static void LoadEffects() {
			string directory = "effects/";
			if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
			DirectoryInfo info = new DirectoryInfo(directory);
			FileInfo[] allEffectFiles = info.GetFiles();
			
			for (int i = 0; i < allEffectFiles.Length; i++) {
			    string realName = Path.GetFileNameWithoutExtension(allEffectFiles[i].Name);
				if (i > byte.MaxValue) {
					Logger.Log(LogType.Warning, "GoodlyEffects: The maximum of 256 effect files have already been loaded. Effect {0} cannot be loaded!", realName);
					continue;
				}
				
				EffectConfig effect = new EffectConfig();
				effect.Load(realName);
				effect.ID = (byte)(i);
				effect.offset = effect.pixelSize / 32;
				effectAtEffectName[realName] = effect;
				Logger.Log(LogType.SystemActivity, "GoodlyEffects: Loaded effect {0} with ID {1}.", realName, i);
			}
		}
		public static void DefineEffectsFor(Player p) {
			if  (!p.Supports(CpeExt.CustomParticles)) {
				p.Message("&WCould not define custom particles because your client is outdated.");
				return;
			}
            p.Socket.LowLatency = true;
            
			foreach(KeyValuePair<string, EffectConfig> entry in effectAtEffectName)
			{
				DefineEffect(p, entry.Value);
			}
		}
		public static void DefineEffectsAll() {
			Player[] players = PlayerInfo.Online.Items;
			foreach (Player p in players) {
				DefineEffectsFor(p);
			}
		}
		public static void SpawnEffectAt(Level lvl, string effectName, float x, float y, float z, float originX, float originY, float originZ, Player notShownTo = null) {
			EffectConfig effect;
			if (!effectAtEffectName.TryGetValue(effectName, out effect)) {
				Logger.Log(LogType.Warning, "GoodlyEffects: Could not find effect named \"{0}\" !", effectName);
				return;
			}
			Player[] players = PlayerInfo.Online.Items;
			foreach (Player p in players) {
				if (p.level != lvl || !p.Supports(CpeExt.CustomParticles) || p == notShownTo) { continue; }
				p.Send(Packet.SpawnEffect(effect.ID, x, y-effect.offset, z, originX, originY-effect.offset, originZ));
			}
		}
		public static void SpawnEffectFor(Player p, string effectName, float x, float y, float z, float originX, float originY, float originZ) {
			EffectConfig effect;
			if (!effectAtEffectName.TryGetValue(effectName, out effect)) {
				p.Message("&WCould not find effect named \"{0}\" !", effectName);
				return;
			}
			if (!p.Supports(CpeExt.CustomParticles)) { return; }
			p.Send(Packet.SpawnEffect(effect.ID, x, y-effect.offset, z, originX, originY-effect.offset, originZ));
		}
		
	}
	
	public class CmdReloadEffects : Command2
	{
		public override string name { get { return "ReloadEffects"; } }
		public override string shortcut { get { return ""; } }
		public override bool MessageBlockRestricted { get { return true; } }
		public override string type { get { return "fun"; } }
		public override bool museumUsable { get { return false; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
		public override void Use(Player p, string message, CommandData data)
		{
			GoodlyEffects.effectAtEffectName.Clear();
			GoodlyEffects.LoadEffects();
			GoodlyEffects.DefineEffectsAll();
			p.Message("Reloaded effects!");
		}
		public override void Help(Player p)
		{
            p.Message("&T/ReloadEffects");
            p.Message("&HReloads the effects from the config files.");
		}
	}
	
	public class CmdEffect : Command2
	{
		public override string name { get { return "Effect"; } }
		public override string shortcut { get { return ""; } }
		public override bool MessageBlockRestricted { get { return false; } }
		public override string type { get { return "fun"; } }
		public override bool museumUsable { get { return false; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override CommandAlias[] Aliases {
            get { return new[] {
                    new CommandAlias("Effects", "list")
                }; }
        }
		public override void Use(Player p, string message, CommandData data)
		{
			if (message == "") { Help(p); return; }
            if (message.CaselessEq("list")) { ListEffects(p); return; }
            
			string[] words = message.Split(' ');
			if (words.Length < 7) {
				p.Message("&WYou need to provide effect, x, y, z, originX, originY, and originZ.");
				return;
			}
			string effectName = words[0];
			float x = 0, y = 0, z = 0;
			float originX = 0, originY = 0, originZ = 0;
			bool showToAll = false;
			if (!CommandParser.GetReal(p, words[1], "x", ref x)) { return; }
			if (!CommandParser.GetReal(p, words[2], "y", ref y)) { return; }
			if (!CommandParser.GetReal(p, words[3], "z", ref z)) { return; }
			if (!CommandParser.GetReal(p, words[4], "originX", ref originX)) { return; }
			if (!CommandParser.GetReal(p, words[5], "originY", ref originY)) { return; }
			if (!CommandParser.GetReal(p, words[6], "originZ", ref originZ)) { return; }
			if (words.Length >= 8) {
				if (!CommandParser.GetBool(p, words[7], ref showToAll)) { return; }
			}
			GoodlyEffects.EffectConfig effect;
			if (!GoodlyEffects.effectAtEffectName.TryGetValue(effectName, out effect)) {
				p.Message("&WUnknown effect \"{0}\".", effectName);
				return;
			}
			
			//default to center of block
			x += 0.5f;
			y += 0.5f;
			z += 0.5f;
			originX += x;
			originY += y;
			originZ += z;
			
			if (showToAll) {
				GoodlyEffects.SpawnEffectAt(p.level, effectName, x, y, z, originX, originY, originZ);
			} else {
				GoodlyEffects.SpawnEffectFor(p, effectName, x, y, z, originX, originY, originZ);
			}
		}
		public void ListEffects(Player p) {
			p.Message("Currently available effects:");
			foreach(KeyValuePair<string, GoodlyEffects.EffectConfig> entry in GoodlyEffects.effectAtEffectName)
			{
				p.Message("&H{0}", entry.Key);
			}
			p.Message("Scroll up to see more effects.");
		}
		public override void Help(Player p)
		{
            p.Message("&T/Effect [effect] [x y z] [originX originY originZ] <show to all players>");
            p.Message("&HSpawns an effect");
			p.Message("&HOrigin is relative and determines the particle's direction.");
			p.Message("&HE.G. origin of 0 -1 0 will make the particles move up.");
			p.Message("&H[show to all players] is optional true or false.");
            p.Message("&T/Effect list &H- Lists currently available effects.");
		}
	}
	
	public class CmdSpawner : Command2
	{
		public override string name { get { return "Spawner"; } }
		//public override string shortcut { get { return "effectspawner"; } }
		public override bool MessageBlockRestricted { get { return true; } }
		public override string type { get { return "fun"; } }
		public override bool museumUsable { get { return false; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandAlias[] Aliases {
            get { return new[] {
                    //new CommandAlias("SpawnerAdd", "add"),
                    //new CommandAlias("SpawnerRemove", "remove"),
                    new CommandAlias("Spawners", "list")
                }; }
        }
        public override CommandPerm[] ExtraPerms {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can edit spawners that don't belong to them") }; }
        }
		public override void Use(Player p, string message, CommandData data)
		{
		    if (message == "") { Help(p); return; }
		    string[] split = message.SplitSpaces(2);
		    string func = split[0];
		    string args = (split.Length < 2) ? "" : split[1];
		    if (func.CaselessEq("list")) { DoList(p, args); return; }
		    if (func.CaselessEq("tp")) { DoTP(p, args); return; }
		    if (!LevelInfo.Check(p, data.Rank, p.level, "modify spawners in this level")) return;
		    if (func.CaselessEq("add")) { DoAdd(p, args); return; }
		    if (func.CaselessEq("remove")) { DoRemove(p, args); return; }
		    if (func.CaselessEq("summon")) { DoSummon(p, args); return; }
		    p.Message("&W\"{0}\" is not a recognized argument.", func);
		    p.Message("Please use &T/help spawner&S.");
		    return;
		}
		static void DoList(Player p, string message) {
	        if (message.CaselessEq("all")) {
		        p.Message("&fADMIN DEBUG LIST");
    		    foreach (KeyValuePair<Level, List<GoodlyEffects.EffectSpawner>> value in GoodlyEffects.spawnersAtLevel) {
    		        p.Message("&fKey is {0}", value.Key.MapName);
    		        foreach (GoodlyEffects.EffectSpawner spawner in value.Value) {
    		            p.Message("&fSpawner is {0}", spawner.name);
    		        }
    		    }
	        }
		    
		    if (GoodlyEffects.spawnersAtLevel.ContainsKey(p.level)) {
		        int count = 0;
		        p.Message("Spawners in {0}:", p.level.ColoredName);
		        foreach (GoodlyEffects.EffectSpawner spawner in GoodlyEffects.spawnersAtLevel[p.level]) {
		            p.Message("{0}, made by {1}", spawner.name, spawner.owner);
		            count++;
		        }
		        p.Message("There are {0} spawners in {1}&S.", count, p.level.ColoredName);
		    } else {
		        p.Message("There are no spawners in {0}&S.", p.level.ColoredName);
		    }
		}
		static void DoAdd(Player p, string message) {
		    if (SpawnerCount(p.level) >= GoodlyEffects.spawnerLimit) {
		        p.Message("&WThe limit of {0} spawners per level has been reached.", GoodlyEffects.spawnerLimit);
		        p.Message("You may remove spawners with &T/spawner remove&S.");
		        return;
		    }
			string[] words = message.Split(' ');
			if (words.Length < 8) {
				p.Message("&WTo add a spawner you need to provide spawner name, effect, x, y, z, originX, originY, and originZ.");
				return;
			}
			string spawnerName = words[0];
			if (SpawnerNameExists(p, spawnerName)) { return; }
			string effectName = words[1];
			GoodlyEffects.EffectConfig effect;
			if (!GoodlyEffects.effectAtEffectName.TryGetValue(effectName, out effect)) {
				p.Message("&WUnknown effect \"{0}\".", effectName);
				return;
			}
			
			float x = 0, y = 0, z = 0;
			float originX = 0, originY = 0, originZ = 0;
			int spawnInterval = 0;
			int spawnTimeOffset = 0;
			float spawnChance = 1f;
			if (!GetCoord(p, words[2], p.Pos.BlockX,            "x", out x)) { return; };
			if (!GetCoord(p, words[3], p.Pos.FeetBlockCoords.Y, "y", out y)) { return; };
			if (!GetCoord(p, words[4], p.Pos.BlockZ,            "z", out z)) { return; };
			if (!GetCoord(p, words[5], p.Pos.BlockX,            "originX", out originX)) { return; };
			if (!GetCoord(p, words[6], p.Pos.FeetBlockCoords.Y, "originY", out originY)) { return; };
			if (!GetCoord(p, words[7], p.Pos.BlockZ,            "originZ", out originZ)) { return; };
			if (words.Length > 8) {
			    if (!CommandParser.GetInt(p, words[8], "spawn interval", ref spawnInterval, 0, 600)) { return; }
			}
			if (words.Length > 9) {
			    if (!CommandParser.GetInt(p, words[9], "spawn time offset", ref spawnTimeOffset, 0, 599)) { return; }
			}
			if (words.Length > 10) {
			    if (!CommandParser.GetReal(p, words[10], "spawn chance", ref spawnChance, 0.01f, 100)) { return; }
			    //convert percentage to 0-1
			    spawnChance /= 100f;
			}
			
			//default to center of block
			x += 0.5f;
			y += 0.5f;
			z += 0.5f;
			originX += 0.5f;
			originY += 0.5f;
			originZ += 0.5f;
			
			GoodlyEffects.EffectSpawner spawner = new GoodlyEffects.EffectSpawner();
			spawner.name = spawnerName;
			spawner.effectName = effectName;
			spawner.owner = p.name;
			spawner.x = x;
			spawner.y = y;
			spawner.z = z;
			spawner.originX = originX;
			spawner.originY = originY;
			spawner.originZ = originZ;
			spawner.spawnInterval = spawnInterval;
			spawner.spawnTimeOffset = spawnTimeOffset;
			spawner.spawnChance = spawnChance;
			
			GoodlyEffects.AddSpawner(spawner, p.level, true);
			p.Message("Successfully added a spawner named {0}.", spawner.name);
		}
		static void DoRemove(Player p, string message) {
		    if (message.CaselessEq("all")) {
		        if (GoodlyEffects.EffectSpawner.CanEditAny(p)) {
    		        GoodlyEffects.RemoveAllSpawners(p.level, true);
    		        p.Message("Removed all spawners from {0}&S.", p.level.ColoredName);
		        } else {
		            p.Message("&WYou cannot remove all spawners unless you are the owner of this map.");
		        }
		        return;
		    }
		    if (message == "") { p.Message("&WPlease provide the name of a spawner to remove."); return; }
		    if (!GoodlyEffects.spawnersAtLevel.ContainsKey(p.level)) {
		        p.Message("There are no spawners in {0}&S.", p.level.ColoredName);
		        return;
		    }
		    int matches;
		    GoodlyEffects.EffectSpawner spawner = Matcher.Find(p, message, out matches,
		                                                             GoodlyEffects.spawnersAtLevel[p.level],
		                                                             x => true,
		                                                             x => x.name,
		                                                             "effect spawners");
		    if (matches > 1 || spawner == null) { return; }
		    if (spawner.EditableBy(p, "remove")) {
    		    p.Message("Removed spawner {0}.", spawner.name);
    		    GoodlyEffects.RemoveSpawner(spawner, p.level, true);
		    }
		}
		static void DoTP(Player p, string message) {
		    if (!Hacks.CanUseHacks(p)) {
		        p.Message("&WYou can't teleport to spawners because hacks are disabled in {0}", p.level.ColoredName);
		        return;
		    }
		    if (message == "") { p.Message("&WPlease provide the name of a spawner to teleport to."); return; }
		    if (!GoodlyEffects.spawnersAtLevel.ContainsKey(p.level)) {
		        p.Message("There are no spawners in {0}&S to teleport to.", p.level.ColoredName);
		        return;
		    }
		    int matches;
		    GoodlyEffects.EffectSpawner spawner = Matcher.Find(p, message, out matches,
		                                                             GoodlyEffects.spawnersAtLevel[p.level],
		                                                             x => true,
		                                                             x => x.name,
		                                                             "effect spawners");
		    if (matches > 1 || spawner == null) { return; }
		    Command.Find("tp").Use(p, "-precise "+(int)(spawner.x*32)+" "+(int)(spawner.y*32)+" "+(int)(spawner.z*32));
		}
		static void DoSummon(Player p, string message) {
		    if (message == "") { p.Message("&WPlease provide the name of a spawner to summon."); return; }
		    string[] args = message.SplitSpaces(2);
		    string spawnerName = args[0];
		    bool precise = (args.Length > 1) ? args[1].CaselessEq("precise") : false;
		    p.Message("precise is {0}", precise);
		        
		    if (!GoodlyEffects.spawnersAtLevel.ContainsKey(p.level)) {
		        p.Message("There are no spawners in {0}&S.", p.level.ColoredName);
		        return;
		    }
		    int matches;
		    GoodlyEffects.EffectSpawner spawner = Matcher.Find(p, spawnerName, out matches,
		                                                             GoodlyEffects.spawnersAtLevel[p.level],
		                                                             x => true,
		                                                             x => x.name,
		                                                             "effect spawners");
		    if (matches > 1 || spawner == null) { return; }
		    if (spawner.EditableBy(p, "summon")) {
		        float diffX = spawner.x - spawner.originX;
		        float diffY = spawner.y - spawner.originY;
		        float diffZ = spawner.z - spawner.originZ;
		        
		        if (precise) {
    		        spawner.x = (float)(p.Pos.X) / 32f;
    		        spawner.y = (float)(p.Pos.Y - Entities.CharacterHeight) / 32f;
    		        spawner.z = (float)(p.Pos.Z) / 32f;
		        } else {
		            spawner.x = p.Pos.BlockX;
		            spawner.y = p.Pos.FeetBlockCoords.Y;
		            spawner.z = p.Pos.BlockZ;
		            //center in block
		            spawner.x += 0.5f;
		            spawner.y += 0.5f;
		            spawner.z += 0.5f;
		        }
		        
		        spawner.originX = spawner.x - diffX;
		        spawner.originY = spawner.y - diffY;
		        spawner.originZ = spawner.z - diffZ;
		        if (precise) {
                    p.Message("Summoned spawner {0} to your precise feet position.", spawner.name);
		        } else {
		            p.Message("Summoned spawner {0} to your block position.", spawner.name);
		        }
    		    GoodlyEffects.SpawnersFile.Save(p.level);
		    }
		}
		
		public override void Help(Player p)
		{
		    p.Message("&HSpawner help page 1:");
            p.Message("&T/Spawner add");
            p.Message("&T[name] [effect] [x y z] [originX originY originZ]");
            p.Message("&T<interval> <time offset> <spawn % chance = 100>");
            p.Message("&HAdds an effect spawner to the world.");
            p.Message("&HPlease use &T/help spawner add &Hfor details on adding.");
			p.Message("&HTo read help page 2, type &T/help spawner 2");
		}
		public override void Help(Player p, string message) {
		    if (message.CaselessEq("2")) {
		        p.Message("&HSpawner help page 2:");
    			p.Message("&T/Spawner remove [name] &H- removes a spawner.");
    			p.Message("&HIf [name] is \"all\", all spawners are removed.");
    			p.Message("&T/Spawner tp [name] &H- teleports you to a spawner.");
    			p.Message("&T/Spawner summon [name] <style> &H- summons a spawner to");
    			p.Message("&Hyour block position. If <style> is \"precise\",");
    			p.Message("&Hthe spawner is summoned to your exact feet position.");
    			p.Message("&T/Spawner list &H- lists spawners in current level.");
    			return;
		    }
		    if (message.CaselessEq("add")) {
		        p.Message("&fRequired arguments for adding a spawner:");
		        p.Message("&T[name] &His used to identify the spawner.");
		        p.Message("&T[effect] &H- the effect this spawner creates.");
		        p.Message("&HUse &T/effect list &Hto view available effects.");
		        p.Message("&T[x y z] &H- the coords the effect spawns around.");
    			p.Message("&T[origin] &H- the coords the effect moves away from.");
    			p.Message("&HTIP: use ~ for coords relative to you.");
    			p.Message("&HE.G. &T~ ~0.5 ~");
    			p.Message("&Hwould make coords at the top of the block you're standing in.");
    			p.Message("&HUse &T/help spawner options &Hfor optional arguments");
    			return;
		    }
		    if (message.CaselessEq("options")) {
		        p.Message("&fOptional arguments for adding a spawner:");
    			p.Message("&T<interval> &H- how long to wait between spawns.");
    			p.Message("&HAn interval of 10 would spawn once per second.");
    			p.Message("&T<time offset> &Hoffsets when the effect spawns.");
    			p.Message("&HAn offset of 5 means half a second.");
    			p.Message("&T<spawn % chance> &H- chance the effect spawns.");
    			p.Message("&HThe default is 100, which means it always spawns.");
    			return;
		    }
		    p.Message("There is no help page named \"{0}\".", message);
		}
		//stolen from CommandParser.cs. Modified to work with float instead of int
        static bool GetCoord(Player p, string arg, float cur, string axis, out float value) {
            bool relative = arg[0] == '~';
            if (relative) arg = arg.Substring(1);
            value = 0;
            // ~ should work as ~0
            if (relative && arg.Length == 0) { value += cur; return true; }
            
            if (!CommandParser.GetReal(p, arg, axis, ref value)) return false;
            if (relative) value += cur;
            return true;
        }
		static bool SpawnerNameExists(Player p, string name) {
		    if (!GoodlyEffects.spawnersAtLevel.ContainsKey(p.level)) { return false; }
		    foreach (GoodlyEffects.EffectSpawner spawner in GoodlyEffects.spawnersAtLevel[p.level]) {
		        if (name.CaselessEq(spawner.name)) {
		            p.Message("A spawner named \"{0}\" already exists.", spawner.name);
		            return true;
		        }
		    }
		    return false;
		}
		static int SpawnerCount(Level lvl) {
		    if (!GoodlyEffects.spawnersAtLevel.ContainsKey(lvl)) { return 0; }
		    return GoodlyEffects.spawnersAtLevel[lvl].Count;
		}
	}
}
