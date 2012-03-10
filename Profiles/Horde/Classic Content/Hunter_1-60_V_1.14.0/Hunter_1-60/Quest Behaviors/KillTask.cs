
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

using Styx.Combat.CombatRoutine;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;
using Action = TreeSharp.Action;


namespace Styx.Bot.Quest_Behaviors
{
    public class KillTask : CustomForcedBehavior
    {
        public KillTask(Dictionary<string, string> args)
            : base(args)
        {
			try
			{
				ItemId      = GetAttributeAsItemId("ItemId", false, null) ?? 0;
				ItemCount   = GetAttributeAsInteger("ItemCount", false, 0, int.MaxValue, null) ?? 1;
				Collect		= GetAttributeAsBoolean("Collect", false, null) ?? false;
				MobIds      = GetNumberedAttributesAsIntegerArray("MobId", 1, 1, int.MaxValue, new [] { "MobID", "NpcId" }) ?? new int[0];
                QuestId     = GetAttributeAsQuestId("QuestId", false, null) ?? 0;
                QuestRequirementComplete = GetAttributeAsEnum<QuestCompleteRequirement>("QuestCompleteRequirement", false, null) ?? QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog    = GetAttributeAsEnum<QuestInLogRequirement>("QuestInLogRequirement", false, null) ?? QuestInLogRequirement.InLog;
			}

			catch (Exception except)
			{
				UtilLogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
										+ "\nFROM HERE:\n"
										+ except.StackTrace + "\n");
				IsAttributeProblem = true;
			}
        }


        // Attributes provided by caller
		public int ItemId { get; set; }
		public int ItemCount { get; set; }
		public bool Collect { get; set; }
		public int[]                    MobIds { get; private set; }
        public int                      QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement    QuestRequirementInLog { get; private set; }

        // Private variables for internal state
        private bool                _isBehaviorDone;
		private bool canmount { get { return (Styx.Logic.Mount.CanMount()); } }
        private Composite           _root;

        // Private properties
        private LocalPlayer         Me { get { return (ObjectManager.Me); } }
        public List<WoWUnit>        MobList { get  { return (ObjectManager.GetObjectsOfType<WoWUnit>()
                                                                    .Where(u => MobIds.Contains((int)u.Entry) && !u.Dead)
                                                                    .OrderBy(u => u.Distance).ToList());
                                            }}


        WoWSpell RangeSpell
        {
            get
            {
                switch (Me.Class)
                {
                    case Styx.Combat.CombatRoutine.WoWClass.Druid:
                        return SpellManager.Spells["Starfire"];
                    case Styx.Combat.CombatRoutine.WoWClass.Hunter:
                        return SpellManager.Spells["Arcane Shot"];
                    case Styx.Combat.CombatRoutine.WoWClass.Mage:
                        return SpellManager.Spells["Frost Bolt"];
                    case Styx.Combat.CombatRoutine.WoWClass.Priest:
                        return SpellManager.Spells["Shoot"];
                    case Styx.Combat.CombatRoutine.WoWClass.Shaman:
                        return SpellManager.Spells["Lightning Bolt"];
                    case Styx.Combat.CombatRoutine.WoWClass.Warlock:
                        return SpellManager.Spells["Curse of Agony"];
                    default: // should never get to here but adding this since the compiler complains
                        return SpellManager.Spells["Auto Attack"]; ;
                }
            }
        }

        private bool IsRanged
        {
            get
            {
               return (((Me.Class == WoWClass.Druid) && 
                        (SpellManager.HasSpell("BalanceSpell") || SpellManager.HasSpell("RestoSpell")))
                       ||
                       ((Me.Class == WoWClass.Shaman) && 
                        (SpellManager.HasSpell("ElementalSpell") || SpellManager.HasSpell("RestoSpell")))
                       ||
                       (Me.Class == WoWClass.Hunter) ||
                       (Me.Class == WoWClass.Mage) ||
                       (Me.Class == WoWClass.Priest) ||
                       (Me.Class == WoWClass.Warlock));
            }
        }

        private int Range
        {
            get
            {
                return (IsRanged ? 25 : 3);
            }
        }
		private Queue<WoWPoint>         Path { get; set; }


        private bool ParsePath()
        {
            var path = new Queue<WoWPoint>();

            foreach (WoWPoint point in ParseWoWPoints(Element.Elements().Where(elem => elem.Name == "Hotspot")))
                path.Enqueue(point);

            Path = path;
            return true;
        }


        public IEnumerable<WoWPoint> ParseWoWPoints(IEnumerable<XElement> elements)
        {
            var temp = new List<WoWPoint>();

            foreach (XElement element in elements)
            {
                XAttribute xAttribute, yAttribute, zAttribute;
                xAttribute = element.Attribute("X");
                yAttribute = element.Attribute("Y");
                zAttribute = element.Attribute("Z");

                float x, y, z;
                float.TryParse(xAttribute.Value, out x);
                float.TryParse(yAttribute.Value, out y);
                float.TryParse(zAttribute.Value, out z);
                temp.Add(new WoWPoint(x, y, z));
            }

            return temp;
        }

        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
							new Decorator(ret => (Collect && ObjectManager.Me.CarriedItems.Where(item => item.Entry == ItemId).Sum(item => (int)item.StackCount) >= ItemCount),
                                new Sequence(
                                    new Action(ret => TreeRoot.StatusText = "Finished!"),
                                    new WaitContinue(120,
                                        new Action(delegate
                                        {
                                            _isBehaviorDone = true;
                                            return RunStatus.Success;
                                        }))
                                    )),
                            new Decorator(ret => (Me.QuestLog.GetQuestById((uint)QuestId) != null && Me.QuestLog.GetQuestById((uint)QuestId).IsCompleted),
                                new Sequence(
                                    new Action(ret => TreeRoot.StatusText = "Finished!"),
                                    new WaitContinue(120,
                                        new Action(delegate
                                        {
                                            _isBehaviorDone = true;
                                            return RunStatus.Success;
                                        }))
                                    )),

                           new Decorator(ret => MobList.Count == 0,
                                new PrioritySelector(
									new Decorator(ret => !Path.Any(),
                                        new Action(delegate
										{
											ParsePath();
										})
                                    ),
									new Decorator(ret => Path.Peek().Distance(Me.Location) <= 5,
                                        new Action(delegate
										{
											Path.Dequeue();
										})
                                    ),
									new Decorator(ret =>Me.GetSkill(Styx.SkillLine.Riding).CurrentValue != 0 && canmount && !Me.Mounted && !Me.IsCasting && Path.Peek().Distance(Me.Location) > 100,
                                        new Action(delegate
										{
											TreeRoot.StatusText = "Mounting";
											WoWMovement.MoveStop();
											Thread.Sleep(100);
											Styx.Logic.Mount.MountUp();
										})
									),
									new Decorator(ret => Path.Peek().Distance(Me.Location) > 5,
                                        new Action(delegate
										{
											TreeRoot.StatusText = "Running to " + Path.Peek().ToString();
											Navigator.MoveTo(Path.Peek());
										})
                                    )
                                )
							),
                           new Decorator(ret => MobList.Count > 0 && !Me.IsCasting,
                                new Sequence(
                                    new DecoratorContinue(ret => MobList[0].Location.Distance(Me.Location) > Range || !MobList[0].InLineOfSight,
                                        new Sequence(
                                            new Action(ret => TreeRoot.StatusText = "Moving to Mob - " + MobList[0].Name + " Yards Away " + MobList[0].Location.Distance(Me.Location)),
                                            new Action(ret => Navigator.MoveTo(MobList[0].Location)),
                                            new Action(ret => Thread.Sleep(300))
                                            )
                                    ),
                                    new DecoratorContinue(ret => MobList[0].Location.Distance(Me.Location) <= Range && MobList[0].InLineOfSight,
                                        new Sequence(
                                        new Action(ret => TreeRoot.StatusText = "Attacking Mob - " + MobList[0].Name + " With Spell: " + RangeSpell.Name),
                                        new Action(ret => WoWMovement.MoveStop()),
                                        new Action(ret => MobList[0].Target()),
                                        new Action(ret => MobList[0].Face()),
                                        new Action(ret => Thread.Sleep(200)),
                                        new Action(ret => SpellManager.Cast(RangeSpell)),
                                        new Action(ret => Thread.Sleep(300))
                                            ))
                                    ))
                    ));
        }


        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone     // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }


        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();

            if (!IsDone)
            {
                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
				ParsePath();
                TreeRoot.GoalText = this.GetType().Name + ": " + ((quest != null) ? ("\"" + quest.Name + "\"") : "In Progress");
            }
        }

        #endregion
    }
}

