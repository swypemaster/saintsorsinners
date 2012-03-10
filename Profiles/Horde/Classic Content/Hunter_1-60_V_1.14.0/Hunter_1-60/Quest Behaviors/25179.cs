using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public class q25179 : CustomForcedBehavior
	{
		public q25179(Dictionary<string, string> args)
            : base(args){}
    
        
        public static LocalPlayer me = ObjectManager.Me;
		WoWPoint Location = new WoWPoint(250.5272, -5083.143, 4.113952);
        #region Overrides of CustomForcedBehavior
		public List<WoWUnit> mobList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 39270 && !Styx.Logic.Blacklist.Contains(u.Guid)))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        
		
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
                    new Decorator(ret => me.QuestLog.GetQuestById(25179) !=null && me.QuestLog.GetQuestById(25179).IsCompleted,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                                }))
                            )),
					new Decorator(ret => mobList.Count == 0 && !me.IsActuallyInCombat,
						new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Moving To Location"),
                            new Action(ret => Navigator.MoveTo(Location)),
                            new Action(ret => Thread.Sleep(300))
                        )
                    ),
					new Decorator(ret => mobList.Count > 0 && !me.IsActuallyInCombat,
						new Sequence(
							new DecoratorContinue(ret => mobList[0].Location.Distance(me.Location) > 3,
                                new Sequence(
									new Action(ret => TreeRoot.StatusText = "Moving to " + mobList[0].Name),
									new Action(ret => Navigator.MoveTo(mobList[0].Location)),
									new Action(ret => Thread.Sleep(300)))),
							new DecoratorContinue(ret => mobList[0].Location.Distance(me.Location) <= 3,	
								new Sequence(
									new Action(ret => TreeRoot.StatusText = "Interracting with " + mobList[0].Name),
									new Action(ret => WoWMovement.MoveStop()),
									new Action(ret => Thread.Sleep(300)),
									new Action(ret => mobList[0].Interact()),
									new Action(ret => Styx.Logic.Blacklist.Add(mobList[0].Guid, System.TimeSpan.FromMinutes(2))),
									new Decorator(ret => me.QuestLog.GetQuestById(25179) == null,
										new Sequence(
											new Action(ret => Thread.Sleep(2000)),
											new Action(ret => Lua.DoString("AcceptQuest()"))))
								
								)
							)
						)
					)
                )
			);
        }

        

        
        

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone; }
        }

        #endregion
    }
}

