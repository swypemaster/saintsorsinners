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
    public class q13888 : CustomForcedBehavior
    {
        
        bool success = true;

        public q13888(Dictionary<string, string> args)
            : base(args)
        {
            QuestId     = GetAttributeAsQuestId("QuestId", false, null) ?? 0;
            Location    = GetXYZAttributeAsWoWPoint("", true, null) ?? WoWPoint.Empty;
			QuestRequirementComplete = GetAttributeAsEnum<QuestCompleteRequirement>("QuestCompleteRequirement", false, null) ?? QuestCompleteRequirement.NotComplete;
            QuestRequirementInLog    = GetAttributeAsEnum<QuestInLogRequirement>("QuestInLogRequirement", false, null) ?? QuestInLogRequirement.InLog;

        }

        public WoWPoint Location { get; private set; }
        public int QuestId { get; set; }
		public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement    QuestRequirementInLog { get; private set; }

        public static LocalPlayer me = ObjectManager.Me;
		
		
        #region Overrides of CustomForcedBehavior
		public List<WoWUnit> flyList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 34289))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        public List<WoWUnit> mobList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 34295 && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') then return 1 else return 0 end", 0) == 1; } }
		static public bool HasCooldown2 { get { return Lua.GetReturnVal<int>("if GetPetActionCooldown(2) == 0 then return 1 else return 0 end", 0) == 0; } }
		static public bool HasCooldown1 { get { return Lua.GetReturnVal<int>("if GetPetActionCooldown(1) == 0 then return 1 else return 0 end", 0) == 0; } }
		public WoWUnit HasDebuff 
		{ 
			get 
			{ 
				return ObjectManager.GetObjectsOfType<WoWUnit>(true)
									.Where(d => d.Entry == 34322 && d.HasAura("Lordly Immolate"))
									.OrderBy(d => d.Distance).FirstOrDefault();
			} 
		}
		
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
                    new Decorator(ret => (!UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete)),
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
							new Action(ret => Lua.DoString("RunMacroText('/click VehicleMenuBarActionButton3','0')")),
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
							
					new Decorator(ret => InVehicle && mobList.Count > 0 && mobList[0].Location.Distance(me.Location) <= 99,
                        new Sequence(
							new Action(ret => mobList[0].Target()),
							new Action(ret => TreeRoot.StatusText = "combat"),
							new DecoratorContinue(ret => HasDebuff != null,
								new Sequence(
									new Action(ret => TreeRoot.StatusText = "Despelling Debuff "),
									new Action(ret => Lua.DoString("RunMacroText('/click VehicleMenuBarActionButton3','0')")),
									new Action(ret => Thread.Sleep(300)))),
							new DecoratorContinue(ret => !HasCooldown2,
								new Sequence(
									new Action(ret => TreeRoot.StatusText = "Casting Spell 2 "),
									new Action(ret => Lua.DoString("RunMacroText('/click VehicleMenuBarActionButton2','0')")),
									new Action(ret => Thread.Sleep(300)))),		
							new DecoratorContinue(ret => !HasCooldown1,
								new Sequence(
									new Action(ret => TreeRoot.StatusText = "Casting Spell 1 "),
									new Action(ret => Lua.DoString("RunMacroText('/click VehicleMenuBarActionButton1','0')")),
									new Action(ret => Thread.Sleep(300))		
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

