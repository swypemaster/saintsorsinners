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
    public class q25543 : CustomForcedBehavior
    {

        public q25543(Dictionary<string, string> args)
            : base(args)
        {
            WoWPoint location = new WoWPoint(-6196.753, -3897.155, 4.935121);
			Location = location;
        }

        public WoWPoint Location { get; private set; }

        public static LocalPlayer me = ObjectManager.Me;
		
		
        #region Overrides of CustomForcedBehavior
		public List<WoWUnit> flyList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 40727))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        public List<WoWUnit> mobList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 40707 && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') then return 1 else return 0 end", 0) == 1; } }
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
                    new Decorator(ret => (me.QuestLog.GetQuestById(25543) != null && me.QuestLog.GetQuestById(25543).IsCompleted),
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
							new Action(ret => Lua.DoString("VehicleExit()")),
							new Action(ret => Thread.Sleep(15000)),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                                }))
                            )),
					new Decorator(ret => (!InVehicle && flyList.Count == 0) || (!InVehicle && flyList[0].Location.Distance(me.Location) > 3),
						new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Moving To get a ride"),
                            new Action(ret => Navigator.MoveTo(Location)),
                            new Action(ret => Thread.Sleep(300))
                        )
                    ),
					new Decorator(ret => !InVehicle && flyList.Count > 0 && flyList[0].Location.Distance(me.Location) <= 3,
						new Sequence(
							new Action(ret => TreeRoot.StatusText = "Interracting with - " + flyList[0].Name + " to enter vehicle"),
							new Action(ret => WoWMovement.MoveStop()),
							new Action(ret => flyList[0].Interact()),
							new Action(ret => Thread.Sleep(300)),
							new Action(ret => Lua.DoString("SelectGossipOption(1)"))
						)	
					),		
							
					new Decorator(ret => InVehicle && mobList.Count > 0,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Bombing - " + mobList[0].Name),
							new Action(ret => Lua.DoString("RunMacroText('/click VehicleMenuBarActionButton1','0')")),
							new Action(ret => LegacySpellManager.ClickRemoteLocation(mobList[0].Location)),
                            new Action(ret => Thread.Sleep(2000))
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

