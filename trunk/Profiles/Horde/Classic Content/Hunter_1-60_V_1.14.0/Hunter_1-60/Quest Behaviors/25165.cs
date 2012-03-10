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
    public class q25165 : CustomForcedBehavior
    {
        

        Dictionary<string, object> recognizedAttributes = new Dictionary<string, object>()
        {

            {"X",null},
            {"Y",null},
            {"Z",null},
            {"QuestId",null},

        };

        bool success = true;

        public q25165(Dictionary<string, string> args)
            : base(args)
        {
            CheckForUnrecognizedAttributes(recognizedAttributes);

            int questId = 0;
            WoWPoint location = new WoWPoint(0, 0, 0);

            success = success && GetAttributeAsInteger("QuestId", false, "0", 0, int.MaxValue, out questId);
            success = success && GetXYZAttributeAsWoWPoint("X", "Y", "Z", true, new WoWPoint(0, 0, 0), out location);

            QuestId = (uint)questId;
            Location = location;

        }

        public WoWPoint Location { get; private set; }
        public uint QuestId { get; set; }
		public int Counter = 0;
        public static LocalPlayer me = ObjectManager.Me;
		
		
        #region Overrides of CustomForcedBehavior
		public List<WoWUnit> mobList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 3125 && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        
		
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
                    new Decorator(ret => (me.QuestLog.GetQuestById(QuestId) != null && me.QuestLog.GetQuestById(QuestId).IsCompleted),
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                                }))
                            )),
					new Decorator(ret => mobList.Count == 0 && !me.IsCasting,
						new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Moving To Location"),
                            new Action(ret => Navigator.MoveTo(Location)),
                            new Action(ret => Thread.Sleep(300))
                        )
                    ),
					new Decorator(ret => mobList.Count > 0 && !me.IsCasting && Counter == 0,
						new Sequence(
							new DecoratorContinue(ret => mobList[0].Location.Distance(me.Location) > 30,
                                new Sequence(
									new Action(ret => TreeRoot.StatusText = "Moving to " + mobList[0].Name),
									new Action(ret => Navigator.MoveTo(mobList[0].Location)),
									new Action(ret => Thread.Sleep(300)))),
							new DecoratorContinue(ret => mobList[0].Location.Distance(me.Location) <= 30,	
								new Sequence(
									new Action(ret => TreeRoot.StatusText = "Waiting for " + mobList[0].Name +" to start cast"),
									new Action(ret => WoWMovement.MoveStop()),
									new Action(ret => Thread.Sleep(300)),
									new Action(ret => mobList[0].Target()),
									new Action(ret => Lua.DoString("AttackTarget()")),
									new Action(ret => Counter++),
									new Action(ret => Thread.Sleep(300)),
									new Action(ret => Lua.DoString("StopAttack()")))))),
					new Decorator(ret => mobList.Count > 0 && mobList[0].IsCasting && Counter != 0,
						new Sequence(
							new Action(ret => TreeRoot.StatusText = "Placing totem"),
							new Action(ret => Lua.DoString("UseItemByName(52505)")),
							new Action(ret => Thread.Sleep(300)),
							new Action(ret => Counter--)
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

