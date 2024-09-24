using System.Collections.Generic;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy
{
    public class High5Consent : Plugin
    {
        public override string name { get { return "High5Consent"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string creator { get { return "Venk"; } }

        Command oldCommand = null;

        public override void Load(bool startup)
        {
            oldCommand = Command.Find("High5");
            if (oldCommand != null) Command.Unregister(oldCommand); // Unregister the default command.
            Command.Register(new CmdModifiedHigh5()); // Register the new command to override the default command's behaviour.

            OnPlayerDisconnectEvent.Register(HandlePlayerDisconnect, Priority.Low);
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("High5")); // Unregister the new command.
            if (oldCommand != null) Command.Register(oldCommand); // Readd the default command.
            OnPlayerDisconnectEvent.Unregister(HandlePlayerDisconnect);
        }

        private void HandlePlayerDisconnect(Player p, string reason)
        {
            CmdModifiedHigh5.RemoveHigh5Request(p);

            // Find and remove any requests where the disconnected player was the target.
            foreach (var kvp in new Dictionary<Player, Player>(CmdModifiedHigh5.high5Requests))
            {
                if (kvp.Value == p)
                {
                    CmdModifiedHigh5.RemoveHigh5Request(kvp.Key);
                }
            }
        }
    }

    public sealed class CmdModifiedHigh5 : Command2
    {
        public override string name { get { return "High5"; } }
        public override string type { get { return "fun"; } } // There is a reason "fun" is in quotation marks...
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }

        public override void Help(Player p)
        {
            p.Message("&T/High5 [player]");
            p.Message("&HSends a high-five request OR consents to a high-five request.");
        }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0)
            {
                Help(p);
                return;
            }

            string[] args = message.SplitSpaces();
            Player partner = PlayerInfo.FindMatches(p, args[0]);
            if (partner == null) return;

            if (partner == p)
            {
                p.Message("&SDon't be lame...");
                return;
            }

            if (IsHigh5Pending(partner, p))
            {
                // Both players have consented.
                Chat.MessageFrom(p, partner.DisplayName + " &Sand " + p.DisplayName + " &Sjust high-fived.");
                RemoveHigh5Request(partner);
                RemoveHigh5Request(p);
                return;
            }

            if (IsHigh5Pending(p, partner))
            {
                // Already requested by the player.
                p.Message("&SYou have already requested to high-five " + partner.DisplayName + "&S. Waiting for their response...");
                return;
            }

            high5Requests[p] = partner;
            p.Message("&SYou have requested to high-five " + partner.DisplayName + "&S. Waiting for their response...");
            partner.Message(p.DisplayName + " &Shas requested to high-five you. Type &a/high5 " + p.name + " &Sto confirm.");
        }

        public static Dictionary<Player, Player> high5Requests = new Dictionary<Player, Player>();

        public static void RemoveHigh5Request(Player p)
        {
            if (high5Requests.ContainsKey(p))
            {
                high5Requests.Remove(p);
            }
        }

        public static bool IsHigh5Pending(Player p1, Player p2)
        {
            Player requestedPlayer;
            return high5Requests.TryGetValue(p1, out requestedPlayer) && requestedPlayer == p2;
        }
    }
}
