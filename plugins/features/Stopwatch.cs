using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Tasks;

namespace MCGalaxy
{

	public class StopwatchPlugin : Plugin
	{
		public override string creator { get { return "Venk"; } }
		public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
		public override string name { get { return "Stopwatch"; } }

		public class StopwatchData
		{
			public int runMin;
			public int runSec;
			public int runMS;
			public bool runStop;
			public bool active;
		}

		static readonly object GetLocker = new object();
		public static StopwatchData Get(Player p)
		{
			lock (GetLocker)
			{
				object obj;
				p.Extras.TryGet("STOPWATCH_DATA", out obj);
				if (obj != null) return (StopwatchData)obj;

				var timer = new StopwatchData();
				p.Extras["STOPWATCH_DATA"] = timer;
				return timer;
			}
		}

		public override void Load(bool startup)
		{
			Command.Register(new CmdStopwatch());
			OnJoinedLevelEvent.Register(HandleOnJoinedLevel, Priority.Low);

		}


		public override void Unload(bool shutdown)
		{
			Command.Unregister(Command.Find("Stopwatch"));
			OnJoinedLevelEvent.Unregister(HandleOnJoinedLevel);
		}

		void HandleOnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
		{
			StopwatchPlugin.StopwatchData timer = StopwatchPlugin.Get(p);

			int finishMin = 0;
			int finishSec = 0;
			int finishMS = 0;

			if (timer.active)
			{
				finishMin = timer.runMin;
				finishSec = timer.runSec;
				finishMS = timer.runMS;
			}
			else
			{
				return;
			}

			string codeMin = "";
			string codeSec = "";
			string codeMS = "";

			if (finishMin < 10)
			{
				codeMin = "0" + finishMin.ToString();
			}
			else if (finishMin < 100)
			{
				codeMin = finishMin.ToString();
			}
			else
			{
				codeMin = "99";
			}
			if (finishSec < 10)
			{
				codeSec = "0" + finishSec.ToString();
			}
			else
			{
				codeSec = finishSec.ToString();
			}
			codeMS = finishMS.ToString();
			timer.runStop = true;

			p.SendCpeMessage(CpeMessageType.BottomRight2, "");
		}
	}

	public sealed class CmdStopwatch : Command2
	{
		public override string name { get { return "Stopwatch"; } }
		public override string shortcut { get { return "Timer"; } }
		public override string type { get { return "other"; } }

		void StartTimer(Player p, bool SpecifiedCode)
		{
			if (!SpecifiedCode && p.Extras.GetBoolean("PKR_STARTED_CODE")) { p.Message("&f╒ %c∩αΓ: %7You cannot restart a predefined stopwatch."); return; }
			StopwatchPlugin.StopwatchData timer = StopwatchPlugin.Get(p);
			if (timer.active)
			{
				timer.runMin = 0;
				timer.runSec = 0;
				timer.runMS = 0;
				return;
			}

			timer.active = true;
			timer.runStop = false;
			timer.runMin = 0;
			timer.runSec = 0;
			timer.runMS = 0;
			new Thread(() => LoopStopwatch(p, SpecifiedCode)).Start();
		}

		void StopTimer(Player p, bool SpecifiedCode)
		{
			if (!SpecifiedCode && p.Extras.GetBoolean("PKR_STARTED_CODE")) { p.Message("&f╒ %c∩αΓ: %7You cannot stop a predefined stopwatch."); return; }
			StopwatchPlugin.StopwatchData timer = StopwatchPlugin.Get(p);

			int finishMin = 0;
			int finishSec = 0;
			int finishMS = 0;

			if (timer.active)
			{
				finishMin = timer.runMin;
				finishSec = timer.runSec;
				finishMS = timer.runMS;
			}
			else
			{
				return;
			}

			string codeMin = "";
			string codeSec = "";
			string codeMS = "";

			if (finishMin < 10)
			{
				codeMin = "0" + finishMin.ToString();
			}
			else if (finishMin < 100)
			{
				codeMin = finishMin.ToString();
			}
			else
			{
				codeMin = "99";
			}
			if (finishSec < 10)
			{
				codeSec = "0" + finishSec.ToString();
			}
			else
			{
				codeSec = finishSec.ToString();
			}
			codeMS = finishMS.ToString();
			timer.runStop = true;

			string codeTime = codeMin + codeSec + codeMS;
			int intCodeTime = int.Parse(codeTime); // Create 5 digit code of the time the player finished with

			if (SpecifiedCode)
			{
				Player[] players = PlayerInfo.Online.Items;
				foreach (Player pl in players)
				{
					if (pl.level != p.level) break;
					if (pl == p)
					{
						p.Message("&aYou finished with a time of: %b" + finishMin + ":" + finishSec + ":" + finishMS + "%a.");
					}

					else
					{
						pl.Message("%b" + p.truename + " %afinished with a time of %b" + finishMin + ":" + finishSec + ":" + finishMS + "%a.");
					}
				}
			}

			else
			{
				p.Message("&aYou finished with a time of: %b" + finishMin + ":" + finishSec + ":" + finishMS + "%a.");
			}

			p.SendCpeMessage(CpeMessageType.BottomRight2, "");
		}


		void ResetTimer(Player p, bool SpecifiedCode)
		{
			if (!SpecifiedCode && p.Extras.GetBoolean("PKR_STARTED_CODE")) { p.Message("&f╒ %c∩αΓ: %7You cannot reset a predefined stopwatch."); return; }
			StopwatchPlugin.StopwatchData timer = StopwatchPlugin.Get(p);
			if (!timer.active) return;
			if (timer.runStop) return;
			timer.runStop = true;
			p.SendCpeMessage(CpeMessageType.BottomRight2, "");
		}

		void LoopStopwatch(Player p, bool SpecifiedCode)
		{
			if (!SpecifiedCode) p.Message("&SYou have started the stopwatch! Type %b/Stopwatch stop %Sto finish it.");
			else { p.Extras["PKR_STARTED_CODE"] = true; }
			int min = 0;
			int sec = 0;
			int ms = 0;
			int firstLoop = 1;

			StopwatchPlugin.StopwatchData timer = StopwatchPlugin.Get(p);
			for (min = 0; ; min++)
			{
				for (sec = 0; sec < 60; sec++)
				{
					for (ms = 0; ms < 10; ms++)
					{
						if (ms == 0)
						{
							if (timer.runMS != 9 && timer.runSec != sec - 1 && firstLoop == 0)
							{
								// Timer was restarted, so we need to set the min/sec/milli lists to the runnerTime
								min = timer.runMin;
								sec = timer.runSec;
								ms = timer.runMS;
							}
						}
						else
						{
							if (timer.runMS != ms - 1 && timer.runSec != sec)
							{
								// Same thing here
								min = timer.runMin;
								sec = timer.runSec;
								ms = timer.runMS;
							}
						}
						firstLoop = 0;

						timer.runMin = min;
						timer.runSec = sec;
						timer.runMS = ms;

						p.SendCpeMessage(CpeMessageType.BottomRight2, "&6Current Time: &c" + min + ":" + sec + ":" + ms);

						for (int i = 0; i < 10; i++)
						{
							Thread.Sleep(10);
							Player[] players = PlayerInfo.Online.Items;
							bool isOnline = false;
							foreach (Player pl in players)
							{
								if (pl.truename == p.truename) isOnline = true;
							}

							if (!isOnline) timer.runStop = true;

							if (timer.runStop)
							{
								timer.runStop = false;
								timer.active = false;
								return;
							}
						}
					}
				}
				sec = 0;
			}
		}

		public override void Use(Player p, string message, CommandData data)
		{
			p.lastCMD = "Secret";
			string[] args = message.SplitSpaces(2);

			if (message.Length == 0) { Help(p); return; }

			if (args[0] == "start")
			{
				if (args.Length >= 2) { if (args[1] == "code") { StartTimer(p, true); return; } }
				else
				{
					StartTimer(p, false);
					return;
				}
			}
			if (args[0] == "stop")
			{
				if (args.Length >= 2) { if (args[1] == "code") { StopTimer(p, true); return; } }
				else
				{
					StopTimer(p, false);
					return;
				}
			}
			if (args[0] == "reset")
			{
				if (args.Length >= 2) { if (args[1] == "code") { ResetTimer(p, true); } }
				else
				{
					ResetTimer(p, false);
					return;
				}
			}
		}

		public override void Help(Player p)
		{
			p.Message("&T/StopWatch [start/stop] &H- Toggles the stopwatch.");
			p.Message("&T/StopWatch reset &H- Resets the stopwatch.");
		}
	}
}
