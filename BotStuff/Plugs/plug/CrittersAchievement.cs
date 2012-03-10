using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx;
using Styx.Helpers;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Forms;
using Styx.Logic.Pathing;
using Styx.Logic.BehaviorTree;
using System.Threading;
using Styx.Logic;
using Styx.Combat.CombatRoutine;
using Styx.Logic.Combat;

namespace CrittersAchievement
{
    public class CrittersAchievement : HBPlugin
    {
        #region Nothing to see here!
        public override string Name { get { return "[CrittersAchievement]"; } }
        public override string Author { get { return "BarryDurex"; } }
        public override Version Version { get { return new Version(1, 0, 6); } }
        public override string ButtonText { get { return "CrittersAchievement"; } }
        public override bool WantButton { get { return true; } }

        private void slog(string format, params object[] args)
        { Logging.Write(System.Drawing.Color.SeaGreen, Name + ": " + format, args); }

        private static List<string> loveAchievement = new List<string>();
        private static List<string> killAchievement = new List<string>();
        private static Stopwatch stuckTimer = new Stopwatch();
        private static Stopwatch retreatTimer = new Stopwatch();
        private long retreatTime;
        LocalPlayer Me = ObjectManager.Me;
        private bool complete = false;
        private bool isInitialize;
        private bool moveBack;
        private WoWPoint backPoint = WoWPoint.Empty;
        #endregion

        public override void Initialize()
        {
            isInitialize = false;
        }
        public override void OnButtonPress()
        {
            updateList(true);
            //Styx.Logic.BehaviorTree.TreeRoot.Start();
        }

        public override void Pulse()
        {
            if (!isInitialize) { updateList(true); isInitialize = true; }
            
            if (!complete && !Me.Combat)
            {
                List<WoWUnit> _Unit = ObjectManager.GetObjectsOfType<WoWUnit>();
                foreach (WoWUnit critterUnit in _Unit)
                {
                    bool achievementFound = false;

                    #region love Achievement
                    if (loveAchievement.Contains(critterUnit.Name))
                    {
                        slog("found: " + critterUnit.Name);
                        achievementFound = true;

                        while (achievementFound)
                        {
                            TreeRoot.StatusText = Name + ": " + Me.Name + " love " + critterUnit.Name;
                            WoWMovement.MoveStop();
                            RunMakro("/tar " + critterUnit.Name);
                            RunMakro("/love");
                            RunMakro("/cleartarget");
                            slog("{0} love {1}", Me.Name, critterUnit.Name);
                            loveAchievement.Remove(critterUnit.Name);
                            achievementFound = false;
                        }
                    }
                    #endregion

                    #region kill Achievement
                    while (killAchievement.Contains(critterUnit.Name) && critterUnit.IsAlive && !Completed(2556) && !Blacklist.Contains(critterUnit.Guid) && !Me.OnTaxi)
                    {
                        //System.Media.SystemSounds.Asterisk.Play();
                        WoWMovement.MoveStop();
                        if (!achievementFound) slog("found: " + critterUnit.Name);
                        achievementFound = true;

                        while ((Me.IsFlying || !Navigator.CanNavigateFully(Me.Location, critterUnit.WorldLocation)) && critterUnit.IsIndoors) 
                        { WoWMovement.ClickToMove(StyxWoW.Me.Location.Add(0, 0, -10)); }
                        while ((Me.IsFlying || !Navigator.CanNavigateFully(Me.Location, critterUnit.WorldLocation)) && !critterUnit.IsIndoors) 
                        { 
                            WoWPoint critterPosi = critterUnit.Location;

                            while (critterPosi.Distance(Me.Location) >= 5)
                            {
                                WoWMovement.ClickToMove(critterPosi);
                                Thread.Sleep(100);
                            }
                        }
                        WoWMovement.MoveStop();

                        while (achievementFound && Navigator.CanNavigateFully(Me.Location, critterUnit.WorldLocation) && !Me.Combat && critterUnit.Distance >= 4)
                        {
                            critterUnit.Face();
                            Thread.Sleep(1500);
                            if (Me.Class == WoWClass.Druid && SpellManager.HasSpell(768)) { slog("(Dismount) switch to {0}", WoWSpell.FromId(768).Name); WoWSpell.FromId(768).Cast(); Thread.Sleep(1500); }
                            else if (Me.Mounted) { slog("Dismount!"); RunMakro("/dismount"); }

                            if (backPoint == WoWPoint.Empty && !Me.IsSwimming) { slog("set retreat position to " + Me.Location.ToString()); backPoint = Me.Location; }

                            if(!retreatTimer.IsRunning) 
                                retreatTimer.Start();

                            if (!Me.Combat && achievementFound)
                            {
                                int maxStuckTimer = (500 * ((int)critterUnit.Distance + 1));
                                //slog("maxStuckTimer: {0}", maxStuckTimer);
                                slog("Move to {0}, Distance: {1}yards", critterUnit.Name, (int)critterUnit.Distance);
                                stuckTimer.Reset(); stuckTimer.Start();

                                while (stuckTimer.ElapsedMilliseconds <= maxStuckTimer && !Me.Combat && critterUnit.Distance >= 2)
                                {
                                    if (backPoint == WoWPoint.Empty && !Me.IsSwimming) { slog("set retreat position to " + Me.Location.ToString()); backPoint = Me.Location; }
                                    TreeRoot.StatusText = (Name + ": Move to " + critterUnit.Name + ", Distance: " + (int)critterUnit.Distance + "yards");
                                    Navigator.MoveTo(critterUnit.Location);
                                    Thread.Sleep(100);
                                }
                                stuckTimer.Stop();

                                if (stuckTimer.ElapsedMilliseconds >= maxStuckTimer)
                                {
                                    Blacklist.Add(critterUnit.Guid, new TimeSpan(0, 5, 0));
                                    achievementFound = false; moveBack = true; stuckTimer.Stop(); Me.ClearTarget();
                                    retreatTimer.Stop(); retreatTime = (retreatTimer.ElapsedMilliseconds + 5000); retreatTimer.Reset();
                                    slog("we stuck! Blacklist - Name: {0} / GuId: {1} - After: {2}ms", critterUnit.Name, critterUnit.Guid, stuckTimer.ElapsedMilliseconds);
                                    TreeRoot.StatusText = Name + " we stuck! Blacklist - Name: " + critterUnit.Name + " / GuId: " + critterUnit.Guid.ToString() + " - After: " + stuckTimer.ElapsedMilliseconds + "ms";
                                    return;
                                }
                            }
                            retreatTimer.Stop(); retreatTime = (retreatTimer.ElapsedMilliseconds + 5000); retreatTimer.Reset();
                            //slog("set retreatTime to: " + retreatTime + "ms");

                            if (Me.Combat) return;
                            WoWMovement.MoveStop();

                            while (!Me.Combat && achievementFound && critterUnit.Distance <= 5)
                            {
                                critterUnit.Target();
                                slog("slay {0}..", critterUnit.Name);

                                stuckTimer.Reset(); stuckTimer.Start();
                                while (critterUnit.IsAlive && stuckTimer.ElapsedMilliseconds <= 10000 && !Me.Combat)
                                {
                                    critterUnit.Interact(); TreeRoot.StatusText = Name + ": slay " + critterUnit.Name;
                                }
                                stuckTimer.Stop();

                                if (stuckTimer.ElapsedMilliseconds >= 10000)
                                {
                                    Blacklist.Add(critterUnit.Guid, new TimeSpan(0, 5, 0));
                                    moveBack = true; achievementFound = false; Me.ClearTarget();
                                    slog("we stuck! Blacklist Name: {0} / GuId: {1}", critterUnit.Name, critterUnit.Guid);
                                    TreeRoot.StatusText = Name + " we stuck! Blacklist Name: " + Name + " / GuId: " + critterUnit.Guid.ToString();
                                }
                                else
                                {
                                    //updateList(false);
                                    killAchievement.Remove(critterUnit.Name);
                                    moveBack = true;
                                    achievementFound = false;
                                }
                            }
                        }
                    }
                    #endregion

                    #region back to retreat position
                    while (moveBack && !Me.Combat && backPoint != WoWPoint.Empty && !achievementFound && !Me.OnTaxi)
                    {
                        if (!IsOutdoors())
                        {
                            slog("Move back to retreat position: " + backPoint.ToString() + " / Distance: " + (int)backPoint.Distance(Me.Location) + "yards");
                            stuckTimer.Reset(); stuckTimer.Start();
                            while (Me.Location.Distance(backPoint) >= 5 && !Me.Combat && stuckTimer.ElapsedMilliseconds <= (retreatTime))
                            {
                                TreeRoot.StatusText = (Name + ": Move back to retreat. Distance: " + (double)backPoint.Distance(Me.Location) + "yards");
                                Navigator.MoveTo(backPoint);
                                Thread.Sleep(100);
                            }
                            stuckTimer.Stop();
                            WoWMovement.MoveStop();

                            if (Me.Combat) return;
                            if (stuckTimer.ElapsedMilliseconds >= (retreatTime)) { slog("sorry we stuck... logout...  :("); Thread.Sleep(2000); RunMakro("/logout"); Thread.Sleep(21000); }
                        }
                        backPoint = WoWPoint.Empty; moveBack = false;
                    }
                    #endregion

                    if (moveBack && backPoint == WoWPoint.Empty) { moveBack = false; }
                    //Logging.WriteDebug(Name + " Achievement done or blacklist!");
                }
            }           
        }

        #region UpdateList
        public void updateList(bool slog_)
        {
            slog("Update search criteria..");
            byte _complete = 0;
            loveAchievement.Clear();
            killAchievement.Clear();
                        
            #region AchievementID 1206
            if (!Completed(1206))
            {
                int done = 0;
                int AchievementMaxCriteria = Lua.GetReturnVal<int>("local value = GetAchievementNumCriteria(1206); return value", 0);

                for (int i = 1; i <= AchievementMaxCriteria; ++i)
                {
                    string value = RunLuaString(1206, i, "quantityString");
                    string name = RunLuaString(1206, i, "criteriaString");

                    if (value.ToInt32() == 0) { loveAchievement.Add(name); ++done; }
                }
                done = (AchievementMaxCriteria - done);
                if (slog_) { slog("[{0}] ({1}/{2})  uncomplete! Search and love..", AchievementName(1206), done, AchievementMaxCriteria); }
            }
            else { if (slog_) { slog("[" + AchievementName(1206) + "] is complete!"); } ++_complete; }
            #endregion

            #region AchievementID 2557
            if (!Completed(2557))
            {
                int done = 0;
                int AchievementMaxCriteria = Lua.GetReturnVal<int>("local value = GetAchievementNumCriteria(2557); return value", 0);

                for (int i = 1; i <= AchievementMaxCriteria; ++i)
                {
                    string value = RunLuaString(2557, i, "quantityString");
                    string name = RunLuaString(2557, i, "criteriaString");

                    if (value.ToInt32() == 0) { loveAchievement.Add(name); ++done; }
                }
                done = (AchievementMaxCriteria - done);
                if (slog_) { slog("[{0}] ({1}/{2})  uncomplete! Search and love..", AchievementName(2557), done, AchievementMaxCriteria); }
            }
            else { if (slog_) { slog("[" + AchievementName(2557) + "] is complete!"); } ++_complete; }
            #endregion

            #region AchievementID 5548
            if (!Completed(5548))
            {
                int done = 0;
                int AchievementMaxCriteria = Lua.GetReturnVal<int>("local value = GetAchievementNumCriteria(5548); return value", 0);

                for (int i = 1; i <= AchievementMaxCriteria; ++i)
                {
                    string value = RunLuaString(5548, i, "quantityString");
                    string name = RunLuaString(5548, i, "criteriaString");

                    if (value.ToInt32() == 0) { loveAchievement.Add(name); ++done; }
                }
                done = (AchievementMaxCriteria - done);
                if (slog_) { slog("[{0}] ({1}/{2})  uncomplete! Search and love..", AchievementName(5548), done, AchievementMaxCriteria); }
            }
            else { if (slog_) { slog("[" + AchievementName(5548) + "] is complete!"); } ++_complete; }
            #endregion

            #region AchievementID 2556
            if (!Completed(2556))
            {
                int done = 0;
                int AchievementMaxCriteria = Lua.GetReturnVal<int>("local value = GetAchievementNumCriteria(2556); return value", 0);

                for (int i = 1; i <= AchievementMaxCriteria; ++i)
                {
                    string value = RunLuaString(2556, i, "quantityString");
                    string name = RunLuaString(2556, i, "criteriaString");

                    if (value.ToInt32() == 0) { killAchievement.Add(name); ++done; }
                }
                done = (AchievementMaxCriteria - done);
                if (slog_) { slog("[{0}] ({1}/{2})  uncomplete! Search and destroy..", AchievementName(2556), done, AchievementMaxCriteria); }
            }
            else { if (slog_) { slog("[" + AchievementName(2556) + "] is complete!"); } ++_complete; }
            #endregion

            if (_complete == 4) { slog("All Achievements are completed! :-)"); complete = true; System.Media.SystemSounds.Asterisk.Play(); }

            //Styx.Logic.BehaviorTree.TreeRoot.Stop();
        }
        #endregion

        #region Lua code
        public string RunLuaString(int achievementID, int criteria, string value)
        {
            //int AchievementMaxCriteria = Lua.GetReturnVal<int>("local value = GetAchievementNumCriteria(1206); return value", 0);
            string Value = Lua.GetReturnVal<string>("local criteriaString, criteriaType, completed, quantity, reqQuantity, charName, flags, assetID, quantityString, criteriaID = GetAchievementCriteriaInfo(" + achievementID + ", " + criteria + "); return " + value + "", 0);
            return Value;
        }

        public bool Completed(int achievementID)
        {
            int idone = 0;
            int AchievementMaxCriteria = Lua.GetReturnVal<int>("local value = GetAchievementNumCriteria(" + achievementID + "); return value", 0);

            for (int i = 1; i <= AchievementMaxCriteria; ++i)
            {
                string value = RunLuaString(achievementID, i, "quantityString");
                if (value.ToInt32() == 1) { ++idone; }
            }

            if (idone == AchievementMaxCriteria)
                return true;
            return false;
        }

        public string AchievementName(int achievementID)
        {
            string name = Lua.GetReturnVal<string>("local IDNumber, Name, Points, Completed, Month, Day, Year, Description, Flags, Image, RewardText, isGuildAch = GetAchievementInfo(" + achievementID + "); return Name", 0);
            return name;
        }

        //private bool KillCriteria(string Name)
        //{
        //    int AchievementMaxCriteria = Lua.GetReturnVal<int>("local value = GetAchievementNumCriteria(2556); return value", 0);

        //    for (int i = 0; i <= AchievementMaxCriteria; ++i)
        //    {
        //        string name = RunLuaString(2556, i, "criteriaString");

        //        if (name == Name)
        //            return true;
        //    }
        //    return false;
        //}

        private void RunMakro(string Makrotext)
        {
            Lua.DoString("RunMacroText(\"" + Makrotext + "\")");
        }

        private bool IsOutdoors()
        {            
            if (Lua.GetReturnVal<int>("local value = IsOutdoors(); return value", 0) == 1) 
                return true;

            return false;
        }
        #endregion

    }
}
