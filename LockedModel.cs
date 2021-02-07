using System;
using MCGalaxy;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace PluginLockedModel {
    public sealed class Core : Plugin {
        public override string creator { get { return "Not UnknownShadow200"; } }
        public override string name { get { return "LockedModel"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.4"; } }
        
        public override void Load(bool startup) {
            OnSendingMotdEvent.Register(HandleGettingMOTD, Priority.Low);
            OnPlayerCommandEvent.Register(OnPlayerCommand, Priority.Low);
        }
        
        public override void Unload(bool shutdown) {
            OnSendingMotdEvent.Unregister(HandleGettingMOTD);
            OnPlayerCommandEvent.Unregister(OnPlayerCommand);
        }
        
        void HandleGettingMOTD(Player p, ref string motd) {
        	string[] models = GetLockedModels(motd);
            const string key = "US200.LockedModel.Model";
            // Model user had before joining a level with locked model
            string originalModel = p.Extras.GetString(key);
            
            if (models == null) {
                // Restore the model back to user's original model
                if (originalModel == null) return;
                p.Extras.Remove(key);
                p.ScaleX = (float)p.Extras.Get(key + "_X");
                p.ScaleY = (float)p.Extras.Get(key + "_Y");
                p.ScaleZ = (float)p.Extras.Get(key + "_Z");
                Entities.UpdateModel(p, originalModel);
                return;
            }
            
            
            string currentModel = p.Model;
            float curX = p.ScaleX, curY = p.ScaleY, curZ = p.ScaleZ;
            p.ScaleX = 0; p.ScaleY = 0; p.ScaleZ = 0;
            
            if (models.CaselessContains(p.Model)) {
                // Still want to reset X/Y/Z per axis model scaling
                Entities.UpdateModel(p, p.Model);
            } else {
                // Switch user to the level's locked model
                Entities.UpdateModel(p, models[0]);
            }

            // Don't overwrite model user had before joining a level with locked model
            if (originalModel != null) return;
            p.Extras.PutString(key, currentModel);
            p.Extras[key + "_X"] = curX;
            p.Extras[key + "_Y"] = curY;
            p.Extras[key + "_Z"] = curZ;
        }
        
        void OnPlayerCommand(Player p, string cmd, string args, CommandData data) {
            if (!cmd.CaselessEq("model")) return;
            if (args.CaselessStarts("bot ")) return; // using model on bot
            
            string[] models = GetLockedModels(p.GetMotd());
            if (models == null) return;
            
            if (!models.CaselessContains(args)) {
                p.Message("&cYou may only change your own model to: %S{0}", models.Join());
                p.cancelcommand = true;
            }
        }
        
        // reuse single instance to minimise mem allocations
        static char[] splitChars = new char[] { ',' };
        static string[] GetLockedModels(string motd) {
            // Does the motd have 'model=' in it?
            int index = motd.IndexOf("model=");
            if (index == -1) return null;
            motd = motd.Substring(index + "model=".Length);
            
            // Get the single word after 'model='
            if (motd.IndexOf(' ') >= 0)
                motd = motd.Substring(0, motd.IndexOf(' '));
            
            // Is there an actual word after 'model='?
            if (motd.Length == 0) return null;
            return motd.Split(splitChars);
        }
    }
}
