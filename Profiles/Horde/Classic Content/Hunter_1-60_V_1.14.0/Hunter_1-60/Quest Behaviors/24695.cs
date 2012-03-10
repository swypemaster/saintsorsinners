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
    public class q24695 : CustomForcedBehavior
	{
		public q24695(Dictionary<string, string> args)
            : base(args){}
    
        
        public static LocalPlayer me = ObjectManager.Me;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') or not(GetBonusBarOffset()==0) then return 1 else return 0 end", 0) == 1; } }
		static public bool Obj1Done { get { return Lua.GetReturnVal<int>("a,b,c=GetQuestLogLeaderBoard(1,GetQuestLogIndexByID(24910));if c==1 then return 1 else return 0 end", 0) == 1; } }
		static public bool Obj2Done { get { return Lua.GetReturnVal<int>("a,b,c=GetQuestLogLeaderBoard(2,GetQuestLogIndexByID(24910));if c==1 then return 1 else return 0 end", 0) == 1; } }
		static public bool Obj3Done { get { return Lua.GetReturnVal<int>("a,b,c=GetQuestLogLeaderBoard(3,GetQuestLogIndexByID(24910));if c==1 then return 1 else return 0 end", 0) == 1; } }
		static public bool Obj4Done { get { return Lua.GetReturnVal<int>("a,b,c=GetQuestLogLeaderBoard(4,GetQuestLogIndexByID(24910));if c==1 then return 1 else return 0 end", 0) == 1; } }
		static public bool OnCooldown2 { get { return Lua.GetReturnVal<int>("a,b,c=GetActionCooldown(122);if b==0 then return 1 else return 0 end", 0) == 0; } }
		WoWPoint obj3loc1 = new WoWPoint(-6369.687, -1842.553, -259.4411);
		WoWPoint obj3loc2 = new WoWPoint(-6399.317, -1675.363, -271.7336);
		private readonly List<ulong>    LocalBlacklist = new List<ulong>();
		private bool locreached;
		
        public List<WoWGameObject> obj1lever
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWGameObject>()
                                    .Where(u => (u.Entry == 202187))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWGameObject> obj2lever
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWGameObject>()
                                    .Where(u => (u.Entry == 202197))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWGameObject> obj3lever
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWGameObject>()
                                    .Where(u => (u.Entry == 202196))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWGameObject> obj4lever
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWGameObject>()
                                    .Where(u => (u.Entry == 202195))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> obj1mob
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (!LocalBlacklist.Contains(u.Guid) && (u.Entry == 6509 || u.Entry == 6510 || u.Entry == 6511 || u.Entry == 6512) && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> obj2mob
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (!LocalBlacklist.Contains(u.Guid) && u.FactionId == 2263 && !u.HasAura("Bite") && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> obj3mob
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (!LocalBlacklist.Contains(u.Guid) && (u.Entry == 6513 || u.Entry == 6514 || u.Entry == 6516) && !LocalBlacklist.Contains(u.Entry) && !u.Dead && Navigator.CanNavigateFully(me.Location, u.Location)))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
					
                    new Decorator(ret => me.QuestLog.GetQuestById(24695) !=null && me.QuestLog.GetQuestById(24695).IsCompleted,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                                }))
                            )),
					new Decorator(ret => !Obj1Done,
						new Action(ret =>
						{
							if (Obj1Done)
								return RunStatus.Success;
							if (!InVehicle)
							{
								if (obj1lever.Count > 0 && obj1lever[0].Location.Distance(me.Location) > 5)
								{
									Navigator.MoveTo(obj1lever[0].Location);
									return RunStatus.Running;
								}
								WoWMovement.MoveStop();
								obj1lever[0].Interact();
								Thread.Sleep(5000);
								return RunStatus.Running;
							}
							if (InVehicle)
							{
								if (obj1mob.Count > 0 && obj1mob[0].Location.Distance(me.Location) > 5)
								{
									Navigator.MoveTo(obj1mob[0].Location);
									obj1mob[0].Target();
									return RunStatus.Running;
								}
								if (obj1mob.Count > 0 && obj1mob[0].Location.Distance(me.Location) <= 5)
								{
									WoWMovement.MoveStop();
									if (!OnCooldown2)
										Lua.DoString("UseAction(122, 'target', 'LeftButton')");
									if (!obj1mob[0].HasAura("Bloodpetal Poison"))
										Lua.DoString("UseAction(123, 'target', 'LeftButton')");
									return RunStatus.Running;
								}
							return RunStatus.Running;
							}
						return RunStatus.Running;
						}
					)),
					new Decorator(ret => !Obj2Done,
						new Action(ret =>
						{
							if (Obj2Done)
								return RunStatus.Success;
							if (!InVehicle)
							{
								if (obj2lever.Count > 0 && obj2lever[0].Location.Distance(me.Location) > 5)
								{
									Navigator.MoveTo(obj2lever[0].Location);
									return RunStatus.Running;
								}
								WoWMovement.MoveStop();
								obj2lever[0].Interact();
								Thread.Sleep(5000);
								return RunStatus.Running;
							}
							if (InVehicle)
							{
								if (!me.HasAura("Bony Armor"))
									Lua.DoString("UseAction(123, 'target', 'LeftButton')");
								if (obj2mob.Count > 0 && obj2mob[0].Location.Distance(me.Location) > 5)
								{
									Navigator.MoveTo(obj2mob[0].Location);
									return RunStatus.Running;
								}
								if (obj2mob.Count > 0 && obj2mob[0].Location.Distance(me.Location) <= 5)
								{
									obj2mob[0].Target();
									WoWMovement.MoveStop();
									Lua.DoString("UseAction(122, 'target', 'LeftButton')");
									return RunStatus.Running;
								}
							return RunStatus.Running;
							}
						return RunStatus.Running;
						}
					)),
					new Decorator(ret => !Obj3Done,
						new Action(ret =>
						{
							if (Obj3Done)
								return RunStatus.Success;
							if (!InVehicle)
							{
								if (obj3lever.Count > 0 && obj3lever[0].Location.Distance(me.Location) > 5)
								{
									Navigator.MoveTo(obj3lever[0].Location);
									return RunStatus.Running;
								}
								WoWMovement.MoveStop();
								obj3lever[0].Interact();
								Thread.Sleep(5000);
								return RunStatus.Running;
							}
							if (InVehicle)
							{
								if (obj3mob.Count == 0)
								{
									if (me.Location.Distance(obj3loc1) < 5)
									locreached = true;
									if (me.Location.Distance(obj3loc2) < 5)
									locreached = false;
									if (!locreached)
									{
										Navigator.MoveTo(obj3loc1);
										return RunStatus.Running;
									}
									if (locreached)
										Navigator.MoveTo(obj3loc2);
								}
								
								if (obj3mob.Count > 0 && ((me.HasAura("Stomper Knowledge") && obj3mob[0].Entry == 6513 
														|| me.HasAura("Thunderer Knowledge") && obj3mob[0].Entry == 6516 
														|| me.HasAura("Gorilla Knowledge") && obj3mob[0].Entry == 6514)))
								{
									LocalBlacklist.Add(obj3mob[0].Entry);
									return RunStatus.Running;
								}
								if (obj3mob.Count > 0 && obj3mob[0].Location.Distance(me.Location) > 5)
								{
									Navigator.MoveTo(obj3mob[0].Location);
									return RunStatus.Running;
								}
								if (obj3mob.Count > 0 && obj3mob[0].Location.Distance(me.Location) <= 5)
								{
									obj3mob[0].Target();
									Thread.Sleep(100);
									obj3mob[0].Interact();
									Thread.Sleep(2000);
									Lua.DoString("SelectGossipOption(1)");
									Thread.Sleep(2000);
									Lua.DoString("SelectGossipOption(1)");
									Thread.Sleep(2000);
									return RunStatus.Running;
								}
							return RunStatus.Running;
							}
						return RunStatus.Running;
						}
					)),
					new Decorator(ret => !Obj4Done,
						new Action(ret =>
						{
							if (Obj4Done)
								return RunStatus.Success;
							if (!InVehicle)
							{
								if (obj4lever.Count > 0 && obj4lever[0].Location.Distance(me.Location) > 5)
								{
									Navigator.MoveTo(obj4lever[0].Location);
									return RunStatus.Running;
								}
								WoWMovement.MoveStop();
								obj4lever[0].Interact();
								Thread.Sleep(5000);
								return RunStatus.Running;
							}
							if (InVehicle)
							{
								WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
								if (!OnCooldown2)
									Lua.DoString("UseAction(122, 'target', 'LeftButton')");
								return RunStatus.Running;
							}
						return RunStatus.Success;
						}
					))
					
                )
			);
        }

        

        
        

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone; }
        }

    }
}

