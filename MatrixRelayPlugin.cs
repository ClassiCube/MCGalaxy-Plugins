using System;
using System.IO;
using System.Net;
using System.Text;
using MCGalaxy.Config;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Network;

namespace MCGalaxy.Modules.Relay.Matrix
{
	public sealed class MatrixConfig
	{
		[ConfigBool("enabled", "General", false)]
		public bool Enabled;
		[ConfigString("username", "General", "", true)]
		public string Username = "";
		[ConfigString("password", "General", "", true)]
		public string Password = "";
		[ConfigBool("use-nicknames", "General", true)]
		public bool UseNicks = true;
		
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
		string botUserID, token, host;
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
			
			string _user;
			// matrix users are in form of 'user_localhost:homeserver'
			Config.Username.Separate(':', out _user, out host);
			if (string.IsNullOrEmpty(host)) host = "matrix.org";
			
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
		
		protected override void DoReadLoop() {
			System.Threading.Thread.Sleep(60 * 1000);
		}
		
		protected override void DoDisconnect(string reason) {
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
			string url = "https://" + host + "/_matrix/client/v3" + msg.Path;
			WebResponse res;
			
			HttpWebRequest req = HttpUtil.CreateRequest(url);
			req.Method         = msg.Method;
			req.ContentType    = "application/json";
			
			if (token != null) {
				req.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
			}
			
			string data = Json.SerialiseObject(msg.ToJson());
			HttpUtil.SetRequestData(req, Encoding.UTF8.GetBytes(data));
			res = req.GetResponse();
			
			string resp = HttpUtil.GetResponseText(res);
			return new JsonReader(resp).Parse();
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
		
		public RoomSendMessage(string roomID, string message) {
			Path    = "/rooms/" + roomID + "/send/m.room.message/";
			content = message;
			Method  = "PUT";
		}
		
		public override JsonObject ToJson() {
			return new JsonObject()
			{
				{ "msgtype", "m.text" },
				{ "body", content }
			};
		}
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
