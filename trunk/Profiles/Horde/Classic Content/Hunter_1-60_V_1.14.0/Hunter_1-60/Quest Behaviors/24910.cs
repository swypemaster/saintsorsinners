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
    public class q24910 : CustomForcedBehavior
	{
		public q24910(Dictionary<string, string> args)
            : base(args){}
    
        
        public static LocalPlayer me = ObjectManager.Me;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') then return 1 else return 0 end", 0) == 1; } }
		static public bool Obj1Done { get { return Lua.GetReturnVal<int>("a,b,c=GetQuestLogLeaderBoard(1,GetQuestLogIndexByID(24910));if c==1 then return 1 else return 0 end", 0) == 1; } }
		static public bool Obj2Done { get { return Lua.GetReturnVal<int>("a,b,c=GetQuestLogLeaderBoard(2,GetQuestLogIndexByID(24910));if c==1 then return 1 else return 0 end", 0) == 1; } }
		public double angle = 0;
		public double CurentAngle = 0;
		WoWPoint Location = new WoWPoint(-7094.154, -3904.856, 10.67494);
        public List<WoWUnit> mob1List
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 40583 && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> mob2List
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 38571 && !u.Dead))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> Baloon
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 40604))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
					
                    new Decorator(ret => me.QuestLog.GetQuestById(24910) !=null && me.QuestLog.GetQuestById(24910).IsCompleted,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
							new Action(ret => Lua.DoString("CastPetAction({0})", 6)),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                                }))
                            )),
					new Decorator(ret => !InVehicle,
						new Action(ret =>
						{
							if (Baloon.Count > 0 && Baloon[0].Location.Distance(me.Location) <= 5)
							{
								WoWMovement.MoveStop();
								Baloon[0].Interact();
							}
							if (Baloon.Count > 0 && Baloon[0].Location.Distance(me.Location) > 5)
							{
								Navigator.MoveTo(Baloon[0].Location);
							}
							if (Baloon.Count == 0)
							{
								Navigator.MoveTo(Location);
							}
						}
					)),
					new Decorator(ret => !Obj2Done && mob1List.Count > 0,
					new Action(ret =>
					{
						if(mob1List.Count == 0)
							return;
						mob1List[0].Target();
							
						while(me.CurrentTarget != null && mob1List.Count > 0 && me.CurrentTarget.Guid == mob1List[0].Guid && me.CurrentTarget.IsAlive)
						{
							WoWMovement.ConstantFace(me.CurrentTarget.Guid);
							angle = -((me.Z - me.CurrentTarget.Z) / (me.CurrentTarget.Location.Distance(me.Location))) + ((me.CurrentTarget.Location.Distance2D(me.Location) - 20) / me.CurrentTarget.Location.Distance(me.Location) / 10);
							CurentAngle = Lua.GetReturnVal<double>("return VehicleAimGetAngle()", 0);
							if (CurentAngle < angle)
							{
								Lua.DoString(string.Format("VehicleAimIncrement(\"{0}\")", (angle - CurentAngle)));
							}
							if (CurentAngle > angle)
							{
								Lua.DoString(string.Format("VehicleAimDecrement(\"{0}\")", (CurentAngle - angle)));
							}
							Lua.DoString("CastPetAction({0})", 2);
						}
					}
					)),
					new Decorator(ret => !Obj1Done,
					new Action(ret =>
					{
						if(mob2List.Count == 0)
							return;
						mob2List[0].Target();
							
						while(me.CurrentTarget != null && mob2List.Count > 0 && me.CurrentTarget.Guid == mob2List[0].Guid && me.CurrentTarget.IsAlive)
						{
							WoWMovement.ConstantFace(me.CurrentTarget.Guid);
							angle = -((me.Z - me.CurrentTarget.Z) / (me.CurrentTarget.Location.Distance(me.Location))) + ((me.CurrentTarget.Location.Distance2D(me.Location) - 20) / me.CurrentTarget.Location.Distance(me.Location) / 10);
							CurentAngle = Lua.GetReturnVal<double>("return VehicleAimGetAngle()", 0);
							if (CurentAngle < angle)
							{
								Lua.DoString(string.Format("VehicleAimIncrement(\"{0}\")", (angle - CurentAngle)));
							}
							if (CurentAngle > angle)
							{
								Lua.DoString(string.Format("VehicleAimDecrement(\"{0}\")", (CurentAngle - angle)));
							}
							Lua.DoString("CastPetAction({0})", 1);
						}
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

