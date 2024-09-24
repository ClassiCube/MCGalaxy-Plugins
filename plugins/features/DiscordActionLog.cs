using System;
using System.IO;

using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Events;
using MCGalaxy.Modules.Relay.Discord;

namespace MCGalaxy
{
    public class DiscordActionLog : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.4"; } }
        public override string name { get { return "DiscordActionLog"; } }

        public const string discordChannelID = ""; // The ID of the Discord channel to forward the message to
        public const string embedColour = ""; // Colour of the embed

        public override void Load(bool startup)
        {
            OnModActionEvent.Register(HandleModerationAction, Priority.Low);
        }

        public override void Unload(bool shutdown)
        {
            OnModActionEvent.Unregister(HandleModerationAction);
        }

        static void HandleModerationAction(ModAction action)
        {
            switch (action.Type)
            {
                case ModActionType.Frozen:
                    AddNote(action, "frozen"); break;
                case ModActionType.Kicked:
                    AddNote(action, "kicked"); break;
                case ModActionType.Muted:
                    AddNote(action, "muted"); break;
                case ModActionType.Warned:
                    AddNote(action, "warned"); break;
                case ModActionType.Ban:
                    string banType = action.Duration.Ticks != 0 ? "temp-banned" : "banned";
                    AddNote(action, banType); break;
            }
        }

        public static void EmbedReport(DiscordBot disc, string type, string title, string duration, string reason)
        {
            ChannelSendEmbed embed = new ChannelSendEmbed(discordChannelID);
            DiscordConfig config = DiscordPlugin.Config;

            embed.Color = Int32.Parse(embedColour);
            embed.Title = title;
            if (type == "frozen" || type == "temp-banned") embed.Fields.Add("Duration", duration);
            embed.Fields.Add("Reason", reason);

            disc.Send(embed);
        }

        static void AddNote(ModAction e, string type)
        {
            string title = e.Target + " was " + type + " by " + e.Actor.name + " at " + DateTime.UtcNow;

            string duration = e.Duration.Shorten(true);
            string reason = e.Reason.Length == 0 ? "No reason was specified." : e.Reason;

            DiscordBot discBot = DiscordPlugin.Bot;
            try
            {
                EmbedReport(discBot, type, title, duration, reason);
            }
            catch (Exception error)
            {
                return;
            }
        }
    }
}
