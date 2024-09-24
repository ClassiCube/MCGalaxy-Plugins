using System;
using MCGalaxy;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy
{
    public delegate void OnCustomTrigger(Player p, string message); // Add more parameters if you wish

    /// <summary> Describe how your event is triggered here. </summary>
    public sealed class OnCustomTriggerEvent : IEvent<OnCustomTrigger>
    {
        public static void Call(Player p, string message)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p, message));
        }
    }

    public class CustomEventExample : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.6"; } }
        public override string name { get { return "CustomEventExample"; } }

        public override void Load(bool startup)
        {
            OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.Low); // Use player connect event as an example to trigger our custom OnCustomTrigger event
        }

        void HandlePlayerConnect(Player p)
        {
            // Place this line of code somewhere in your plugin to trigger the event. I am just using the player connect event to trigger the custom event when a player connects.
            // Example usage: Custom player click handshake to communicate player data from Plugin1.cs to Plugin2.cs via a custom click event. (Plugin1: OnPlayerConnectEvent() -> CustomEvent() -> Plugin2: CustomEventHandler())

            OnCustomTriggerEvent.Call(p, "Hello from CustomEventExample!");
        }

        public override void Unload(bool shutdown)
        {
            OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
        }
    }

    #region This code can be in its own file if you wish!

    public class CustomEventHandlerExample : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.6"; } }
        public override string name { get { return "CustomEventHandlerExample"; } }

        public override void Load(bool startup)
        {
            OnCustomTriggerEvent.Register(HandlerCustomTriggerEvent, Priority.Low);
        }

        public override void Unload(bool shutdown)
        {
            OnCustomTriggerEvent.Unregister(HandlerCustomTriggerEvent);
        }

        static void HandlerCustomTriggerEvent(Player p, string message)
        {
            p.Message("%SIncoming message: %b" + message);
        }
    }

    #endregion
}
