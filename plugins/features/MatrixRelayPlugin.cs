using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using MCGalaxy;
using MCGalaxy.Config;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Modules.Relay;
using MCGalaxy.Network;

namespace MatrixRelay
{
	public sealed class MatrixConfig
	{
		[ConfigBool("enabled", "General", false)]
		public bool Enabled;
		[ConfigString("username", "General", "", true)]
		public string Username = "";
		[ConfigString("password", "General", "", true)]
		public string Password = "";
		[ConfigString("password", "General", "https://matrix.org", false)]
		public string ServerAddress = "https://matrix.org";
		
		[ConfigString("rooms", "General", "", true)]
		public string Rooms = "";
		[ConfigString("op-rooms", "General", "", true)]
		public string OpRooms = "";
		[ConfigString("ignored-user-ids", "General", "", true)]
		public string IgnoredUsers = "";
		
		public const string PROPS_PATH = "properties/matrixbot.properties";
		static ConfigElement[] cfg;
		
		public void Load() {
			// create default config file
			if (!File.Exists(PROPS_PATH)) Save();

			if (cfg == null) cfg = ConfigElement.GetAll(typeof(MatrixConfig));
			ConfigElement.ParseFile(cfg, PROPS_PATH, this);
		}
		
		public void Save() {
			if (cfg == null) cfg = ConfigElement.GetAll(typeof(MatrixConfig));
			
			using (StreamWriter w = new StreamWriter(PROPS_PATH)) {
				ConfigElement.Serialise(cfg, w, this);
			}
		}
	}
	
	public sealed class MatrixPlugin : Plugin
	{
		public override string name { get { return "MatrixRelay"; } }
		
		public static MatrixConfig Config = new MatrixConfig();
		public static MatrixBot Bot = new MatrixBot();
		
		public override void Load(bool startup) {
			try { Directory.CreateDirectory("text/Matrix"); } catch { }
			
			Bot.Config = Config;
			Bot.ReloadConfig();
			Bot.Connect();
			OnConfigUpdatedEvent.Register(OnConfigUpdated, Priority.Low);
		}
		
		public override void Unload(bool shutdown) {
			OnConfigUpdatedEvent.Unregister(OnConfigUpdated);
			Bot.Disconnect("Disconnecting Matrix bot");
		}
		
		void OnConfigUpdated() { Bot.ReloadConfig(); }
	}
	
	public sealed class CmdMatrixBot : RelayBotCmd
	{
		public override string name { get { return "MatrixBot"; } }
		protected override RelayBot Bot { get { return MatrixPlugin.Bot; } }
	}
	
	public sealed class CmdMatrixControllers : BotControllersCmd
	{
		public override string name { get { return "MatrixControllers"; } }
		protected override RelayBot Bot { get { return MatrixPlugin.Bot; } }
	}

	public sealed class MatrixBot : RelayBot
	{
		string botUserID, token;
		public override string RelayName { get { return "Matrix"; } }
		public override bool Enabled     { get { return Config.Enabled; } }
		public override string UserID    { get { return botUserID; } }
		public MatrixConfig Config;
		MatrixMsgSender sender;
		
		
		protected override bool CanReconnect {
			get { return canReconnect; }
		}
		
		protected override void DoConnect() {
			token = null;

			LoginMessage msg = new LoginMessage(Config.Username, Config.Password);
			object value     = MakeRequest(msg);
			ParseLoginResponse(value);
			OnReady();
			
			sender     = new MatrixMsgSender();
			sender.Bot = this;
			sender.RunAsync();
		}
		
		void ParseLoginResponse(object value) {
			JsonObject obj = (JsonObject)value;
			botUserID = (string)obj["user_id"];
			token     = (string)obj["access_token"];
		}
		
		bool running;
		protected override void DoReadLoop() {
			running = true;
			string since = null;
			
			while (running)
			{
				object value = MakeRequest(new SyncMessage(since));
				ParseSyncResponse(value, ref since);
				Thread.Sleep(10 * 1000);
			}
		}
		
		protected override void DoDisconnect(string reason) {
			running = false;
		}
		
		
		void ParseSyncResponse(object value, ref string since) {
			JsonObject obj = value as JsonObject;
			if (obj == null) return;
			
			obj.TryGetValue("next_batch", out value);
			if (value != null) since = (string)value;
			
			obj.TryGetValue("rooms", out value);
			if (value != null) ParseSyncRooms(value);
		}
		
		void ParseSyncRooms(object value) {
			JsonObject groups = value as JsonObject;
			if (groups == null) return;
			
			foreach (var group in groups)
			{
				if (group.Key != "join") continue;
				JsonObject rooms = group.Value as JsonObject;
				if (rooms == null) continue;
				
				foreach (var room in rooms)
				{
					ParseSyncRoom(room.Key, room.Value);
				}
			}
		}
		
		void ParseSyncRoom(string roomID, object value) {
			JsonObject props = value as JsonObject;
			if (props == null) return;
			
			foreach (var kvp in props)
			{
				if (kvp.Key != "timeline") continue;
				JsonObject timeline = kvp.Value as JsonObject;
				if (timeline == null) continue;
				
				timeline.TryGetValue("events", out value);
				JsonArray events = value as JsonArray;
				if (events == null) continue;
				
				foreach (object event_ in events)
				{
					ParseSyncEvent(roomID, event_);
				}
			}
		}
		
		void ParseSyncEvent(string roomID, object value) {
			JsonObject event_ = value as JsonObject;
			if (event_ == null) return;
			string host;
			
			if (!event_.TryGetValue("type", out value)) return;
			if ((string)value != "m.room.message")      return;
			
			if (!event_.TryGetValue("content", out value)) return;
			JsonObject content = value as JsonObject;
			if (content == null) return;
			
			RelayUser user = new RelayUser();
			if (!event_.TryGetValue("sender", out value)) return;
			user.ID   = (string)value;
			user.ID.Separate(':', out user.Nick, out host); // TODO nicks
			user.Nick = user.Nick.Replace("@", "");
			
			if (!content.TryGetValue("body", out value)) return;
			HandleChannelMessage(user, roomID, (string)value);
		}
		
		
		public override void ReloadConfig() {
			Config.Load();
			base.ReloadConfig();
		}
		
		protected override void UpdateConfig() {
			Channels     = Config.Rooms.SplitComma();
			OpChannels   = Config.OpRooms.SplitComma();
			IgnoredUsers = Config.IgnoredUsers.SplitComma();

			LoadBannedCommands();
		}
		
		public override void LoadControllers() {
			Controllers = PlayerList.Load("text/Matrix/controllers.txt");
		}

		
		protected override string ParseMessage(string input) {
			StringBuilder sb = new StringBuilder(input);
			SimplifyCharacters(sb);
			return sb.ToString();
		}
		
		
		protected override void OnStart() {
			base.OnStart();
		}
		
		protected override void OnStop() {
			if (sender != null) {
				sender.StopAsync();
				sender = null;
			}
			base.OnStop();
		}

		
		/// <summary> Asynchronously sends a message to the matrix API </summary>
		public void Send(MatrixApiMessage msg) {
			if (sender != null) sender.QueueAsync(msg);
		}
		
		protected override void DoSendMessage(string channel, string message) {
			message = ConvertMessage(message);
			RoomSendMessage msg = new RoomSendMessage(channel, message);
			Send(msg);
		}
		
		
		/// <summary> Formats a message for displaying on Matrix </summary>
		string ConvertMessage(string message) {
			message = ConvertMessageCommon(message);
			message = Colors.StripUsed(message);
			return message;
		}
		
		protected override string PrepareMessage(string message) {
			return message;
		}
		
		
		// all users are already verified by Matrix
		protected override bool CheckController(string userID, ref string error) { return true; }
		
		
		public object MakeRequest(MatrixApiMessage msg) {
			string url = Config.ServerAddress + "/_matrix/client/v3" + msg.Path;

			HttpWebRequest req = HttpUtil.CreateRequest(url);
			req.Method         = msg.Method;
			if (token != null) req.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
			
			object body = msg.ToJson();
			if (body != null) {
				req.ContentType = "application/json";
				string data     = Json.SerialiseObject(body);
				HttpUtil.SetRequestData(req, Encoding.UTF8.GetBytes(data));
			}
			
			WebResponse res = req.GetResponse();
			string response = HttpUtil.GetResponseText(res);
			return new JsonReader(response).Parse();
		}
	}
	
	public abstract class MatrixApiMessage
	{
		public string Path;
		public string Method = "POST";
		public abstract JsonObject ToJson();
	}
	
	class LoginMessage : MatrixApiMessage
	{
		public string Username, Password;
		
		public LoginMessage(string username, string password) {
			Username = username;
			Password = password;
			Path     = "/login";
		}
		
		public override JsonObject ToJson() {
			return new JsonObject()
			{
				{ "type", "m.login.password" },
				{ "device_id", "MCG_RELAY_BOT" },
				{ "initial_device_display_name", Server.SoftwareName + " relay bot" },
				{ "identifier", new JsonObject()
					{
						{ "type", "m.id.user" },
						{ "user", Username },
					}
				},
				{ "password", Password }
			};
		}
	}
	
	class RoomSendMessage : MatrixApiMessage
	{
		string content;
		static int idCounter;
		
		public RoomSendMessage(string roomID, string message) {
			int txnID = Interlocked.Increment(ref idCounter);
			Path      = "/rooms/" + roomID + "/send/m.room.message/" + txnID;
			content   = message;
			Method    = "PUT";
		}
		
		public override JsonObject ToJson() {
			return new JsonObject()
			{
				{ "msgtype", "m.text" },
				{ "body", content }
			};
		}
	}
	
	class SyncMessage : MatrixApiMessage
	{
		public SyncMessage(string since) {
			Method = "GET";
			Path   = "/sync";
			if (since != null) Path += "?since=" + since;
		}
		
		public override JsonObject ToJson() { return null; }
	}
	
	sealed class MatrixMsgSender : AsyncWorker<MatrixApiMessage>
	{
		public MatrixBot Bot;
		
		MatrixApiMessage GetNextRequest() {
			if (queue.Count == 0) return null;
			return queue.Dequeue();
		}
		
		protected override string ThreadName { get { return "Matrix-ApiClient"; } }
		protected override void HandleNext() {
			MatrixApiMessage msg = null;
			
			lock (queueLock) { msg = GetNextRequest(); }
			if (msg == null) { WaitForWork(); return;  }
			
			try {
				object value = Bot.MakeRequest(msg);
			} catch (WebException ex) {
				string err = HttpUtil.GetErrorResponse(ex);
				HttpUtil.DisposeErrorResponse(ex);
				
				LogError(ex, msg);
				LogResponse(err);
			} catch (Exception ex) {
				LogError(ex, msg);
			}
		}
		
		static void LogError(Exception ex, MatrixApiMessage msg) {
			string target = "(" + msg.Method + " " + msg.Path + ")";
			Logger.LogError("Error sending request to Matrix API " + target, ex);
		}
		
		static void LogResponse(string err) {
			if (string.IsNullOrEmpty(err)) return;
			Logger.Log(LogType.Warning, "Matrix API returned: " + err);
		}
	}
}
