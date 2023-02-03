using System;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.DB;

public sealed class TopReachPlugin : Plugin 
{
	public override string name { get { return "TopReach"; } }
	TopStat reachStat = new ReachStat();

	public override void Load(bool startup) {
		TopStat.Stats.Add(reachStat);
	}
	
	public override void Unload(bool shutdown) {
		TopStat.Stats.Remove(reachStat);
	}
}

class ReachStat : TopStat
{
	public ReachStat() : base("Reach", null,null, () => "Largest custom reach distances", FormatResult) { }
	
	public override List<TopResult> GetResults(int maxResults, int offset) {
		List<string> raw = Server.reach.AllLines();
		string[] names   = new string[raw.Count];
		string[] reaches = new string[raw.Count];
		char splitter    = Server.reach.Separator;
		
		for (int i = 0; i < raw.Count; i++)
		{
			raw[i].Separate(splitter, out names[i], out reaches[i]);
		}
		Array.Sort(reaches, names, new NumberComparer());
		var results = new List<TopResult>(maxResults);
		
		for (int i = offset; i < raw.Count && i < offset + maxResults; i++)
		{
			TopResult result;
			result.Name  = names[i];
			result.Value = reaches[i];
			results.Add(result);
		}
		return results;
	}
	
	
	class NumberComparer : IComparer<string>
	{
		public int Compare(string a, string b)
		{
			//return ParseNumber(a).CompareTo(ParseNumber(b));
			
			// reverse sort, since want results in *descending* order
			return ParseNumber(b).CompareTo(ParseNumber(a));
		}
	}
	
	static int ParseNumber(string raw) {
		int value;
		int.TryParse(raw, out value);
		return value;
	}
	
	
	static string FormatResult(string raw) {
		int value = ParseNumber(raw);
		
		// reach is stored in raw units
		float distance = value / 32.0f;
		return distance.ToString("F2") + " blocks";
	}
}
