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
    public class q13880 : CustomForcedBehavior
    {
        
        bool success = true;

        public q13880(Dictionary<string, string> args)
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
		public List<WoWGameObject> objList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWGameObject>()
                                    .Where(u => (u.Entry == 195002 && !Styx.Logic.Blacklist.Contains(u.Guid)))
                                    .OrderBy(u => u.Distance).ToList();
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
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                                }))
                            )),
					new Decorator(ret => objList.Count == 0 && !me.IsCasting,
						new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Moving To Location"),
                            new Action(ret => Navigator.MoveTo(Location)),
                            new Action(ret => Thread.Sleep(300))
                        )
                    ),
					new Decorator(ret => objList.Count > 0 && !me.IsCasting,
						new Sequence(
							new DecoratorContinue(ret => objList[0].Location.Distance(me.Location) > 5,
                                new Sequence(
									new Action(ret => TreeRoot.StatusText = "Moving to " + objList[0].Name),
									new Action(ret => Navigator.MoveTo(objList[0].Location)),
									new Action(ret => Thread.Sleep(300)))),
							new DecoratorContinue(ret => objList[0].Location.Distance(me.Location) <= 10,	
								new Sequence(
									new Action(ret => TreeRoot.StatusText = "Filling - " + objList[0].Name),
									new Action(ret => WoWMovement.MoveStop()),
									new Action(ret => Thread.Sleep(300)),
									new Action(ret => Lua.DoString("UseItemByName(46352)")),
									new Action(ret => LegacySpellManager.ClickRemoteLocation(objList[0].Location)),
									new Action(ret => Thread.Sleep(300)),
									new Action(ret => Styx.Logic.Blacklist.Add(objList[0].Guid, System.TimeSpan.FromSeconds(15)))
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

