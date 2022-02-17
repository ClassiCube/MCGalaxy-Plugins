//reference System.dll
//reference System.Core.dll
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using MCGalaxy;
using MCGalaxy.Authentication;
using MCGalaxy.Network;

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
		string externalIP;

		public MicrosoftFallbackAuthenticator(Authenticator underlyingAuthenticator)
		{
			this.underlyingAuthenticator = underlyingAuthenticator;
			GetExternalIP();
		}

		void GetExternalIP()
		{
			if (externalIP != null) return;

			try
			{
				externalIP = new WebClient().DownloadString("http://ipv4.icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
			}
			catch (Exception ex)
			{
				Logger.LogError("Retrieving external IP", ex);
			}
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
			bool mppass_valid = underlyingAuthenticator.VerifyLogin(p, mppass);
			if (mppass_valid) return true;
			GetExternalIP();

			string serverId = externalIP + ":" + Server.Config.Port;
			var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(serverId));
			serverId = string.Concat(hash.Select(b => b.ToString("x2")));

			// Check if the player has authenticated with Mojang's session server.
			if (!HasJoined(p.truename, serverId)) return false;

			p.verifiedName = true;
			return true;
		}

		bool HasJoined(string username, string serverId)
		{
			string url = "https://sessionserver.mojang.com/session/minecraft/hasJoined?username=" + username + "&serverId=" + serverId;
			try
			{
				HttpWebRequest request   = HttpUtil.CreateRequest(url);
				request.Timeout = 10 * 1000; // give up after 10 seconds

				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				return response.StatusCode == HttpStatusCode.OK;
			} 
			catch (Exception ex)
			{
				HttpUtil.DisposeErrorResponse(ex);
				Logger.LogError("Verifying Mojang session", ex);
			}

			return false;
		}
	}
}
