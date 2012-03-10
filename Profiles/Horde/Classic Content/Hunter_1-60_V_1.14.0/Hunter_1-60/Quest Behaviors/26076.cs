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
    public class q26076 : CustomForcedBehavior
	{
		public q26076(Dictionary<string, string> args)
            : base(args){}
    
        
        public static LocalPlayer me = ObjectManager.Me;
		WoWPoint location = new WoWPoint(1718.423, 794.8479, 130.4974);
		#region Overrides of CustomForcedBehavior
        public List<WoWUnit> MobList
		{
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => ((u.Entry ==  42034 || u.Entry ==  42035) && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public WoWItem Bomb
        {
            get
            {
                return StyxWoW.Me.CarriedItems.FirstOrDefault(b => b.Entry == 56800 && b.Cooldown == 0);
            }
        }
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
					
                    new Decorator(ret => me.QuestLog.GetQuestById(26076) !=null && me.QuestLog.GetQuestById(26076).IsCompleted,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                                }))
                            )),
					new Decorator(ret => !me.Dead && !me.Combat && MobList.Count == 0,
						new Action(ret =>
						{
							Navigator.MoveTo(location);
							TreeRoot.StatusText = "Moving to Location";
						})),
					new Decorator(ret => !me.Dead && !me.Combat && MobList.Count > 0,
						new Action(ret =>
						{
							if (MobList[0].Location.Distance(me.Location) > 30 || !MobList[0].InLineOfSight)
							{
								Navigator.MoveTo(MobList[0].Location);
								TreeRoot.StatusText = "Moving to "+MobList[0].Name;
							}
							if (MobList[0].Location.Distance(me.Location) <= 30 && MobList[0].InLineOfSight)
							{
								if (MobList[0].Flags == 33587200)
								{
									WoWMovement.MoveStop();
									Bomb.UseContainerItem();
									LegacySpellManager.ClickRemoteLocation(MobList[0].Location);
									TreeRoot.StatusText = "bombing "+MobList[0].Name;
									Thread.Sleep(2000);
								}
								else
								{
									WoWMovement.MoveStop();
									MobList[0].Target();
									SpellManager.Cast(5116);
								}
							}
						}
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

