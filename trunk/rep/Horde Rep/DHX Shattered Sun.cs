using System;
using System.Collections.Generic;
using Styx;
using Styx.Helpers;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals;
using Styx.Logic;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using System.Threading;
using System.Diagnostics;
using System.Linq;

namespace DHXShatteredSun
{
    public class DHXShatteredSun : HBPlugin
    {
//        While you mouse over to a button in WoW type /run print(GetMouseFocus():GetName())
//It will print the button name to chat window.
//In HB use Lua.DoString("buttonName:Click()"); to make it click that button.
        #region HBformdetails
        public override string Name { get { return "DHX Shattered Sun"; } }
        public override string Author { get { return "DHX: Protopally"; } }
        public override Version Version { get { return new Version(1, 0); } }
        public override bool WantButton { get { return true; } }
        public override string ButtonText { get { return "Toggle"; } }
        #endregion
        private static LocalPlayer Me { get { return ObjectManager.Me; } }
        List<uint> SSCompQuests = new List<uint>();
        List<uint> SSQuestIDList = new List<uint>();
        bool ley1done = false;
        bool ley2done = false;
        bool ley3done = false;
        bool DOIT = false;
        bool arms1 = false;
        bool arms2 = false;
        int eskillcount = 0;


        public override void Pulse()
        {
            #region pulsechecks
            if (!TreeRoot.IsRunning)
                return;
            if (Me.Combat && !Me.GotTarget)
            {
                GetClosestLiveUnitByID(24978).Target();
            }
            if (Me.Combat || Me.Dead || Me.IsGhost || Me.IsResting)
                return;
            #endregion
            QuestlistUpdate();
            //Logging.WriteDebug(TreeRoot.GoalText);
            if (TreeRoot.GoalText == "Goal: Kill Sunwell - Quest Bunny - Poratl x 1")
                TreeRoot.GoalText = "DHX Goal: Take Poratl Reading";
            if (TreeRoot.GoalText == "Goal: Kill Sunwell - Quest Bunny - Sunwell x 1")
                TreeRoot.GoalText = "DHX Goal: Take BloodCrystal Reading";
            if (TreeRoot.GoalText == "Goal: Kill Sunwell - Quest Bunny - Shrine x 1")
                TreeRoot.GoalText = "DHX Goal: Take Shrine Reading";
            if (TreeRoot.GoalText == "Goal: Kill mob with ID 25086 10 times")
                TreeRoot.GoalText = "DHX Goal: Free 10 Greengill Slaves";
            #region looting fix
            List<WoWUnit> LootMobList = ObjectManager.GetObjectsOfType<WoWUnit>();
            if (!StyxWoW.Me.Combat)
            {
                foreach (WoWUnit unit in LootMobList)
                {
                    if (!unit.IsAlive && unit.CanLoot)
                    {
                        while (StyxWoW.Me.Location.Distance(unit.Location) > 4 || unit.CanLoot)
                        {
                            try
                            {
                                WoWMovement.ClickToMove(unit.Location);
                            }
                            catch { }
                            if (StyxWoW.Me.Location.Distance(unit.Location) <= 4)
                            {
                                try
                                {
                                    if (unit.CanLoot)
                                    {
                                        unit.Interact();
                                        Styx.Logic.Inventory.Frames.LootFrame.LootFrame.Instance.LootAll();
                                        LootMobList.Remove(unit);
                                    }
                                    else if (unit.CanLoot)
                                    {
                                        while (unit.CanLoot)
                                        {
                                            unit.Interact();
                                            Styx.Logic.Inventory.Frames.LootFrame.LootFrame.Instance.LootAll();
                                        }
                                        LootMobList.Remove(unit);
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            if (!StyxWoW.Me.GotTarget && !StyxWoW.Me.Combat && Styx.Logic.LootTargeting.Instance.LootingList.Count == 0)
            {
                LootMobList.Clear();
            }
            #endregion
            #region bloodberry bush loot reliability
            WoWObject bloddberrybush = GetClosestObjectByID(187333);
            if (bloddberrybush != null)
            {
                if (bloddberrybush.Distance < 5 && !Me.Combat)
                {
                    bloddberrybush.Interact();
                }
            }
            #endregion
            #region emissary of hate
            if(InvChkByID(34414))
            {
                if (Me.CanLoot || Me.Looting)
                    return;
                WoWUnit emissary = GetUnitByID(25003);
                if (emissary != null)
                {
                    if (emissary.Dead && emissary.Distance < 10)
                    {
                        useItemByID(34414);
                    }
                }
            }
            #endregion
            #region dont stop now
            ObjectManager.Update();
            while (KeyChkByID(34477) && InvTotalByID(34479) < 3 && IsQuestCompleted(11547))
            {
                Logging.WriteDebug("found key");
                WoWObject orechest = GetClosestObjectByID(187264);
                if (orechest != null)
                {
                    Logging.WriteDebug("found chest");
                    while (orechest.Distance > 3)
                    {
                        if (Me.Combat || Me.Dead || Me.IsGhost)
                            return;
                        Navigator.MoveTo(orechest.Location);
                        if (orechest.Distance < 5)
                        {
                            Navigator.Clear(); WoWMovement.MoveStop();
                            orechest.Interact();
                            Thread.Sleep(3000);
                        }
                        ObjectManager.Update();
                        return;
                    }
                }
            }
            while (!IsQuestCompleted(11536) && TreeRoot.GoalText == "Goal: Collect Darkspine Iron Ore x 3" && !KeyChkByID(34477))
            {
                if (Me.Combat || Me.Dead || Me.IsGhost)
                    return;
                WoWUnit oretarget = GetClosestLiveUnitByID(25060);
                oretarget.Target();
                oretarget.Face();
                Me.ToggleAttack();
                if (oretarget.Distance > 5)
                    Navigator.MoveTo(oretarget.Location);
            }
            #endregion
            #region pay 10 gold
            if (SSQuestIDList.Contains(11548))
            {
                Lua.DoString("StaticPopup1Button1:Click()");
            }
            #endregion

            #region greengill
            if (Me.CanLoot || Me.Looting)
                return;
            if (InvChkByID(34483))
            {
                WoWUnit greengilltarget = GetClosestUnitByID(25084);
                if (greengilltarget != null)
                {
                    if (greengilltarget.Distance < 36)
                    {
                        useItemByID(34483);
                        Styx.Logic.Combat.SpellManager.ClickRemoteLocation(greengilltarget.Location);
                        Thread.Sleep(4000);
                    }
                }
            }
            #endregion
            if (InvTotalByID(34338) >= 4)
                arms1 = true;
            #region turnin Arm the Wards
            if ((TreeRoot.GoalText == "Goal: Collect Mana Remnants x 4" || TreeRoot.GoalText == "Goal: TurnIn Arm the Wards!") && IsQuestCompleted(11523) && SSQuestIDList.Contains(11523) && !SSCompQuests.Contains(11523))
            {
                bool handindone = false;
                WoWPoint armsgoto = new WoWPoint(12892.95, -6880.174, 9.040649);
                while (armsgoto.Distance(Me.Location) > 0 || !handindone)
                {
                    if (TreeRoot.GoalText != "Goal: TurnIn Arm the Wards!")
                    {
                        TreeRoot.GoalText = "Goal: TurnIn Arm the Wards!";
                    }
                    Navigator.MoveTo(armsgoto);
                    while (armsgoto.Distance(Me.Location) < 3 && !handindone)
                    {
                        Navigator.Clear();
                        WoWMovement.MoveStop();
                        GetUnitByID(24967).Interact();
                        Thread.Sleep(2000);
                        Lua.DoString("GossipTitleButton1:Click()");
                        Thread.Sleep(2000);
                        QuestBot.QuestHelper.QuestFrame.CompleteQuest();
                        handindone = true;
                        return;
                    }
                }
            }
            #endregion
            #region ES
            if (SSQuestIDList.Contains(11525) && InvChkByID(34368) && !IsQuestCompleted(11525) && (!Me.CanLoot || !Me.Looting))
            {
                ObjectManager.Update();
                if (Me.Dead || Me.Combat || Me.IsGhost || Me.CanLoot || Me.Looting || Me.IsResting)
                    return;
                if (!IsQuestCompleted(11525))
                {
                    if (Me.Dead || Me.Combat || Me.IsGhost || Me.CanLoot || Me.Looting || Me.IsResting)
                        return;
                    WoWUnit ClosestUnit = GetClosestUnitByID(24972);
                    if (ClosestUnit == null)
                        return;
                    if (ClosestUnit.KilledByMe)
                    {
                        while (ClosestUnit.Distance > 5)
                            {
                                if (Me.Dead || Me.Combat || Me.IsGhost || Me.CanLoot || Me.Looting || Me.IsResting)
                                {
                                    Navigator.Clear(); WoWMovement.MoveStop();
                                    return;
                                }
                                Navigator.MoveTo(ClosestUnit.Location);
                            }
                            TargetByGUID(ClosestUnit.Guid);
                            Navigator.Clear(); WoWMovement.MoveStop();
                            useItemByID(34368);
                            Thread.Sleep(3000);
                            return;
                    }
                        if (ClosestUnit.Entry == 24972 && !ClosestUnit.TaggedByOther && !ClosestUnit.TaggedByMe && !ClosestUnit.Dead && !IsQuestCompleted(11525))
                        {
                            Logging.Write("found 1 alive");
                            if (ClosestUnit.Distance < 30)
                            {
                                ClosestUnit.Target();
                                Me.ToggleAttack();
                                while (!Me.Combat && ClosestUnit.Distance < 50)
                                {
                                    if (Me.Dead || Me.Combat || Me.IsGhost || Me.CanLoot || Me.Looting || Me.IsResting)
                                    {
                                        Navigator.Clear(); WoWMovement.MoveStop();
                                        return;
                                    }
                                    Navigator.MoveTo(ClosestUnit.Location);
                                    Me.ToggleAttack();
                                }
                            }
                            return;
                        }
                }
            }
            #endregion
          while (arms1 && !IsQuestCompleted(11523) && IsQuestCompleted(11525))
            {
                if (Me.Dead || Me.Combat || Me.IsGhost || Me.CanLoot || Me.Looting || Me.IsResting)
                    return;
                WoWPoint armsgoto = new WoWPoint(12913.55, -6932.246, 3.872839);
                while (armsgoto.Distance(Me.Location) > 0)
                {
                    if (Me.Dead || Me.Combat || Me.IsGhost || Me.CanLoot || Me.Looting || Me.IsResting)
                        return;
                    Navigator.MoveTo(armsgoto);
                    if (armsgoto.Distance(Me.Location) < 5)
                    {
                        useItemByID(34338);
                        Thread.Sleep(5000);
                        return;
                    }
                }
                return;
            }
            #region Know your ley lines
            if (InvChkByID(34533))
            {
                WoWObject ley1 = GetClosestObjectByID(25156);
                WoWObject ley2 = GetClosestObjectByID(25157);
                WoWObject ley3 = GetClosestObjectByID(25154);
                if (ley1 != null && !ley1done && ley1.Distance2D <5)
                {
                    Logging.WriteDebug("ley 1");
                    Thread.Sleep(2000);
                    Navigator.Clear(); WoWMovement.MoveStop();
                    useItemByID(34533);
                    Thread.Sleep(6000);
                    ley1done = true;
                    
//<Hotspot X="12575.54" Y="-6915.316" Z="4.602137" />

                }
                if (ley2 != null && !ley2done && ley2.Distance2D < 5)
                {
                    Logging.WriteDebug("ley 2");
                    Thread.Sleep(2000);
                    Navigator.Clear(); WoWMovement.MoveStop();
                    useItemByID(34533);
                    Thread.Sleep(6000);
                    ley2done = true;
                }
                //<Hotspot X="12771.83" Y="-6705.073" Z="2.288224" />
                if (ley3 != null && !ley3done && ley3.Distance2D < 5)
                {
                    Logging.WriteDebug("ley 2");
                    Thread.Sleep(2000);
                    Navigator.Clear(); WoWMovement.MoveStop();
                    useItemByID(34533);
                    Thread.Sleep(6000);
                    ley3done = true;
                }
            }
            #endregion
        }
        public override void OnButtonPress(){}
        public static uint InvTotalByID(uint ID)///returns total number of ID item in inventory.
        {
            ObjectManager.Update();
            List<WoWItem[]> bags = new List<WoWItem[]>();
            bags.Add(ObjectManager.Me.Inventory.Backpack.PhysicalItems);
            bags.Add(ObjectManager.Me.GetBag(WoWBagSlot.Bag1).Bag.PhysicalItems);
            bags.Add(ObjectManager.Me.GetBag(WoWBagSlot.Bag2).Bag.PhysicalItems);
            bags.Add(ObjectManager.Me.GetBag(WoWBagSlot.Bag3).Bag.PhysicalItems);
            bags.Add(ObjectManager.Me.GetBag(WoWBagSlot.Bag4).Bag.PhysicalItems);
            uint itemcount = 0;
            foreach (WoWItem[] bag in bags)
            {
                try
                {
                    foreach (WoWItem item in bag)
                    {
                        try
                        {
                            if (item.Entry == ID)
                                itemcount = itemcount + item.StackCount;
                        }
                        catch
                        {//empty slot
                        }
                    }
                }
                catch (Exception e)
                { Logging.Write(e.Message); }
            }
            return itemcount;
        }
        public void QuestlistUpdate()
        {
            List<uint> cqtemp = QuestBot.QuestManager.GetCompletedQuests();
            List<Styx.Logic.Questing.PlayerQuest> questLog = ObjectManager.Me.QuestLog.GetAllQuests();
            SSQuestIDList.Clear();
            foreach (Styx.Logic.Questing.PlayerQuest q in questLog)
            {
                SSQuestIDList.Add(q.Id);
                int[] ItemIDList = q.CollectItemIDs;
                int[] IntermediatItemIdList = q.CollectIntermediateItemIDs;
            }
            SSCompQuests.Clear();
            foreach (uint cq in cqtemp)
            {
                SSCompQuests.Add(cq);
            }
            SSCompQuests.Sort();
        }
        public static bool InvChkByID(uint ID) ///returns ture if item with ID is in inventory else false.
        {
            ObjectManager.Update();
            List<WoWItem[]> bags = new List<WoWItem[]>();
            bags.Add(ObjectManager.Me.Inventory.Backpack.PhysicalItems);
            bags.Add(ObjectManager.Me.GetBag(WoWBagSlot.Bag1).Bag.PhysicalItems);
            bags.Add(ObjectManager.Me.GetBag(WoWBagSlot.Bag2).Bag.PhysicalItems);
            bags.Add(ObjectManager.Me.GetBag(WoWBagSlot.Bag3).Bag.PhysicalItems);
            bags.Add(ObjectManager.Me.GetBag(WoWBagSlot.Bag4).Bag.PhysicalItems);
            foreach (WoWItem[] bag in bags)
            {
                try
                {
                    foreach (WoWItem item in bag)
                    {
                        try
                        {
                            if (item.Entry == ID) { return true; }
                        }
                        catch { }
                    }
                }
                catch (Exception e) { Logging.Write(e.Message); }
            }
            return false;
        }
        public static void useItemByID(uint ID) ///Uses Item by WoWHead ID.
        {
            Lua.DoString("UseItemByName(\"" + ID + "\")");
        }
        public static WoWUnit GetUnitByID(uint ID) /// Get unit by WoWHeadID
        {
            ObjectManager.Update();
            foreach (WoWUnit gunit in ObjectManager.GetObjectsOfType<WoWUnit>())
                if (gunit.Entry == ID)
                    return gunit;
            return null;
        }
        public static WoWUnit GetClosestUnitByID(uint ID) /// Get Closest unit With WoWHeadID
        {
            WoWUnit closest = null;
            double closestDist = double.MaxValue;
            foreach (WoWUnit u in ObjectManager.GetObjectsOfType<WoWUnit>())
            {
                if (u.IsFriendly || u.Entry != ID)
                    continue;
                double dist = u.DistanceSqr;
                if (dist < closestDist && u.InLineOfSight)
                {
                    closestDist = dist;
                    closest = u;
                }
            }
            return closest;
        }
        public static WoWUnit GetClosestLiveUnitByID(uint ID) /// Get Closest live unit With WoWHeadID
        {
            WoWUnit closest = null;
            double closestDist = double.MaxValue;
            foreach (WoWUnit u in ObjectManager.GetObjectsOfType<WoWUnit>())
            {
                if (u.IsFriendly || u.Entry != ID || u.Dead)
                    continue;
                double dist = u.DistanceSqr;
                if (dist < closestDist && u.InLineOfSight)
                {
                    closestDist = dist;
                    closest = u;
                }
            }
            return closest;
        }
        public static WoWObject GetClosestObjectByID(uint ID) /// Get Closest Object with WoWHeadID
        {
            WoWObject closest = null;
            double closestDist = double.MaxValue;
            foreach (WoWObject u in ObjectManager.ObjectList)
            {
                if (u.Entry != ID)
                    continue;
                double dist = u.DistanceSqr;
                if (dist < closestDist)//&& u.InLineOfSight
                {
                    closestDist = dist;
                    closest = u;
                }
            }
            return closest;
        }
        public static bool TargetByGUID(ulong GUID) ///Targets Mob by its GUID and returns true else false if not found.
        {
            ObjectManager.Update();
            List<WoWUnit> WoWUnitList = ObjectManager.GetObjectsOfType<WoWUnit>();
            foreach (WoWUnit target in WoWUnitList)
            {
                if (target.Guid == GUID)
                {
                    target.Target();
                    return true;
                }
            }
            return false;
        }
        private static bool IsQuestCompleted(uint ID)
        {
            //to make sure every header is expanded in quest log
            Lua.DoString("ExpandQuestHeader(0)");
            //number of values in quest log (includes headers like "Durator")
            int QuestCount = Lua.GetReturnVal<int>("return select(1, GetNumQuestLogEntries())", 0);
            for (int i = 1; i <= QuestCount; i++)
            {
                List<string> QuestInfo = Lua.LuaGetReturnValue("return GetQuestLogTitle(" + i + ")", "raphus.lua");

                //pass if the index isHeader or isCollapsed
                if (QuestInfo[4] == "1" || QuestInfo[5] == "1")
                    continue;

                string QuestStatus = null;
                if (QuestInfo[6] == "1")
                    QuestStatus = "completed";
                else if (QuestInfo[6] == "-1")
                    QuestStatus = "failed";
                else
                    QuestStatus = "in progress";
                if (QuestInfo[8] == Convert.ToString(ID) && QuestStatus == "completed")
                {
                    return true;
                }
            }
            return false;
        }
        public static bool KeyChkByID(uint ID)///checks for keys by ID true if found else false.
        {
            ObjectManager.Update();
            List<WoWItem[]> MyKeys = new List<WoWItem[]>();
            MyKeys.Add(ObjectManager.Me.Inventory.Keyring.Items);
            foreach (WoWItem[] keyring in MyKeys)
            {
                try
                {
                    foreach (WoWItem key in keyring)
                    {
                        try
                        {
                            if (key.Entry == ID)
                                return true;
                        }
                        catch
                        {//empty slot
                        }
                    }
                }
                catch (Exception e)
                { Logging.Write(e.Message); }
            }
            return false;
        }
    }
}
