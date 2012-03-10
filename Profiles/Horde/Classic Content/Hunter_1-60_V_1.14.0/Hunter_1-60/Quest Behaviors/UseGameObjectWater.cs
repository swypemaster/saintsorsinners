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
		public enum ObjectType
        {
            Npc,
            GameObject,
        }
        public UseGameObject(Dictionary<string, string> args)
            : base(args)
        {
			MobIds      = GetNumberedAttributesAsIntegerArray("ObjectId", 1, 1, int.MaxValue, new [] { "MobID", "NpcId" }) ?? new int[0];
            QuestId     = GetAttributeAsQuestId("QuestId", false, null) ?? 0;
            NumOfTimes  = GetAttributeAsNumOfTimes("NumOfTimes", false, null) ?? 1;
            Location    = GetXYZAttributeAsWoWPoint("", true, null) ?? WoWPoint.Empty;
            WaitTime    = GetAttributeAsWaitTime("WaitTime", false, null) ?? 1500;
			ObjType     = GetAttributeAsEnum<ObjectType>("ObjectType", false, new [] { "MobType" }) ?? ObjectType.GameObject;
			QuestRequirementComplete = GetAttributeAsEnum<QuestCompleteRequirement>("QuestCompleteRequirement", false, null) ?? QuestCompleteRequirement.NotComplete;
            QuestRequirementInLog    = GetAttributeAsEnum<QuestInLogRequirement>("QuestInLogRequirement", false, null) ?? QuestInLogRequirement.InLog;
        }

		public int[] MobIds { get; private set; }
        public WoWPoint Location { get; private set; }
        public int NumOfTimes { private get; set; }
        public int QuestId { get; private set; }
        public int WaitTime { get; private set; }
		public ObjectType ObjType { get; private set; }
		private readonly List<ulong> ObjBlacklist = new List<ulong>();
        public static LocalPlayer Me { get { return StyxWoW.Me; } } 
		public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement    QuestRequirementInLog { get; private set; }
		
        public WoWObject GameObject
        {
			get
            {
                WoWObject @object = null;
                switch (ObjType)
                {
                    case ObjectType.GameObject:
                        @object = ObjectManager.GetObjectsOfType<WoWGameObject>().OrderBy(ret => ret.Distance).FirstOrDefault(obj =>
                            MobIds.Contains((int)obj.Entry) && !obj.InUse && !obj.IsDisabled && !ObjBlacklist.Contains(obj.Guid));

                        break;

                    case ObjectType.Npc:
                        @object = ObjectManager.GetObjectsOfType<WoWUnit>().OrderBy(ret => ret.Distance).FirstOrDefault(obj =>
                            MobIds.Contains((int)obj.Entry) && !ObjBlacklist.Contains(obj.Guid));

                        break;

                }
				if (@object != null)
                {
                    Logging.Write(@object.Name);
                }
                return @object;
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
                            new Action(ret => ObjBlacklist.Add(GameObject.Guid)),
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
                            new Action(ret => Logging.Write("Using Object [{0}] {1} Times out of {2}", GameObject.Name, _counter + 1, NumOfTimes)),
                            new Action(ret => GameObject.Interact()),
                            new Action(ret => StyxWoW.SleepForLagDuration()),
							new Action(ret => ObjBlacklist.Add(GameObject.Guid)),
                            new Action(ret => Thread.Sleep(WaitTime)),
                            new Action(delegate { _counter++; })
                        )),

                        new Decorator(ret => Location != WoWPoint.Empty && GameObject == null,
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
