//reference System.Net.dll
//reference System.dll
//reference Newtonsoft.Json.dll

// You will need to put your channel's webhook URL in line 32

using System;
using System.IO;
using System.Net;
using MCGalaxy;
using Newtonsoft.Json;

namespace Core {
	public class BugWebhook : Plugin {
        
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
        public override string name { get { return "BugWebhook"; } }

        public override void Load(bool startup) {
        	HookLogger();
        }

        public override void Unload(bool shutdown) {}
        
        void HookLogger() {
		    Logger.LogHandler += LogMessage;
		}
		
        static void LogMessage(LogType type, string error) {
            if (type != LogType.Error) return;
            string webhookUrl = "";
            string message = "**Server:** `" + Server.Config.Name + "` \n```\n" + error + "```";
            try { sendRequest(webhookUrl, message); } catch {}
        }

        public static void sendRequest(string URL, string msg) {
     		using (DiscordWeb dcWeb = new DiscordWeb()) {
         		dcWeb.ProfilePicture = "https://i.imgur.com/GDNEv05.png";
         		dcWeb.UserName = "New Bug:";
         		dcWeb.WebHook = URL;
         		dcWeb.SendMessage(msg);
      		}
   		}
    }
	
	public class DiscordWeb : IDisposable {
        readonly WebClient wc;
        public string WebHook, UserName, ProfilePicture;
        
        sealed class DiscordMessage {
        	public string username;
        	public string avatar_url;
        	public string content;
        }

        public DiscordWeb() {
            wc = new WebClient();
        }

        string ToJson(string message) {
        	DiscordMessage msg = new DiscordMessage();
        	msg.username = UserName;
        	msg.avatar_url = ProfilePicture;
        	msg.content = message;
			return JsonConvert.SerializeObject(msg);
        }

		void LogFailure(WebException ex) {
			try {
				string msg = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				Logger.Log(LogType.Warning, "Error sending Discord webhook: " + msg);
			} catch {
			}
		}
        
        public void SendMessage(string msgSend) {
			wc.Headers[HttpRequestHeader.ContentType] = "application/json";
			try {
	        		wc.UploadString(WebHook, ToJson(msgSend));
			} catch (WebException ex) {
				LogFailure(ex);
				throw;
			}
        }

        public void Dispose() {
            wc.Dispose();
        }
    }
}
