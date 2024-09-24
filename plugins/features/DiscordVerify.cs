using System;
using System.IO;

using MCGalaxy;
using MCGalaxy.Config;
using MCGalaxy.Events;
using MCGalaxy.Modules.Relay;
using MCGalaxy.Modules.Relay.Discord;

namespace Core
{
    public class DiscordVerify : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.1"; } }
        public override string name { get { return "DiscordVerify"; } }

        public string serverID = ""; // The ID of the server
        public bool giveRole = false; // Whether the player will be given a role (true) or not (false) upon verifying
        public string roleID = ""; // The ID of the role to give if giveRole is true
        public bool changeNick = false; // Whether the player's nick will be forcefully changed to their username (true) or not (false) upon verifying

        public static PlayerExtList verified;

        public override void Load(bool startup)
        {
            if (!Directory.Exists("plugins/DiscordVerify")) Directory.CreateDirectory("plugins/DiscordVerify");
            // If the directory does not exist, then we will just create it.
            if (!File.Exists("plugins/DiscordVerify/verified.txt")) File.Create("plugins/DiscordVerify/verified.txt");
            // If the file does not exist, then we will also create that file. We don't want our user to do it manually.
            verified = PlayerExtList.Load("plugins/DiscordVerify/verified.txt");
            // Purpose is not to have it break.
            OnChannelMessageEvent.Register(HandleDiscordMessage, Priority.High);

            Command.Register(new CmdVerify());
        }

        public override void Unload(bool shutdown)
        {
            OnChannelMessageEvent.Unregister(HandleDiscordMessage);

            Command.Unregister(Command.Find("Verify"));
        }

        void HandleDiscordMessage(RelayBot bot, string channel, RelayUser p, string message, ref bool cancel)
        {
            if (bot != DiscordPlugin.Bot) return; // Don't want it enabled for IRC
            if (!message.StartsWith(".verify ")) return;

            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.Extras.GetString("DISCORD_VERIFICATION_CODE") == message.Split(' ')[1])
                {
                    string data = verified.FindData(pl.truename);

                    if (data == null)
                    {
                        // Give verified role on Discord
                        if (giveRole)
                        {
                            DiscordApiMessage msg = new AddGuildMemberRole(serverID, p.ID, roleID);
                            DiscordPlugin.Bot.Send(msg);
                        }

                        // Change nickname on Discord
                        // NOTE: Does not work on the guild's owner. See: https://github.com/discord/discord-api-docs/issues/2139
                        if (changeNick)
                        {
                            DiscordApiMessage msg = new ChangeNick(serverID, p.ID, pl.name);
                            DiscordPlugin.Bot.Send(msg);
                        }

                        verified.Update(pl.truename, p.ID);
                        verified.Save();

                        pl.Extras.Remove("DISCORD_VERIFICATION_CODE");

                        pl.Message("%aAccount successfully linked with ID %b" + p.ID + "%a.");
                    }
                }
            }

            cancel = true;
        }
    }

    class AddGuildMemberRole : DiscordApiMessage
    {
        public override JsonObject ToJson() { return new JsonObject(); }

        public AddGuildMemberRole(string serverID, string userID, string roleID)
        {
            const string format = "/guilds/{0}/members/{1}/roles/{2}";
            Path = string.Format(format, serverID, userID, roleID);
            Method = "PUT";
        }
    }

    class ChangeNick : DiscordApiMessage
    {
        public string Nick;

        public override JsonObject ToJson()
        {
            return new JsonObject()
            {
                { "nick", Nick }
            };
        }

        public ChangeNick(string serverID, string userID, string nick)
        {
            const string fmt = "/guilds/{0}/members/{1}";
            Path = string.Format(fmt, serverID, userID);
            Method = "PATCH";
            Nick = nick;
        }
    }

    public sealed class CmdVerify : Command2
    {
        public override string name { get { return "Verify"; } }
        public override string type { get { return "information"; } }

        public override void Use(Player p, string message)
        {
            string data = DiscordVerify.verified.FindData(p.truename);

            if (data != null)
            {
                p.Message("%SYour account has already been verified.");
                return;
            }

            string code = GetVerificationCode(8);

            p.Message("%SType %b.verify " + code + " %Son Discord to link your account.");
            p.Extras["DISCORD_VERIFICATION_CODE"] = code;
        }

        public static string GetVerificationCode(int length)
        {
            char[] chArray = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            string str = string.Empty;
            Random random = new Random();
            for (int i = 0; i < length; i++)
            {
                int index = random.Next(1, chArray.Length);
                if (!str.Contains(chArray.GetValue(index).ToString()))
                {
                    str = str + chArray.GetValue(index);
                }
                else
                {
                    i--;
                }
            }
            return str;
        }

        public override void Help(Player p)
        {
            p.Message("%T/Verify %H- Generates a random code.");
            p.Message("%HType %b.verify [code] %Hon Discord to verify your account.");
        }
    }
}