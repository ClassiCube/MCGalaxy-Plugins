//reference System.Core.dll
//pluginref _extralevelprops.dll

using System;
using System.Linq;
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
using ExtraLevelProps;

namespace MCGalaxy {
    
    public sealed class GoodlyEffects : Plugin {
        public override string name { get { return "GoodlyEffects"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.2"; } }
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
                using (StreamWriter w = FileIO.CreateGuarded("effects/" + effectName + ".properties")) {
                    ConfigElement.SerialiseElements(cfg, w, this);
                }
            }
        }
        public const int spawnerLimit = 32;
        public static int spawnerAccum;
        public static Dictionary<Level, List<EffectSpawner>> spawnersAtLevel = new Dictionary<Level, List<EffectSpawner>>();
        public class EffectSpawner {
            
            public EffectSpawner Clone(string newName) {
                EffectSpawner clone = new EffectSpawner();
                clone.name = newName;
                clone.effectName = effectName;
                clone.owner = owner;
                clone.x = x;
                clone.y = y;
                clone.z = z;
                clone.originX = originX;
                clone.originY = originY;
                clone.originZ = originZ;
                clone.spawnInterval = spawnInterval;
                clone.spawnTimeOffset = spawnTimeOffset;
                clone.spawnChance = spawnChance;
                return clone;
            }
            
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
                ItemPerms perms = CommandExtraPerms.GetOrAdd("Spawner", 1, LevelPermission.Operator);
                return perms.UsableBy(p.Rank);
            }
            public bool EditableBy(Player p, string attemptedAction = "modify") {
                if (CanEditAny(p)) { return true; }
                if (owner == p.name) { return true; }
                
                p.Message("&WYou are not allowed to {0} spawners that you did not create.", attemptedAction);
                return false;
            }
            
            public void EditEffect(Player p, string newName) {
                if (GetEffect(p, newName) == null) { return; } //ensure new name is a valid effect
                this.effectName = newName;
                p.Message("Spawner &f{0}&S's effect was changed to &f{1}&S.", name, newName);
            }
            
            public Vec3F32 position {
                get => new Vec3F32(x, y, z);
                set { x = value.X; y = value.Y; z = value.Z; }
            }
            public Vec3F32 origin {
                get => new Vec3F32(originX, originY, originZ);
                set { originX = value.X; originY = value.Y; originZ = value.Z; }
            }
            public Vec3F32 positionPreserveRelativeOrigin {
                set {
                    Vec3F32 diff = position - origin; //preserve origin
                    
                    position = value;
                    
                    origin = position - diff; //preserve origin
                }
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
                if (!File.Exists(srcPath)) { return; }
                File.Copy(srcPath, dstPath);
            }
            
            public static void Rename(string srcMap, string dstMap) {
                string srcPath = SpawnerPath(srcMap);
                string dstPath = SpawnerPath(dstMap);
                if (!File.Exists(srcPath)) { return; }
                File.Move(srcPath, dstPath);
            }
            
            internal static void WriteAll(StreamWriter w, List<EffectSpawner> props) {
                if (elems == null) elems = ConfigElement.GetAll(typeof(EffectSpawner));
                JsonConfigWriter ser = new JsonConfigWriter(w, elems);
                string separator = null;
                
                w.WriteLine("[");
                for (int i = 0; i < props.Count; i++) 
                {
                    w.Write(separator);
                    ser.WriteObject(props[i]);
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

        public const string NoSpawnersProp = "nospawners";
        static readonly string[] NoSpawnersDesc = new string[] {
            "[true/false]",
            "If true, /spawner may not be used.",
        };

        public override void Load(bool startup) {

            ExtraLevelProps.ExtraLevelProps.Register(name, NoSpawnersProp, LevelPermission.Guest, NoSpawnersDesc, ExtraLevelProps.ExtraLevelProps.OnPropChangingBool);

            Command.Register(new CmdReloadEffects());
            Command.Register(new CmdEffect());
            Command.Register(new CmdSpawner());
            
            rnd = new Random();
            ReloadEffects();            

            OnPlayerFinishConnectingEvent.Register(OnPlayerFinishConnecting, Priority.Low);
            OnConfigUpdatedEvent.Register(OnConfigUpdated, Priority.Low);
            
            
            OnLevelAddedEvent.Register(OnLevelAdded, Priority.Low);
            OnLevelRemovedEvent.Register(OnLevelRemoved, Priority.Low);
            OnLevelDeletedEvent.Register(OnLevelDeleted, Priority.Low);
            OnLevelCopiedEvent.Register(OnLevelCopied, Priority.Low);
            OnLevelRenamedEvent.Register(OnLevelRenamed, Priority.Low);
            
            
            SpawnersFile.cache = new ThreadSafeCache();
            if (!Directory.Exists(SpawnersFile.spawnerDirectory)) {
                Directory.CreateDirectory(SpawnersFile.spawnerDirectory);
            }
            ReloadSpawners();
            ActivateSpawners();
        }
        public override void Unload(bool shutdown) {

            ExtraLevelProps.ExtraLevelProps.Unregister(NoSpawnersProp);

            Command.Unregister(Command.Find("ReloadEffects"));
            Command.Unregister(Command.Find("Effect"));
            Command.Unregister(Command.Find("Spawner"));
            
            OnPlayerFinishConnectingEvent.Unregister(OnPlayerFinishConnecting);
            OnConfigUpdatedEvent.Unregister(OnConfigUpdated);
            
            OnLevelAddedEvent.Unregister(OnLevelAdded);
            OnLevelRemovedEvent.Unregister(OnLevelRemoved);
            OnLevelDeletedEvent.Unregister(OnLevelDeleted);
            OnLevelCopiedEvent.Unregister(OnLevelCopied);
            OnLevelRenamedEvent.Unregister(OnLevelRenamed);
            
            spawnersAtLevel.Clear();
            instance.Cancel(tickSpawners);
        }
        
        static void OnConfigUpdated() {
            spawnersAtLevel.Clear(); // TODO: remove this hack, just avoids warning messages to players when reloading
            ReloadEffects();
            ReloadSpawners();
        }
        
        static void OnPlayerFinishConnecting(Player p) {
            DefineEffectsFor(p);
        }
        
        static void OnLevelAdded(Level lvl) {
            SpawnersFile.Load(lvl);
        }
        static void OnLevelRemoved(Level lvl) {
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

        public static void ReloadEffects() {
            effectAtEffectName.Clear();
            LoadEffects();
            DefineEffectsAll();
        }

        public static void ReloadSpawners() {
            spawnersAtLevel.Clear();
            Level[] levels = LevelInfo.Loaded.Items;
            foreach (Level level in levels) {
                SpawnersFile.Load(level);
            }
            spawnerAccum = 0;
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
                //p.Message("&WCould not define custom particles because your client is outdated.");
                return;
            }
            
            try {
                p.Socket.LowLatency = true;
            } catch (ObjectDisposedException) {
                // player already disconnected
            }
            
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
        
        public static EffectConfig GetEffect(Player p, string effectName) {
            GoodlyEffects.EffectConfig effect;
            if (!GoodlyEffects.effectAtEffectName.TryGetValue(effectName, out effect)) {
                p.Message("&WUnknown effect \"{0}\".", effectName);
                p.Message("Use &T/effect list &Sto list all effects.");
                return null;
            }
            return effect;
        }
    }
    
    public class CmdReloadEffects : Command2 {
        public override string name { get { return "ReloadEffects"; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override string type { get { return "fun"; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override void Use(Player p, string message, CommandData data)
        {
            GoodlyEffects.ReloadEffects();
            p.Message("Reloaded effects!");
        }
        public override void Help(Player p)
        {
            p.Message("&T/ReloadEffects");
            p.Message("&HReloads the effects from the config files.");
        }
    }
    
    public class CmdEffect : Command2 {
        public override string name { get { return "Effect"; } }
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
            
            if (data.Context != CommandContext.MessageBlock && !LevelInfo.Check(p, p.Rank, p.level, "use /effect in this level")) return;
            
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
            
            List<string> names = new List<string>();
            foreach(var entry in GoodlyEffects.effectAtEffectName) { names.Add("&H"+entry.Key); }
            names.Sort((name1, name2) => string.Compare(name1, name2));
            
            p.Message("Currently available effects:");
            p.MessageLines(names);
            p.Message("Scroll up to see more effects.");
        }
        public override void Help(Player p) {
            p.Message("&T/Effect [effect] [x y z] [originX originY originZ] <show to all players>");
            p.Message("&HSpawns an effect");
            p.Message("&HOrigin is relative and determines the particle's direction.");
            p.Message("&HE.G. origin of 0 -1 0 will make the particles move up.");
            p.Message("&H&WTake note: &Horigin is backwards to what you'd expect.");
            p.Message("&H[show to all players] is optional true or false.");
            p.Message("&T/Effect list &H- Lists currently available effects.");
        }
    }
    
    public class CmdSpawner : Command2 {
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
            get { return new[]
                {
                    new CommandPerm(LevelPermission.Operator, "can edit spawners that don't belong to them"),
                    new CommandPerm(LevelPermission.Operator, "can add more than "+GoodlyEffects.spawnerLimit+" spawners in a map")
                };
            }
        }
        
        public override void Use(Player p, string message, CommandData data) {
            if (p.level.GetExtraPropBool(GoodlyEffects.NoSpawnersProp)) {
                p.Message("Spawners are disabled in this level.");
                return;
            }

            if (message == "") { Help(p); return; }
            string[] split = message.SplitSpaces(2);
            string func = split[0];
            string args = (split.Length < 2) ? "" : split[1];
            if (func.CaselessEq("list")) { DoList(p, args); return; }
            if (func.CaselessEq("tp")) { DoTP(p, args); return; }
            if (!LevelInfo.Check(p, data.Rank, p.level, "modify spawners in this level")) return;
            if (func.CaselessEq("add")) { DoAdd(p, args, data); return; }
            if (func.CaselessEq("edit")) { DoEdit(p, args); return; }
            if (func.CaselessEq("remove")) { DoRemove(p, args); return; }
            if (func.CaselessEq("info")) { DoInfo(p, args); return; }
            if (func.CaselessEq("copy")) { DoCopy(p, args); return; }
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
        void DoAdd(Player p, string message, CommandData data) {
            if (SpawnerCount(p.level) >= GoodlyEffects.spawnerLimit && !CheckExtraPerm(p, data, 2)) {
                p.Message("&WThe limit of {0} spawners per level has been reached.", GoodlyEffects.spawnerLimit);
                p.Message("You may remove spawners with &T/spawner remove&S.");
                return;
            }
            string[] words = message.SplitSpaces();
            if (words.Length < 2) {
                p.Message("&WTo add a spawner you need to provide a spawner name and effect.");
                p.Message("Use &T/effect list &Sto list all effects.");
                return;
            }
            string spawnerName = words[0];
            if (SpawnerNameExists(p, spawnerName)) { return; }
            
            string effectName = words[1];
            if (GoodlyEffects.GetEffect(p, effectName) == null) {
                return; //ensure the effect name is valid
            }
            
            Vec3F32 position = NonPreciseCoords(p);
            Vec3F32 origin = position;
            origin.Y -= 1; //go up
            
            GoodlyEffects.EffectSpawner spawner = new GoodlyEffects.EffectSpawner();
            spawner.name = spawnerName;
            spawner.effectName = effectName;
            spawner.owner = p.name;
            
            spawner.position = position;
            spawner.origin = origin;
            
            spawner.spawnInterval = 0;
            spawner.spawnTimeOffset = 0;
            spawner.spawnChance = 1;
            
            GoodlyEffects.AddSpawner(spawner, p.level, true);
            p.Message("Successfully added spawner &f{0}&S.", spawner.name);
            p.Message("Use &b/spawner edit {0} &Sto change direction and more.", spawner.name);
        }
        static void DoEdit(Player p, string message) {
            const int MAX_INTERVAL = 600;
            
            if (message.Length == 0) {
                p.Message("&WPlease provide the name of a spawner to edit.");
                p.Message("&7Use &a/spawners &7to list all spawners.");
                return;
            }
            
            string[] args = message.SplitSpaces(3);
            
            GoodlyEffects.EffectSpawner spawner = GetSpawner(p, args[0]);
            if (spawner == null) { return; }
            
            if (args.Length < 2) {
                DoInfo(p, spawner.name);
                p.Message("&HUse &T/spawner edit {0} [property]&H to learn how to change that property.", spawner.name);
                return;
            }
            
            if (!spawner.EditableBy(p, "summon")) { return; }
            
            string prop = args[1].ToLower();
            string value = args.Length < 3 ? "" : args[2];
            string[] values = value.SplitSpaces();
            
            switch (prop) {
                case "name":
                    if (value == "") {
                        p.Message("&HProvide a name for the spawner.");
                        return;
                    }
                    if (SpawnerNameExists(p, value)) { return; }
                    p.Message("Set &f{0}&S's name to &f{1}&S.", spawner.name, value);
                    spawner.name = value;
                    break;
                case "effect":
                    if (value == "") {
                        p.Message("&HProvide an effect for the spawner to spawn.");
                        p.Message("&HUse &T/effects &Hto view available effects.");
                        p.Message("&HExample: &T/spawner edit {0} effect steam", spawner.name);
                        return;
                    }
                    spawner.EditEffect(p, value);
                    break;
                case "owner":
                    p.Message("&WThe owner of a spawner cannot be edited.");
                    break;
                case "pos":
                case "position":
                    if (value == "") {
                        p.Message("&HProvide x y z coordinates for spawner position.");
                        p.Message("&HUse ~ for a coord to move the spawner relative to its current position.");
                        p.Message("&HOr type &bhere&H or &bhereprecise&H to move the spawner to your position.");
                        p.Message("&HExample: &T/spawner edit {0} pos ~ ~1 ~", spawner.name);
                        return;
                    }
                    
                    
                    //Modify via temporary variable because "ref" does not work for setters
                    Vec3F32 pos = spawner.position;
                         if (value.CaselessEq("here")) { pos = NonPreciseCoords(p); }
                    else if (value.CaselessEq("hereprecise")) { pos = PreciseCoords(p); }
                    else {
                        if (!GetCoords(p, values, 0, ref pos)) { return; }
                    }
                    spawner.positionPreserveRelativeOrigin = pos;
                    
                    
                    p.Message("Moved &f{0}&S to &f{1} {2} {3}&S.", spawner.name, spawner.x, spawner.y, spawner.z);
                    
                    break;
                case "dir":
                case "direction":
                    if (value == "") {
                        p.Message("&HProvide x y z difference for spawner direction.");
                        p.Message("&HOr type &bhere&H or &bhereprecise&H to make the direction towards yourself.");
                        p.Message("&HExample ( up ): &T/spawner edit {0} dir 0 1 0", spawner.name);
                        return;
                    }
                    
                    Vec3F32 dir = new Vec3F32(0,0,0);
                    
                         if (value.CaselessEq("here"))        { dir = NonPreciseCoords(p) - spawner.position; }
                    else if (value.CaselessEq("hereprecise")) { dir = PreciseCoords(p)    - spawner.position; }
                    else {
                        if (!GetCoords(p, values, 0, ref dir, "direction")) { return; }
                    }
                    
                    dir = dir - spawner.position;
                    
                    spawner.origin = new Vec3F32(-dir.X, -dir.Y, -dir.Z);
                    
                    string origin = FormatOrigin(spawner.x, spawner.y, spawner.z, spawner.originX, spawner.originY, spawner.originZ);
                    p.Message("Set &f{0}&S's direction to &f{1}&S.", spawner.name, origin);
                    
                    break;
                case "interval":
                    if (value == "") {
                        p.Message("&HProvide a number between 0 and {0} for spawner interval.", MAX_INTERVAL);
                        p.Message("&HInterval is how many ticks to wait between spawning.");
                        p.Message("&H0 is the fastest and spawns once every 10th of a second.");
                        return;
                    }
                    if (!CommandParser.GetInt(p, value, "interval", ref spawner.spawnInterval, 0, MAX_INTERVAL)) { return; }
                    p.Message("Set &f{0}&S's interval to &f{1}&S.", spawner.name, spawner.spawnInterval);
                    break;
                case "intervaloffset":
                    if (value == "") {
                        p.Message("&HProvide a number between 0 and {0} for spawner interval offset.", MAX_INTERVAL-1);
                        p.Message("&HInterval offset changes when an effect is spawned relative to other spawners.");
                        p.Message("&HThis value will make no difference when interval is 0.");
                        return;
                    }
                    if (!CommandParser.GetInt(p, value, "interval", ref spawner.spawnTimeOffset, 0, MAX_INTERVAL-1)) { return; }
                    p.Message("Set &f{0}&S's intervaloffset to &f{1}&S.", spawner.name, spawner.spawnTimeOffset);
                    break;
                case "chance":
                    if (value == "") {
                        p.Message("&HProvide a number between 0.0 and 100.0 for spawner chance.");
                        p.Message("&HThis controls how likely an effect is to spawn, allowing irregularly appearing effects.");
                        return;
                    }
                    float spawnChance = 100;
                    if (!CommandParser.GetReal(p, value, "chance", ref spawnChance, 0.01f, 100)) { return; }
                    spawnChance /= 100f; //convert percentage to 0-1
                    spawner.spawnChance = spawnChance;
                    p.Message("Set &f{0}&S's chance to &f{1}%&S.", spawner.name, spawner.spawnChance * 100);
                    break;
                default:
                    p.Message("&WThere is no spawner property \"{0}\"", prop);
                    EditHelp(p, spawner);
                    break;
            }
            GoodlyEffects.SpawnersFile.Save(p.level);
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
            
            GoodlyEffects.EffectSpawner spawner = GetSpawner(p, message);
            if (spawner == null) { return; }
            if (!spawner.EditableBy(p, "remove")) { return; }
            
            p.Message("Removed spawner {0}.", spawner.name);
            GoodlyEffects.RemoveSpawner(spawner, p.level, true);
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
            
            GoodlyEffects.EffectSpawner spawner = GetSpawner(p, message);
            if (spawner == null) { return; }
            
            Command.Find("tp").Use(p, "-precise "+(int)(spawner.x*32)+" "+(int)(spawner.y*32)+" "+(int)(spawner.z*32));
        }
        static void DoInfo(Player p, string message) {
            if (message == "") { p.Message("&WPlease provide the name of a spawner to get info from."); return; }
            if (!GoodlyEffects.spawnersAtLevel.ContainsKey(p.level)) {
                p.Message("There are no spawners in {0}&S to get info from", p.level.ColoredName);
                return;
            }
            
            GoodlyEffects.EffectSpawner spawner = GetSpawner(p, message);
            if (spawner == null) { return; }
            
            string origin = FormatOrigin(spawner.x, spawner.y, spawner.z, spawner.originX, spawner.originY, spawner.originZ);
            p.Message("Spawner &f{0}&S has:", spawner.name);
            p.Message("  Name: &f{0}", spawner.name);
            p.Message("  Effect: &f{0}", spawner.effectName);
            p.Message("  Owner: &f{0}", spawner.owner);
            p.Message("  Position (pos): (&f{0} {1} {2}&S)", spawner.x, spawner.y, spawner.z);
            p.Message("  Direction (dir): (&f{0}&S)", origin);
            p.Message("  Interval: &f{0}", spawner.spawnInterval);
            p.Message("  IntervalOffset: &f{0}", spawner.spawnTimeOffset);
            p.Message("  Chance: &f{0}%", spawner.spawnChance * 100); //convert to percentage
        }
        static void DoCopy(Player p, string message) {
            string[] args = message.SplitSpaces();
            if (args.Length != 2) {
                p.Message("&WPlease provide the name of a spawner to copy and the copy name.");
                p.Message("&7Use &a/spawners &7to list all spawners.");
                return;
            }
            string spawnerName = args[0];
            string cloneName = args[1];
            
            GoodlyEffects.EffectSpawner spawner = GetSpawner(p, spawnerName);
            if (spawner == null) { return; }
            
            if (SpawnerNameExists(p, cloneName)) { return; }
            
            GoodlyEffects.EffectSpawner clone = spawner.Clone(cloneName);
            clone.positionPreserveRelativeOrigin = NonPreciseCoords(p);
            GoodlyEffects.AddSpawner(clone, p.level, true);
            p.Message("Successfully cloned spawner &f{0}&S from &f{1}&S.", clone.name, spawner.name);
            p.Message("Use &b/spawner edit {0} &Sto change direction and more.", clone.name);
        }
        static string FormatOrigin(float X, float Y, float Z, float oX, float oY, float oZ) {
            float rX = oX - X;
            float rY = oY - Y;
            float rZ = oZ - Z;
            string a = (-rX).ToString();
            string b = (-rY).ToString();
            string c = (-rZ).ToString();
            return String.Format("{0} {1} {2}", a, b, c);
        }
        
        
        public override void Help(Player p) {
            p.Message("&T/Spawner add [name] [effect]");
            p.Message("&HAdds an effect spawner to the world.");
            p.Message("&T/Spawner edit [name] <prop> &H- edit/move a spawner.");
            EditHelp(p);
            p.Message("&HWhen editing spawner position or direction,");
            p.Message("&Hyou can use \"here\" or \"hereprecise\" for your position.");
            p.Message("&T/Spawner remove [name] &H- removes a spawner.");
            p.Message("&HIf [name] is \"all\", all spawners are removed.");
            p.Message("&T/Spawner tp [name] &H- teleports you to a spawner.");
            p.Message("&T/Spawner list &H- lists spawners in current level.");
            p.Message("&T/Spawner copy [name] [newName] &H- clones a spawner.");
        }
        static void EditHelp(Player p, GoodlyEffects.EffectSpawner spawner = null) {
            string name = (spawner == null) ? "[spawner]" : spawner.name;
            p.Message("&T/Spawner info {0} &H- show editable properties.", name);
        }

        static bool SpawnerNameExists(Player p, string name) {
            if (!GoodlyEffects.spawnersAtLevel.ContainsKey(p.level)) { return false; }
            foreach (GoodlyEffects.EffectSpawner spawner in GoodlyEffects.spawnersAtLevel[p.level]) {
                if (name.CaselessEq(spawner.name)) {
                    p.Message("&WA spawner named \"{0}\" already exists.", spawner.name);
                    return true;
                }
            }
            return false;
        }
        static int SpawnerCount(Level lvl) {
            if (!GoodlyEffects.spawnersAtLevel.ContainsKey(lvl)) { return 0; }
            return GoodlyEffects.spawnersAtLevel[lvl].Count;
        }
        
        static GoodlyEffects.EffectSpawner GetSpawner(Player p, string message) {
            if (!GoodlyEffects.spawnersAtLevel.ContainsKey(p.level)) {
                p.Message("There are no spawners in {0}&S.", p.level.ColoredName);
                return null;
            }
            
            int matches;
            GoodlyEffects.EffectSpawner spawner = Matcher.Find(p, message, out matches,
                                                                     GoodlyEffects.spawnersAtLevel[p.level],
                                                                     x => true,
                                                                     x => x.name,
                                                                     "effect spawners");
            if (matches > 1 || spawner == null) { p.Message("Use &T/spawners &Sto list all spawners."); return null; }
            return spawner;
        }
        
        static bool GetCoords(Player p, string[] args, int argsOffset, ref Vec3F32 P, string name = "position") {
            if (args.Length < 3 + argsOffset) { p.Message("&WNot enough arguments for {0}", name); return false; }
            return
                CommandParser.GetCoordFloat(p, args[argsOffset + 0], "X coordinate", ref P.X) &&
                CommandParser.GetCoordFloat(p, args[argsOffset + 1], "Y coordinate", ref P.Y) &&
                CommandParser.GetCoordFloat(p, args[argsOffset + 2], "Z coordinate", ref P.Z);
        }
        static Vec3F32 NonPreciseCoords(Player p) {
            Vec3F32 position = new Vec3F32(p.Pos.BlockX, p.Pos.FeetBlockCoords.Y, p.Pos.BlockZ);
            //default to center of block
            position.X += 0.5f;
            position.Y += 0.5f;
            position.Z += 0.5f;
            return position;
        }
        static Vec3F32 PreciseCoords(Player p) {
            return new Vec3F32((float)(p.Pos.X) / 32f, (float)(p.Pos.Y - Entities.CharacterHeight) / 32f, (float)(p.Pos.Z) / 32f);
        }
    }

}