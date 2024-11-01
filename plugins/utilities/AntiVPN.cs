//reference System.dll

// Credit to UnknownShadow200 for some of the code
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using MCGalaxy;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy {
    public class AntiVPN : Plugin {
		public static PlayerList whitelist;
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.0"; } }
        public override string name { get { return "AntiVPN"; } }
		
        static Dictionary<string, byte> ipList = new Dictionary<string, byte>();
        static readonly object ipLock = new object();
		
        public override void Load(bool startup) {
			whitelist = PlayerList.Load("extra/vpnwhitelist.txt");
            OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.Low);
        }
		
        public override void Unload(bool shutdown) {
            OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
        }
		      
        void HandlePlayerConnect(Player p) {
	    	if (AntiVPN.whitelist.Contains(p.truename)) return;
            byte state = 0;

            lock (ipLock) {
                if (!ipList.TryGetValue(p.ip, out state)) {
                    ipList[p.ip] = 0;
                    Thread checker = new Thread(CheckIpAsync);
                    checker.IsBackground = true;
                    checker.Start(p.ip);
                }
            }

            if (state == 0) return;

            DoKick(p);	
        }
        
        static void CheckIpAsync(object arg) {
            string ip = (string)arg;
            try {
                string result = null;
                using (WebClient client = new WebClient()) {
                    string url = "http://check.getipintel.net/check.php?ip=" + arg + "&contact=fakeemailacc0unt1234521@gmail.com&flags=m";
                    result = client.DownloadString(url);
				        }
				
                float score = 0;
                if (!float.TryParse(result, out score)) return;
                if (score < 0.99) return;

                // Kick any players online with this IP
                Player[] players = PlayerInfo.Online.Items;
                foreach (Player p in players) {
                    if (p.ip != ip) continue;
                    DoKick(p);
                }
				
				        lock (ipLock) ipList[ip] = 1;
            }
            
            catch (Exception ex) {
                Logger.LogError(ex);
            }
        }
		
        static void DoKick(Player p) {
            p.Kick("&cProxy IPs are not allowed here.");
            Server.s.Log("&cWARNING: &S" + p.truename + " tried connecting from a proxy IP");	
        }
    }
}
