using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Styx.Database;
using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Styx.Bot.Quest_Behaviors
{
    public class UseGameObject : CustomForcedBehavior
    {

        bool success = true;
		
        public UseGameObject(Dictionary<string, string> args)
            : base(args)
        {

            NumOfTimes  = GetAttributeAsNumOfTimes("NumOfTimes", false, null) ?? 1;
            QuestId     = GetAttributeAsQuestId("QuestId", false, null) ?? 0;
            WaitTime    = GetAttributeAsWaitTime("WaitTime", false, null) ?? 1500;
            Location    = GetXYZAttributeAsWoWPoint("", true, null) ?? WoWPoint.Empty;
        }

        public WoWPoint Location { get; private set; }
        public int NumOfTimes { private get; set; }
        public int QuestId { get; private set; }
        public int WaitTime { get; private set; }
		public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement    QuestRequirementInLog { get; private set; }

        public static LocalPlayer Me { get { return StyxWoW.Me; } } 

        public WoWGameObject GameObject
        {
            get
            {
                return 
                    ObjectManager.GetObjectsOfType<WoWGameObject>().Where(
                    u => (u.Entry == 202956 || u.Entry == 202957 || u.Entry == 202958 || u.Entry == 202959 || u.Entry == 202960 || u.Entry == 202961) && 
                        !u.InUse && !Styx.Logic.Blacklist.Contains(u.Guid) && 
                        !u.IsDisabled).OrderBy(u => u.Distance).FirstOrDefault();
            }
        }
		private static Stopwatch MoveTimer = new Stopwatch();
        #region Overrides of CustomForcedBehavior

        private Composite _root;
        private int _counter;

        protected override Composite CreateBehavior()
        {	
			
            return _root ?? (_root =
                new PrioritySelector(
				
					

                    // Move to the gameobject if it isn't null and we aren't withing interact range.
					new Decorator(ret => GameObject != null && !GameObject.WithinInteractRange && MoveTimer.ElapsedMilliseconds > 30*1000,
                        new Sequence(
                            new Action(ret => Styx.Logic.Blacklist.Add(GameObject.Guid, System.TimeSpan.FromMinutes(1))),
							new Action(ret => MoveTimer.Reset())
                            )
                        ),
                    new Decorator(ret => GameObject != null && !GameObject.WithinInteractRange,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "movetimer "+MoveTimer.ElapsedMilliseconds/1000 +" Moving to interact with gameobject: " + GameObject.Name),
                            new Action(ret => TreeRoot.GoalText = "Use Gameobject: " + GameObject.Name),
                            new Action(ret => WoWMovement.ClickToMove(GameObject.Location)),
							new Action(ret => MoveTimer.Start())
                            )
                        ),

                    // Interact etc. 
                    new Decorator(ret => GameObject != null && GameObject.WithinInteractRange,
                        // Set the context to the gameobject
                        new Sequence(ret => GameObject,

                            new DecoratorContinue(ret => StyxWoW.Me.IsMoving,
                                new Sequence(
                                    new Action(ret => WoWMovement.MoveStop()),
                                    new WaitContinue(5, ret => !StyxWoW.Me.IsMoving,
                                        null)
                                    )),
							new Action(ret => MoveTimer.Reset()),
                            new Action(ret => Logging.Write("Using Object [{0}] {1} Times out of {2}", ((WoWGameObject)ret).Name, _counter + 1, NumOfTimes)),
                            new Action(ret => ((WoWGameObject)ret).Interact()),
                            new Action(ret => StyxWoW.SleepForLagDuration()),
                            new Action(ret => Thread.Sleep(WaitTime)),
                            new Action(delegate { _counter++; })
                        )),

                        new Decorator(ret => Location != WoWPoint.Empty,
                            new Sequence(
                                new Action(ret => TreeRoot.StatusText = "Moving to interact with gameobject"),
                                new Action(ret => TreeRoot.GoalText = "Use Gameobject"),
                                new Action(ret => WoWMovement.ClickToMove(Location))
                                )
                            )
                        ));
        }

        public override bool IsDone
        {
            get
            {

                return
                   _counter >= NumOfTimes || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete);
            }
        }

        #endregion
    }
}
