//reference System.dll
//reference System.Net.dll
using System;
using System.Net;

using MCGalaxy;
using MCGalaxy.Commands.Moderation;
using MCGalaxy.Config;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events;
using MCGalaxy.Network;

namespace Core {
    public class LocationJoin : Plugin {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }   
        public override string name { get { return "LocationJoin"; } }
        
        class GeoInfo {
            [ConfigString] public string region;
            [ConfigString] public string country;
        }
        static ConfigElement[] elems;

        public override void Load(bool startup) {
            OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.High);
        }

        public override void Unload(bool shutdown) {
            OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
        }

        void HandlePlayerConnect(Player p) {
            string ip = p.ip;
            if (ip == null) return;
            
            if (IPUtil.IsPrivate(IPAddress.Parse(ip))) {
                p.Message("&WPlayer has an internal IP, cannot trace"); return;
            }

            string json;
            try {
                WebRequest req  = HttpUtil.CreateRequest("http://ipinfo.io/" + ip + "/geo");
                WebResponse res = req.GetResponse();
                json = HttpUtil.GetResponseText(res);
            } catch (Exception ex) {
                HttpUtil.DisposeErrorResponse(ex);
                throw;
            }
            
            JsonReader reader = new JsonReader(json);
            JsonObject obj    = (JsonObject)reader.Parse();
            if (obj == null) { p.Message("&WError parsing GeoIP info"); return; }
            
            object region = null, country = null;
            obj.TryGetValue("region",   out region);
            obj.TryGetValue("country", out country);           
            
            p.Message("&2" + p.truename + " comes from " + country + "!");
        }
    }
}
