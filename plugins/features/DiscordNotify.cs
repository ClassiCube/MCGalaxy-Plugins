// You must set 'can-mention-roles = true' in './properties/discordbot.properties' in order to use this plugin.
// Please edit 'channelID' and 'roleID' below with your own IDs.
// NOTE: If you have a custom chat token that formats to an ampersand, it is possible for players to ping roles using the bot. Use at your own discretion in this case.
// Check out opapinguin's plugin if you want this to be automated: https://github.com/opapinguin/MCGalaxy-plugins/blob/main/ActivityBot.cs

using System;
using System.Collections.Generic;
using MCGalaxy.Modules.Relay.Discord;

namespace MCGalaxy
{
    public class DiscordNotify : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string name { get { return "DiscordNotify"; } }

        public static DateTime lastServerNotificationTime = DateTime.MinValue;

        public override void Load(bool startup)
        {
            Command.Register(new CmdNotify());
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Notify"));
        }
    }

    public sealed class CmdNotify : Command2
    {
        public override string name { get { return "Notify"; } }
        public override string shortcut { get { return "pingdiscord"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

        private string FormatTimeSpan(TimeSpan span)
        {
            return span.Hours + " hours, " + span.Minutes + " minutes, and " + span.Seconds + " seconds";
        }

        public override void Use(Player p, string message, CommandData data)
        {
            TimeSpan cooldownDuration = TimeSpan.FromHours(12);

            // Calculate the time remaining before the next notification is allowed
            TimeSpan timeRemaining = cooldownDuration - (DateTime.Now - DiscordNotify.lastServerNotificationTime);

            // Check if enough time has passed since the last server-wide notification
            if (timeRemaining > TimeSpan.Zero)
            {
                p.Message("You can send a notification in " + FormatTimeSpan(timeRemaining) + ".");
                return;
            }

            DiscordNotify.lastServerNotificationTime = DateTime.Now; // Update the last server-wide notification time

            string channelID = ""; // Insert the ID of the channel you wish to send the message to
            string roleID = ""; // Insert the ID of the role you wish to ping

            int playersOnline = 0;
            List<OnlineListEntry> all = PlayerInfo.GetOnlineList(p, data.Rank, out playersOnline);

            DiscordPlugin.Bot.Send(new ChannelSendMessage(channelID, "**" + p.truename + "**" + " is requesting players." +
                "\n**Players online:** " + playersOnline +
                "\n<@&" + roleID + ">"));
        }

        public override void Help(Player p) { }
    }
}
