using System;
using MCGalaxy;
using MCGalaxy.Config;
using MCGalaxy.Modules.Relay;
using MCGalaxy.Modules.Relay.Discord;
using MCGalaxy.Tasks;

namespace Core
{
    public class DiscordChannelName : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.1"; } }
        public override string name { get { return "DiscordChannelName"; } }

        public static string channelID = ""; // The ID of channel to rename

        public static SchedulerTask task;

        public override void Load(bool startup)
        {
            // Discord limits changing of channel names to twice every 10 minutes
            Server.MainScheduler.QueueRepeat(DoLoop, null, TimeSpan.FromMinutes(5));
        }

        public override void Unload(bool shutdown)
        {
            Server.MainScheduler.Cancel(task);
        }

        public static void DoLoop(SchedulerTask t)
        {
            task = t;

            Player[] online = PlayerInfo.Online.Items;

            string name = "Players online: " + online.Length;

            // Send API message and edit channel name
            DiscordApiMessage msg = new UpdateChannelName(channelID, name);
            DiscordPlugin.Bot.Send(msg);
        }
    }

    public class UpdateChannelName : DiscordApiMessage
    {
        string _name;

        public UpdateChannelName(string channelID, string name)
        {
            Path = "/channels/" + channelID;
            Method = "PATCH";
            _name = name;
        }

        public override JsonObject ToJson()
        {
            return new JsonObject()
            {
                { "name", _name }
            };
        }
    }
}
