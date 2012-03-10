using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Styx.Database;
using Styx.Logic.Combat;
using Styx.Helpers;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Pathing;
using Styx.Logic.Profiles.Quest;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Styx.Logic.BehaviorTree;
using Action = TreeSharp.Action;

namespace Styx.Bot.Quest_Behaviors
{
    public class q14236 : CustomForcedBehavior
	{
		public q14236(Dictionary<string, string> args)
            : base(args){}
    
        
        public static LocalPlayer me = ObjectManager.Me;
		
		WoWPoint endloc = new WoWPoint(1350.093, 1485.307, 306.6513);
		private readonly List<ulong>    LocalBlacklist = new List<ulong>();
		
        
		public List<WoWUnit> objmob
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (!LocalBlacklist.Contains(u.Guid) && (u.Entry == 35995 || u.Entry == 35996 || u.Entry == 35997) && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
					
                    new Decorator(ret => me.QuestLog.GetQuestById(14236) !=null && me.QuestLog.GetQuestById(14236).IsCompleted,
						new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                            })))),
					new Decorator(ret => me.QuestLog.GetQuestById(14236) !=null && !me.QuestLog.GetQuestById(14236).IsCompleted,
						new Action(ret =>
						{
							if (me.QuestLog.GetQuestById(14236).IsCompleted)
							{
								while (me.Location.Distance(endloc) > 5)
								{
									Navigator.MoveTo(endloc);
									Thread.Sleep(1000);
								}
								Lua.DoString("UseItemByName(49108)");
								return RunStatus.Success;
							}
							if (LocalBlacklist.Count > 10)
								LocalBlacklist.Clear();
							if (objmob.Count > 0 && objmob[0].Location.Distance(me.Location) > 15)
							{
								Navigator.MoveTo(objmob[0].Location);
								Thread.Sleep(100);
							}
							if (objmob.Count > 0 && (objmob[0].Location.Distance(me.Location) <= 15 || !Navigator.CanNavigateFully(me.Location, objmob[0].Location)))
							{
								LocalBlacklist.Add(objmob[0].Guid);
							}
						return RunStatus.Running;
						}
					))
					
					
                )
			);
        }

        

        
        

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone; }
        }

    }
}

