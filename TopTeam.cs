using System;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.DB;
using MCGalaxy.Games;

public sealed class TopTeamsPlugin : Plugin 
{
    public override string name { get { return "TopTeams"; } }
    TopStat teamStat = new TeamStat();

    public override void Load(bool startup) {
        TopStat.Register(teamStat);
    }
    
    public override void Unload(bool shutdown) {
        TopStat.Unregister(teamStat);
    }
}

class TeamStat : TopStat
{
    public TeamStat() : base("Teams", "Largest teams", FormatResult) { }
    
    public override string FormatName(Player p, string name) { return name; }
	
    public override List<TopResult> GetResults(int maxResults, int offset) {
        List<Team> teams = new List<Team>(Team.Teams);
        // sort in reverse for descending
        teams.Sort((a,b) => b.Members.Count.CompareTo(a.Members.Count));
        List<TopResult> results = new List<TopResult>();
        
        for (int i = offset; i < teams.Count && i < offset + maxResults; i++)
        {
            Team t = teams[i];
            TopResult result;
            
            result.Name  = t.Color + t.Name;
            result.Value = FormatTeam(t);
            results.Add(result);
        }
        return results;
    }
    
    static string FormatTeam(Team t) {
        int members = t.Members.Count;
        const string fmt = "{0} member{1} (owned by {2})";
        return string.Format(fmt, members, members.Plural(), t.Owner);
    }
    
    static string FormatResult(string raw) { return raw; }
}
