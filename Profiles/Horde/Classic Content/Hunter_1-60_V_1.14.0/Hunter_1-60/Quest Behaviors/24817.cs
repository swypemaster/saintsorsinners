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
    public class q24817 : CustomForcedBehavior
	{
		public q24817(Dictionary<string, string> args)
            : base(args){}
    
        
        public static LocalPlayer me = ObjectManager.Me;
		
		
        
		public List<WoWUnit> objmob
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 36682 && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
					
                    new Decorator(ret => me.QuestLog.GetQuestById(24817) !=null && me.QuestLog.GetQuestById(24817).IsCompleted,
						new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                            })))),
					new Decorator(ret => me.QuestLog.GetQuestById(24817) !=null && !me.QuestLog.GetQuestById(24817).IsCompleted,
						new Action(ret =>
						{
							if (me.QuestLog.GetQuestById(24817).IsCompleted)
							{
								Lua.DoString("VehicleExit()");
								return RunStatus.Success;
							}
							if (objmob.Count > 0 && objmob[0].Location.Distance(me.Location) > 45)
							{
								Navigator.MoveTo(objmob[0].Location);
								Thread.Sleep(100);
							}
							if (objmob.Count > 0 && (objmob[0].Location.Distance(me.Location) <= 45))
							{
								objmob[0].Face();
								objmob[0].Target();
								WoWMovement.Move(WoWMovement.MovementDirection.Backwards);
								WoWMovement.MoveStop(WoWMovement.MovementDirection.Backwards);
								Lua.DoString("CastPetAction(3) CastPetAction(2) CastPetAction(1)");
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

