//reference System.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MCGalaxy;
using MCGalaxy.Config;
using MCGalaxy.Network;

namespace PluginSecureSocket
{
	public sealed class SecureSocketPlugin : Plugin
	{
		public override string name { get { return "SecureSocketPlugin"; } }
		public override string MCGalaxy_Version { get { return "1.9.4.0"; } }
		public static X509Certificate2 Cert;
		const string SETTINGS_FILE = "properties/ssl.properties";
		
		public override void Load(bool startup) {
			if (!File.Exists(SETTINGS_FILE)) SaveDefault();
			
			SSLConfig settings  = new SSLConfig();
			ConfigElement[] cfg = ConfigElement.GetAll(typeof(SSLConfig));
			ConfigElement.ParseFile(cfg, SETTINGS_FILE, settings);
			
			// UGLY HACK I don't know what this file should even contain??? seems you need public and private key
			Cert = new X509Certificate2(settings.CertPath, settings.CertPath);
			INetSocket.Protocols[0x16] = ConstructSecureWebsocket;
		}
		
		public override void Unload(bool shutdown) {
			INetSocket.Protocols[0x16] = null;
		}
		
		static INetProtocol ConstructSecureWebsocket(INetSocket socket) {
			if (!Server.Config.WebClient) return null;
			return new SecureSocket(socket);
		}
		
		static void SaveDefault() {
			using (StreamWriter w = new StreamWriter(SETTINGS_FILE))
			{
				w.WriteLine("# Path to certificate file (must contain both public and private key)");
				w.WriteLine("certificate-path = ");
				w.WriteLine();
				w.WriteLine("# Password/Passphrase of the certificate ");
				w.WriteLine("certificate-pass = "); 
			}
		}
	}
	
	sealed class SSLConfig
	{
		[ConfigString("certificate-path", "Other", "", true)]
		public string CertPath = "";
		[ConfigString("certificate-password", "Other", "", true)]
		public string CertPass = "";
	}

	// This code is unfinished and experimental, and is terrible quality. I apologise in advance.
	sealed class SecureSocket : INetSocket, INetProtocol 
	{
		readonly INetSocket raw;
		WrapperStream wrapper;
		SslStream ssl;
		
		public SecureSocket(INetSocket socket) {
			raw = socket;
			
			wrapper = new WrapperStream();
			wrapper.s = this;
			
			ssl = new SslStream(wrapper);
			new Thread(IOThread).Start();
		}
		
		// Init taken care by underlying socket
		public override void Init() { }
		public override IPAddress IP { get { return raw.IP; } }
		public override bool LowLatency { set { raw.LowLatency = value; } }
		public override void Close() { raw.Close(); }
		public void Disconnect() { Close(); }
		
		// This is an extremely UGLY HACK
		readonly object locker = new object();
		public override void Send(byte[] buffer, SendFlags flags) {
			try {
				lock (locker) ssl.Write(buffer);
			} catch (Exception ex) {
				Logger.LogError("Error writing to secure stream", ex);
			}
		}
		
		public int ProcessReceived(byte[] data, int count) {
			lock (wrapper.locker) {
				for (int i = 0; i < count; i++) {
					wrapper.input.Add(data[i]);
				}
			}
			return count;
		}
		
		void IOThread() {
			try {
				ssl.AuthenticateAsServer(SecureSocketPlugin.Cert);
				
				byte[] buffer = new byte[4096];
				for (;;) {
					int read = ssl.Read(buffer, 0, 4096);
					if (read == 0) break;
					this.HandleReceived(buffer, read);
				}
			} catch (Exception ex) {
				Logger.LogError("Error reading from secure stream", ex);
			}
		}
		
		// UGLY HACK because can't derive from two base classes
		sealed class WrapperStream : Stream 
		{
			public SecureSocket s;
			public readonly object locker = new object();
			public readonly List<byte> input = new List<byte>();
			
			public override bool CanRead { get { return true; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return true; } }
			
			static Exception ex = new NotSupportedException();
			public override void Flush() { }
			public override long Length { get { throw ex; } }
			public override long Position { get { throw ex; } set { throw ex; } }
			public override long Seek(long offset, SeekOrigin origin) { throw ex; }
			public override void SetLength(long length) { throw ex; }
			
			public override int Read(byte[] buffer, int offset, int count) {
				// UGLY HACK wait until got some data
				for (;;) {
					lock (locker) { if (input.Count > 0) break; }
					Thread.Sleep(1);
				}
				
				// now actually output the data
				lock (locker) {
					count = Math.Min(count, input.Count);
					for (int i = 0; i < count; i++) {
						buffer[offset++] = input[i];
					}
					input.RemoveRange(0, count);
				}
				return count;
			}
			
			public override void Write(byte[] buffer, int offset, int count) {
				byte[] data = new byte[count];
				Buffer.BlockCopy(buffer, offset, data, 0, count);
				s.raw.Send(data, SendFlags.None);
			}
		}
	}
}
