//reference System.dll
//reference System.Core.dll
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using MCGalaxy;
using MCGalaxy.Authentication;

namespace AuthPlugin
{
	public sealed class MicrosoftAuthenticationPlugin : Plugin
	{
		public override string name { get { return "MicrosoftAuthenticationPlugin"; } }
		public override string MCGalaxy_Version { get { return "1.9.3.6"; } }

		public override void Load(bool auto)
		{
			Authenticator.Current = new MicrosoftFallbackAuthenticator(Authenticator.Current);
		}

		public override void Unload(bool auto)
		{
			Authenticator.Current = ((MicrosoftFallbackAuthenticator)Authenticator.Current).underlyingAuthenticator;
		}
	}

    class MicrosoftFallbackAuthenticator : Authenticator
    {
		// underlyingAuthenticator is used to allow the plugin to wrap an already wrapped authenticator if there is one.
		// In addition, the default authenticator is sealed, and so cannot be inherited / extended.
		public readonly Authenticator underlyingAuthenticator;
		private readonly string externalIP; //TODO: Move this out.

		public MicrosoftFallbackAuthenticator(Authenticator underlyingAuthenticator)
        {
			this.underlyingAuthenticator = underlyingAuthenticator;
			externalIP = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
		}

        public override bool HasPassword(string name)
        {
			return underlyingAuthenticator.HasPassword(name);
        }

        public override bool ResetPassword(string name)
        {
			return underlyingAuthenticator.ResetPassword(name);
        }

        public override void StorePassword(string name, string password)
        {
			underlyingAuthenticator.StorePassword(name, password);
		}

        public override bool VerifyPassword(string name, string password)
        {
			return underlyingAuthenticator.VerifyPassword(name, password);
        }

		public override bool VerifyLogin(Player p, string mppass)
		{
			bool mppass_valid = base.VerifyLogin(p, mppass);
			if (mppass_valid) return true;

			string serverId = externalIP + ":" + Server.Config.Port;
			var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(serverId));
			serverId = string.Concat(hash.Select(b => b.ToString("x2")));

			// Check if the player has authenticated with Mojang's session server.
			if (!hasJoined(p.truename, serverId)) return false;

			p.verifiedName = true;
			return true;
		}

		private bool hasJoined(string username, string serverId)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://sessionserver.mojang.com/session/minecraft/hasJoined?username=" + username + "&serverId=" + serverId);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				return response.StatusCode == HttpStatusCode.OK;
			} catch (Exception ex)
            {
				Logger.LogError(ex);
            }

			return false;
		}
	}
}