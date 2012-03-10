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
using Styx.Logic.POI;
using Styx.Combat.CombatRoutine;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Styx.Logic;
using Styx.Logic.BehaviorTree;
using Action = TreeSharp.Action;

using System.Diagnostics;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;

namespace Styx.Bot.Quest_Behaviors
{
    public class KillUntilComplete : CustomForcedBehavior
    {
        

        /// <summary>
        /// KillUntilComplete by Natfoth
        /// This is only used when you get a quest that Says, Kill anything x times. Or on the chance the wowhead ID is wrong
        /// ##Syntax##
        /// QuestId: Id of the quest.
        /// MobId, MobId2, MobId3: Currently Accepts 3 Mob Values that it will kill.
        /// X,Y,Z: The general location where theese objects can be found
        /// </summary>
        /// 

        bool success = true;

        public KillUntilComplete(Dictionary<string, string> args)
            : base(args)
        {
			MobId       = GetAttributeAsMobId("MobId", true, new [] { "NpcId" }) ?? 0;
            Location    = GetXYZAttributeAsWoWPoint("", true, null) ?? WoWPoint.Empty;
			ItemId      = GetAttributeAsItemId("ItemId", false, null) ?? 0;
			ItemCount   = GetAttributeAsInteger("ItemCount", false, 0, int.MaxValue, null) ?? 1;
			Collect		= GetAttributeAsBoolean("Collect", false, null) ?? false;
			QuestId     = GetAttributeAsQuestId("QuestId", false, null) ?? 0;
            QuestRequirementComplete = GetAttributeAsEnum<QuestCompleteRequirement>("QuestCompleteRequirement", false, null) ?? QuestCompleteRequirement.NotComplete;
            QuestRequirementInLog    = GetAttributeAsEnum<QuestInLogRequirement>("QuestInLogRequirement", false, null) ?? QuestInLogRequirement.InLog;
			
            Counter = 0;
            TimesUsedCounter = 1;
            MovedToLocation = false;
			lastunit = null;

        }

        public WoWPoint Location { get; private set; }
        public int Counter { get; set; }
        public int TimesUsedCounter { get; set; }
        public int MobId { get; set; }
		public int ItemId { get; set; }
		public int ItemCount { get; set; }
        public bool MovedToLocation { get; set; }
		public bool Collect { get; set; }
		public WoWUnit lastunit { get; set; }
		public int                      QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement    QuestRequirementInLog { get; private set; }
        public static LocalPlayer me = ObjectManager.Me;

        #region Overrides of CustomForcedBehavior

        public List<WoWUnit> mobList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => u.Entry == MobId && !u.Dead && !Styx.Logic.Blacklist.Contains(u.Guid))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> deadList
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => u.Entry == MobId && u.Dead && u.CanLoot && !LootBlacklist.Contains(u.Guid))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        private static Stopwatch PullTimer = new Stopwatch();
		private readonly List<ulong> LootBlacklist = new List<ulong>();

        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
							
							new Decorator(ret => ObjectManager.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime < 60000 && ObjectManager.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime !=0,
								new Sequence(
											new Action(ret => WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend)),
											new Action(ret => 
											{
												while (ObjectManager.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime !=0)
												{
												Thread.Sleep(1000);
												TreeRoot.StatusText = "Air left for "+ObjectManager.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime / 1000 +" seconds. Moving to take a breath";
												}
											}),
											new Action(ret => WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend))
										
							)),

                            new Decorator(ret => (Collect && ObjectManager.Me.CarriedItems.Where(item => item.Entry == ItemId).Sum(item => (int)item.StackCount) >= ItemCount),
                                new Sequence(
                                    new Action(ret => TreeRoot.StatusText = "Finished!"),
                                    new WaitContinue(120,
                                        new Action(delegate
                                        {
											while (ObjectManager.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime !=0)
											{
											WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
											}
											WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
                                            _isDone = true;
                                            return RunStatus.Success;
                                        }))
                                    )),
							new Decorator(ret => (!Collect && !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete)),
                                new Sequence(
                                    new Action(ret => TreeRoot.StatusText = "Finished!"),
                                    new WaitContinue(120,
                                        new Action(delegate
                                        {
											while (ObjectManager.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime !=0)
											{
											WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
											}
											WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
                                            _isDone = true;
                                            return RunStatus.Success;
                                        }))
                                    )),
                           

							new Decorator(ret => deadList.Count > 0 && !me.IsCasting,
								new Sequence(
									new DecoratorContinue(ret => deadList.Count > 0 && PullTimer.ElapsedMilliseconds > 60*1000,
										new Sequence(
											new Action(ret => LootBlacklist.Add(deadList[0].Guid)),
											new Action(ret => PullTimer.Reset())
                                    )),
									new DecoratorContinue(ret => deadList[0].Location.Distance(me.Location) > 3,
										new Sequence(
											new Action(ret => TreeRoot.StatusText = "Moving To loot"),
											new Action(ret => WoWMovement.ClickToMove(deadList[0].Location)),
											new Action(ret => PullTimer.Start()),
											new Action(ret => Thread.Sleep(300))
                                    )),
									new DecoratorContinue(ret => deadList[0].Location.Distance(me.Location) <= 3,	
										new Sequence(
											new Action(ret => TreeRoot.StatusText = "looting"),
											new Action(ret => WoWMovement.MoveStop()),
											new Action(ret => deadList[0].Interact()),
											new Action(ret => Thread.Sleep(300))
							)))),
							
							new Decorator(ret => mobList.Count == 0,
                                new Sequence(
                                        new Action(ret => TreeRoot.StatusText = "Moving To Location - X: " + Location.X + " Y: " + Location.Y),
                                        new Action(ret => WoWMovement.ClickToMove(Location)),
										new Action(ret => PullTimer.Start()),
                                        new Action(ret => Thread.Sleep(300))
                                    )
                                ),
							new Decorator(ret => mobList.Count > 0 && PullTimer.ElapsedMilliseconds > 60*1000,
								new Sequence(
									new Action(ret => Styx.Logic.Blacklist.Add(mobList[0].Guid, System.TimeSpan.FromMinutes(2))),
									new Action(ret => PullTimer.Reset()))),
							new Decorator(ret => mobList.Count > 0 && lastunit != mobList[0],
								new Sequence(
									new Action(ret => PullTimer.Reset()),
									new Action(ret => WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend)),
									new Action(ret => Thread.Sleep(2000)),
									new Action(ret => WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend)),
									new Action(ret => lastunit = mobList[0])
									)),
							
							new Decorator(ret => mobList.Count > 0 && !me.IsCasting,
                                new Sequence(
                                    new DecoratorContinue(ret => mobList[0].Location.Distance(me.Location) > range,
                                        new Sequence(
                                            new Action(ret => TreeRoot.StatusText = "pulltimer "+PullTimer.ElapsedMilliseconds/1000 +" Moving to Mob - " + mobList[0].Name + " Yards Away " + mobList[0].Location.Distance(me.Location)),
                                            new Action(ret => WoWMovement.ClickToMove(mobList[0].Location)),
											new Action(ret => lastunit = mobList[0]),
                                            new Action(ret => Thread.Sleep(300)),
											new Action(ret => PullTimer.Start())
                                            )
                                    ),
                                    new DecoratorContinue(ret => mobList[0].Location.Distance(me.Location) <= range,
                                        new Sequence(
                                        new Action(ret => TreeRoot.StatusText = "Attacking Mob - " + mobList[0].Name + " With Spell: " + RangeSpell.Name),
										new Action(ret => PullTimer.Reset()),
                                        new Action(ret => WoWMovement.MoveStop()),
                                        new Action(ret => mobList[0].Target()),
                                        new Action(ret => mobList[0].Face()),
                                        new Action(ret => Thread.Sleep(200)),
                                        new Action(ret => SpellManager.Cast(RangeSpell)),
                                        new Action(ret => Thread.Sleep(300))
                                            ))
                                    ))
                    ));
        }

        WoWSpell RangeSpell
        {
            get
            {
                switch (me.Class)
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

        bool isRanged
        {
            get
            {
               return (me.Class == WoWClass.Druid && 
                   (SpellManager.HasSpell("balanceSpell") || SpellManager.HasSpell("RestoSpell"))||
                   me.Class == WoWClass.Shaman && 
                   (SpellManager.HasSpell("ElementalSpell") || SpellManager.HasSpell("RestoSpell"))||
                   me.Class == WoWClass.Hunter || me.Class == WoWClass.Mage || me.Class == WoWClass.Priest ||
                   me.Class == WoWClass.Warlock);
            }
        }

        int range
        {
            get
            {
                if (isRanged)
                {
                    return 25;
                }
                else
                {
                    return 3;
                }
            }
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone; }
        }

        #endregion
    }
}

