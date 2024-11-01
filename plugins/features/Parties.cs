using System;
using System.Collections.Generic;
using System.IO;
using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy {
	
    public sealed class TeamData {
        public Team Team;
        public string Invite; 
        public DateTime NextInvite;
    }

    public sealed class Team {

        public string Color, Name, Owner;
        public List<string> Members = new List<string>();

        public Team() { }
        public Team(string name, string owner) {
          Name = name;
          Owner = owner;
          Members.Add(owner);
        }

        public static TeamData GetData(Player p) {
            object value;
            if (!p.Extras.TryGet("PARTY-DATA", out value)) {
                value = new TeamData();
                p.Extras["PARTY-DATA"] = value;
            }
            return (TeamData)value;
        }

        public void Message(Player source, string message) {
            message = "&d- Party - λNICK: &f" + message;
            Chat.MessageChat(ChatScope.All, source, message, this, (pl, arg) => GetData(pl).Team == arg);
        }

        public void Action(Player source, string message) {
            message = "&dParties> &SλNICK &S" + message;
            Chat.MessageFrom(ChatScope.All, source, message, this, (pl, arg) => GetData(pl).Team == arg);
        }

        public bool Remove(string name) {
            return Members.CaselessRemove(name);
        }

        public void OwnerLeft(Player p) {
        	Team team = Team.GetData(p).Team;
        	if (Members.Count > 0) { 
        		// Choose a new owner at random
        		team.Action(p, "has left the party (disconnected).");
	            team.Remove(p.name);
	            Team.SaveList();
	            Team.GetData(p).Team = null;
	            
	            var random = new Random();
		        int index = random.Next(team.Members.Count);
		        
		        team.Message(p, "&dThe new party owner is %b" + team.Members[index]);
		        
				team.Owner = team.Members[index];
	            Team.SaveList();
        		return; 
        	}
        	
            Teams.Remove(this); // Remove empty
        }

        public static List<Team> Teams = new List<Team>();
        static readonly object ioLock = new object();

        public static Team TeamIn(Player p) {
            foreach (Team team in Teams) {
                List<string> members = team.Members;
                if (members.CaselessContains(p.name)) return team;
            }
            return null;
        }

        public static Team Find(string name) {
            name = Colors.Strip(name);

            foreach (Team team in Teams) {
                string teamName = Colors.Strip(team.Name);
                if (name.CaselessEq(teamName)) return team;
            }
            return null;
        }

        public static void Add(Team team) {
            Team old = Find(team.Name);
            if (old != null) Teams.Remove(old);
            Teams.Add(team);
        }

        public static void SaveList() {
            lock (ioLock)
            using (StreamWriter w = new StreamWriter("plugins/Parties/parties.txt"))
            foreach (Team team in Teams) {
                w.WriteLine(team.Owner);
                string list = team.Members.Join(",");
                w.WriteLine("Members=" + list);
                w.WriteLine("");
            }
        }

        public static void LoadList() {
            if (!File.Exists("plugins/Parties/parties.txt")) return;
            Team tmp = new Team();

            lock (ioLock) {
                Teams.Clear();
                PropertiesFile.Read("plugins/Parties/parties.txt", ref tmp, LineProcessor, '=');
                if (tmp.Name != null) Add(tmp);
            }
        }

        static void LineProcessor(string key, string value, ref Team tmp) {
            switch (key.ToLower()) {
                case "color":
                tmp.Color = value; break;
                case "owner":
                tmp.Owner = value; break;
                case "members":
                tmp.Members = new List<string>(value.SplitComma()); break;
            }
        }
    }

    public sealed class CmdParty : Command2 {
        public override string name { get { return "Party"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool SuperUseable { get { return false; } }

        public override CommandPerm[] ExtraPerms {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can create parties.")}; }
        }

        public override void Use(Player p, string message, CommandData data) {
        	p.lastCMD = "Party";
            string prefix = "&dParties> %S";
            string noParty = prefix + "You are not in a party.";

            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(2);

            switch (args[0].ToLower()) {
                case "owner": HandleOwner(p, args); return;
                case "kick": HandleKick(p, args); return;
                case "create": HandleCreate(p, args, data); return;
                case "join": HandleJoin(p, args); return;
                case "invite": HandleInvite(p, args); return;
                case "leave": HandleLeave(p, args); return;
                case "list": HandleMembers(p, args); return;
            }

            Team team = Team.GetData(p).Team;
            if (team == null) {
                p.Message(noParty); return;
            }
            team.Message(p, message);
        }

        void HandleOwner(Player p, string[] args) {
            string prefix = "&dParties> %S";
            string noParty = prefix + "You are not in a party.";

            Team team = Team.GetData(p).Team;
            if (team == null) { p.Message(noParty); return; }

            if (args.Length == 1) {
                p.Message(prefix + "The current owner of the party is: " + team.Owner); return;
            }

            Player who = PlayerInfo.FindMatches(p, args[1]);
            if (who == null) return;

            if (!p.name.CaselessEq(team.Owner)) {
                p.Message(prefix + "Only the party owner can set the new party owner."); return;
            }
            
            team.Owner = who.name;
            team.Action(p, "set the party owner to " + who.ColoredName + "&S.");
            Team.SaveList();
        }

        void HandleKick(Player p, string[] args) {
            string prefix = "&dParties> %S";
            string noParty = prefix + "You are not in a party.";

            Team team = Team.GetData(p).Team;
            if (team == null) { p.Message(noParty); return; }
            	
            if (args.Length == 1) {
                p.Message(prefix + "You need to provide the name of the player to kick."); return;
            }
            	
            if (!p.name.CaselessEq(team.Owner)) {
                p.Message(prefix + "Only the party owner can kick players from the party."); return;
            }

            if (team.Remove(args[1])) {
                team.Action(p, "kicked " + args[1] + " %Sfrom the party.");
                Player who = PlayerInfo.FindExact(args[1]);
                if (who != null) {
                    Team.GetData(who).Team = null;
                }

            	team.OwnerLeft(p);
            	Team.SaveList();
          	} 
            	
            else {
            	p.Message(prefix + "Player not found. You need to use their full account name.");
        	}
        }

        void HandleCreate(Player p, string[] args, CommandData data) {
            string prefix = "&dParties> %S";
            string noParty = prefix + "You are not in a party.";
            string nothing = "";

          	if (!HasExtraPerm(p, data.Rank, 1)) return;
          	Team team = Team.GetData(p).Team;
          	if (team != null) { p.Message(prefix + "You need to leave your current party before you can create one."); return; }

	        team = new Team(nothing, p.name);
	        Team.GetData(p).Team = team;
	        Team.Add(team);
	        Team.SaveList();
	        
	        p.Message(prefix + "&SYou created a party. You can invite people to it via %b/party invite [name]%d.");
        }

        void HandleJoin(Player p, string[] args) {
            string prefix = "&dParties> %S";
            string noParty = prefix + "You are not in a party.";

          	Team team = Team.GetData(p).Team;
          	string invite = Team.GetData(p).Invite;

          	if (invite == null) { p.Message(prefix + "You have not been invited to any parties."); return; }
          	if (team != null) { p.Message(prefix + "You need to leave your current party before you can join another one."); return; }

          	team = Team.Find(invite);
          	if (team == null) { p.Message(prefix + "The party you were invited to no longer exists."); return; }

          	Team.GetData(p).Team = team;
          	Team.GetData(p).Invite = null;

          	team.Members.Add(p.name);
          	team.Action(p, "joined the party.");
          	Team.SaveList();
        }

        void HandleInvite(Player p, string[] args) {
            string prefix = "&dParties> %S";
            string noParty = prefix + "You are not in a party.";

          	Team team = Team.GetData(p).Team;
          	if (team == null) { p.Message(prefix + "You need to be in a party to invite players."); return; }
          	if (args.Length == 1) {
            	p.Message(prefix + "You need to provide the name of the person to invite."); return;
          	}
          	
          	Player who = PlayerInfo.FindMatches(p, args[1]);
          	if (who == null) return;

          	DateTime cooldown = Team.GetData(p).NextInvite;
          	DateTime now = DateTime.UtcNow;
          	if (now < cooldown) {
            	p.Message(prefix + "You can invite a player to join your party in another {0} seconds",
                      (int)(cooldown - now).TotalSeconds);
            	return;
          	}
          	
          	Team.GetData(p).NextInvite = now.AddSeconds(5);

            team.Action(p, "invited " + who.ColoredName + "&S to join the party.");
          	who.Message(prefix + p.ColoredName + " %Shas sent you a party request.");
            who.Message("To join, type %b/party join%S or ignore this message to decline.");
          	Team.GetData(who).Invite = team.Name;
        }

        void HandleLeave(Player p, string[] args) {
            string prefix = "&dParties> %S";
            string noParty = prefix + "You are not in a party.";

            Team team = Team.GetData(p).Team;
            if (team == null) { p.Message(noParty); return; }

            // Handle '/party leave me alone', for example
            if (args.Length > 1) {
                team.Message(p, args.Join(" ")); return;
            }

            team.Action(p, "has left the party.");
            team.Remove(p.name);
            Team.GetData(p).Team = null;

            team.OwnerLeft(p);
            Team.SaveList();
        }

        void HandleMembers(Player p, string[] args) {
            string prefix = "&dParties> %S";
            string noParty = prefix + "You are not in a party.";

            Team team = Team.GetData(p).Team;
            if (args.Length == 1) {
                if (team == null) { p.Message(noParty); return; }
            }
            p.Message("&dParty owner: %b" + team.Owner);
            p.Message("&dMembers: %b" + team.Members.Join());
        }

        public override void Help(Player p) {
            p.Message("&T/Party owner [name] &H- Sets the player who has owner privileges for the party.");
            p.Message("&T/Party kick [name] &H- Removes that player from the party you are in.");
            p.Message("&T/Party create &H- Creates a new party.");
            p.Message("&T/Party join &H- Joins the party you last received an invite to.");
            p.Message("&T/Party invite [name] &H- Invites that player to join your party.");
            p.Message("&T/Party leave &H- Leaves the party.");
            p.Message("&T/Party list &H- Lists the players in your party.");
            p.Message("&HAnything else is sent as a message to all online members of the party.");
        }
    }
	
    public class Parties : Plugin {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
        public override string name { get { return "Parties"; } }

        public override void Load(bool startup) {
            OnPlayerDisconnectEvent.Register(LeaveServer, Priority.High);
            Command.Register(new CmdParty());
        }

        void LeaveServer(Player p, string reason) {
        	Team team = Team.GetData(p).Team;
	        if (team == null) return;
	            
	        if (p.name.CaselessEq(team.Owner)) {
	            team.OwnerLeft(p);
	            return;
	        }
	            
	        team.Action(p, "has left the party (disconnected).");
	        team.Remove(p.name);
	        Team.GetData(p).Team = null;
	        Team.SaveList();
        }
         

        public override void Unload(bool shutdown) {
            OnPlayerDisconnectEvent.Unregister(LeaveServer);
            Command.Unregister(Command.Find("Party"));
        }
    }
}
