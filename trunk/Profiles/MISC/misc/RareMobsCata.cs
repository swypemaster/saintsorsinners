using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;

using Styx;
using Styx.Plugins.PluginClass;
using Styx.Logic.BehaviorTree;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.Logic.Pathing;
using Styx.Logic.Combat;
using Styx.Combat.CombatRoutine;
using Styx.WoWInternals.WoWObjects;
using Styx.Logic.Inventory.Frames.Quest;
using Styx.Logic.Questing;
using Styx.Plugins;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Common;
using Styx.Logic.Inventory.Frames.Merchant;
using Styx.Logic;
using Styx.Logic.Profiles;
using Styx.Logic.Inventory.Frames.LootFrame;


namespace katzerle
{
	class RareMobsCata: HBPlugin
	{
		public static string pull { get { return "Flame Shock"; } }	 // <---------  Insert your instant Cast with Range min 20 here !!!

        // ***** anything below here isn't meant to be modified *************		
		public static string name { get { return "RareMobsCata Killer " + _version.ToString(); } }
		public override string Name { get { return name; } }
		public override string Author { get { return "katzerle"; } }
		private readonly static Version _version = new Version(0, 1);
		public override Version Version { get { return _version; } }
		public override string ButtonText { get { return "No Settings"; } }
		public override bool WantButton { get { return false; } }
		public static LocalPlayer Me = ObjectManager.Me;

		public override void OnButtonPress()
		{
		}
		
		public override void Pulse()
		{
			try
			{

                if (!inCombat)
					findAndKillMob();
			}
			catch (ThreadAbortException) { }
			catch (Exception e)
			{
				Log("Exception in Pulse:{0}", e);
			}
		}

		static public void findAndKillMob()
		{
			ObjectManager.Update();
			List<WoWUnit> objList = ObjectManager.GetObjectsOfType<WoWUnit>()
                .Where(o => ((o.Entry == 50057) // Blazewing Hyjal
				|| (o.Entry == 50053)	// Thartuk the Exile Hyjal
				|| (o.Entry == 50050)	// Shok'sharak Vashir
				|| (o.Entry == 50005)	// Poseidus Vashir
				|| (o.Entry == 50052)	// Burgy Blackheart Vashir
				|| (o.Entry == 49913)	// Lady La-La Vashir
				|| (o.Entry == 50060)	// Terborus Tiefenheim
				|| (o.Entry == 50059)	// Golgarok Tiefenheim
				|| (o.Entry == 50065)	// Armagedillo Uldum
				|| (o.Entry == 50064)	// Cyrus the Black Uldum
				|| (o.Entry == 50085)	// Overlord Sunderfury Twilight Higlands
				|| (o.Entry == 50086)	// Tarvus the Vile
				))
				.OrderBy(o => o.Distance).ToList();
			foreach (WoWUnit o in objList)
			{
				if(!o.Dead)
				{
					if (inCombat) return;
					Log("Find a Drake !!!");
					if (!(o.Entry == 50005) || !(o.Entry == 50052) || !(o.Entry == 49913))
					{
						Log("Flying to kill a Rare Mob !!!");
						while (o.Location.Distance(Me.Location) > 20)
							Flightor.MoveTo(o.Location);
					}
					o.Target();
					o.Face();
					SpellManager.Cast(pull);
					Log("Pull {0}", o.Entry);
					Thread.Sleep(500);
				}
			}
		}
		static public void Log(string msg, params object[] args) { Logging.Write(msg, args); }

		static public void Log(System.Drawing.Color c, string msg, params object[] args) { Logging.Write(c, msg, args); }
		
        static public bool inCombat
        {
            get
            {
                if (Me.Combat || Me.Dead || Me.IsGhost) return true;
                return false;
            }
        }

		public static int GetPing
		{
			get
			{
				return Lua.GetReturnVal<int>("return GetNetStats()", 2);
			}
		}
	}
}

