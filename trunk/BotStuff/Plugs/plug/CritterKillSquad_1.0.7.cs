// CritterKillSquad by BarryDurex
// Credits: eGB by erenion | PoolFishingBuddy by iggi66

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx;
using TreeSharp;
using Action = TreeSharp.Action;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.Logic.Pathing;
using Styx.Logic.Combat;
using System.Threading;
using Styx.Helpers;
using System.Diagnostics;
using Styx.Logic;
using Styx.Logic.POI;
using Styx.Logic.BehaviorTree;
using Bots.Grind;
using Styx.Logic.Profiles;
using Styx.Logic.AreaManagement;
using CommonBehaviors.Actions;
using System.Windows.Forms;
using Styx.Combat.CombatRoutine;
using Styx.WoWInternals.World;
using Styx.Logic.Profiles.Quest;

namespace CritterKillSquad
{
    public class BotCKS : BotBase
    {
        public override PulseFlags PulseFlags
        {
            get { return new PulseFlags(); }
        }
        private readonly Version _version = new Version(1, 0, 7);
        public override string Name { get { return "CritterKillSquad [v" + _version + "] beta"; } }
        
        public static double DistToHarvest = 100;
        //public static string MountName = "Tawny Wind Rider";
        public static int blackListTime = 20;//minutes
        public static List<string> blacklistMobs = new List<string>()//MUST BE LOWER CASE
        {
            "baron geddon"
        };
        static int numHarvestAttempts = 5;//number of fails before blacklisting


        static Dictionary<Stopwatch, WoWPoint> blackList = new Dictionary<Stopwatch, WoWPoint>();
        static Dictionary<int, string> mounts = new Dictionary<int, string>();
        static public List<WoWPoint> HotspotList;
        static public List<WoWPoint> BlackspotList;
        private static List<ulong> KillList = new List<ulong>();
        private static int KillListCount = 0;
        public static int AOEcount = 0;
        private string AchDone = string.Empty;
        public static Random ran = new Random();
        private static Stopwatch runTimer = new Stopwatch();
        private static LocalPlayer Me = ObjectManager.Me;
        //public static readonly TalentManager talent = new TalentManager();

        static int _pullDis = 0;
        static bool _findMountAuto = false;
        static bool _useRndMount = false;
        static bool _findVenAuto = false;
        static bool _learnFP = false;

        public static int maxTimeFollowed = 300;//Max number of seconds to be followed by another player before hearthing or logging out
        public static bool useHearth = true;//Whether or not to use the hearthstone
        public static bool useHearthAlways = true;//If set to false, when stuck, will log out of WoW instead of waiting on hearthstone to cooldown
        //Do not edit below this line

        private static Stopwatch sw = new Stopwatch();
        private static Stopwatch swHearth = new Stopwatch();
        static int numStuck = 0;
        static int hearthstoneCooldownTime = 0;

        static public string TimeNow { get { return DateTime.Now.ToString("hh:mm:ss tt", System.Globalization.DateTimeFormatInfo.InvariantInfo); } }
        static public GrindArea GrindArea { get; set; }

        static public void slog(string format, params object[] args)
        { Logging.Write(System.Drawing.Color.SeaGreen, "CritterKillSquad: " + format, args); }

        static public void dlog(string format, params object[] args)
        {
            Logging.WriteDebug(System.Drawing.Color.Red, "######## CritterKillSquad ########");
            Logging.WriteDebug(System.Drawing.Color.Red, format, args);
            Logging.WriteDebug(System.Drawing.Color.Red, "##################################");
        }

        public override void Start()
        {
            Logging.Write(System.Drawing.Color.ForestGreen, "{0} - {1} starting!", TimeNow, Name);
            //Logging.Write(System.Drawing.Color.ForestGreen, "                          - Optimized for Paladin, Shaman & Priest");

            if (!isCompleted(5144))
            {
                AchDone = AchievementValue(5144);
                Logging.Write(System.Drawing.Color.DarkOliveGreen, "[{0}] | [{1}]", AchievementName(5144), AchDone);
            }

            _pullDis = CharacterSettings.Instance.PullDistance;
            _findMountAuto = CharacterSettings.Instance.FindMountAutomatically;
            _useRndMount = CharacterSettings.Instance.UseRandomMount;
            _findVenAuto = CharacterSettings.Instance.FindVendorsAutomatically;
            _learnFP = CharacterSettings.Instance.LearnFlightPaths;

            if (Me.Class == WoWClass.Paladin ||
                Me.Class == WoWClass.DeathKnight ||
                Me.Class == WoWClass.Druid ||
                Me.Class == WoWClass.Rogue ||
                Me.Class == WoWClass.Warrior)

                CharacterSettings.Instance.PullDistance = 10;
            else
                CharacterSettings.Instance.PullDistance = 20;

            slog("reset kill counter");
            KillListCount = 0;
            KillList.Clear();

            if (mounts.Count == 0)
            {
                slog("create MountList ...");
                findMounts();
            }

            CharacterSettings.Instance.FindMountAutomatically = false;
            CharacterSettings.Instance.UseRandomMount = false;

            Styx.BotEvents.OnBotStart += Init;
            Styx.BotEvents.OnBotStop += Final;

            if (TreeRoot.IsRunning)
                Init(new System.EventArgs());

            StyxSettings.Instance.LogoutForInactivity = false;

        }

        #region Achievement
        public string AchievementName(int achievementID)
        {
            string name = Lua.GetReturnVal<string>("local IDNumber, Name, Points, Completed, Month, Day, Year, Description, Flags, Image, RewardText, isGuildAch = GetAchievementInfo(" + achievementID + "); return Name", 0);
            return name;
        }

        public string AchievementValue(int achievementID)
        {
            int AchievementMaxCriteria = Lua.GetReturnVal<int>("local value = GetAchievementNumCriteria(" + achievementID + "); return value", 0);
            string Value = Lua.GetReturnVal<string>("local criteriaString, criteriaType, completed, quantity, reqQuantity, charName, flags, assetID, quantityString, criteriaID = GetAchievementCriteriaInfo(" + achievementID + ", " + AchievementMaxCriteria + "); return quantityString", 0);
            return Value;
        }

        public bool isCompleted(int achievementID)
        {
            int AchievementMaxCriteria = Lua.GetReturnVal<int>("local value = GetAchievementNumCriteria(" + achievementID + "); return value", 0);
            int Value = Lua.GetReturnVal<int>("local criteriaString, criteriaType, completed, quantity, reqQuantity, charName, flags, assetID, quantityString, criteriaID = GetAchievementCriteriaInfo(" + achievementID + ", " + AchievementMaxCriteria + "); return quantity", 0);

            if (Value == 50000)
                return true;
            return false;
        }
        #endregion

        public override void Stop()
        {
            while (StyxWoW.Me.IsCasting)
            {
                SpellManager.StopCasting();
                Thread.Sleep(100);
            }
            if (AchDone != string.Empty)
                Logging.Write(System.Drawing.Color.DarkOliveGreen, "[{0}] before: [{1}] / now: [{2}]", AchievementName(5144), AchDone, AchievementValue(5144));

            Logging.Write(System.Drawing.Color.ForestGreen, "{0} - Runtime: {1}h {2}m {3}s.", TimeNow, runTimer.Elapsed.Hours, runTimer.Elapsed.Minutes, runTimer.Elapsed.Seconds);
            Logging.Write(System.Drawing.Color.ForestGreen, "{0} - Kills: {1}", TimeNow, KillListCount);
            Logging.Write(System.Drawing.Color.ForestGreen, "{0} - {1} stopped!", TimeNow, Name);
            StyxSettings.Instance.LogoutForInactivity = true;
            runTimer.Reset();

            CharacterSettings.Instance.PullDistance = _pullDis;
            CharacterSettings.Instance.FindMountAutomatically = _findMountAuto;
            CharacterSettings.Instance.UseRandomMount = _useRndMount;
            CharacterSettings.Instance.FindVendorsAutomatically = _findVenAuto;
            CharacterSettings.Instance.LearnFlightPaths = _learnFP;

            if (!TreeRoot.IsRunning)
                Final(new System.EventArgs());
        }

        public static List<WoWPoint> profile
        {
            get
            {
                CharacterSettings.Instance.FindVendorsAutomatically = true;
                CharacterSettings.Instance.LearnFlightPaths = false;
                if (StyxWoW.Me.ZoneId == 33)
                {
                    return Stranglethorn;
                }
                return BoreanischeTundra;
            }
        }
        #region Profile
        static List<WoWPoint> Stranglethorn = new List<WoWPoint>
        {
            new WoWPoint(11973.03,-1702.655,83.05856),
            new WoWPoint(-12007.87,-1713.329,77.01167),
            new WoWPoint(-12011.49,-1678.677,74.86162)
        };

        static List<WoWPoint> BoreanischeTundra = new List<WoWPoint>
        {
            new WoWPoint(2209.217,4989.452,80.83177),
            new WoWPoint(2068.127,5001.674,78.02212),
            new WoWPoint(1933.623,5027.523,75.9446),
            new WoWPoint(1830.544,5129.574,75.88148),
            new WoWPoint(1751.906,5245.666,76.82143),
            new WoWPoint(1704.497,5382.203,80.62263),
            new WoWPoint(1691.436,5525.783,74.76878),
            new WoWPoint(1678.711,5670.154,68.88467),
            new WoWPoint(1666.019,5814.153,63.01569),
            new WoWPoint(1653.259,5958.924,57.11529),
            new WoWPoint(1640.533,6103.294,51.23118),
            new WoWPoint(1627.841,6247.294,45.3622),
            new WoWPoint(1691.343,6374.819,44.41555),
            new WoWPoint(1763.134,6500.424,45.32064),
            new WoWPoint(1836.197,6626.048,50.11084),
            new WoWPoint(1908.589,6749.804,56.76795),
            new WoWPoint(2004.504,6855.326,56.94577),
            new WoWPoint(2117.726,6945.991,56.88276),
            new WoWPoint(2229.76,7035.707,56.8204),
            new WoWPoint(2350.716,7112.065,55.81729),
            new WoWPoint(2487.033,7161.68,53.81307),
            new WoWPoint(2622.945,7211.146,51.8148),
            new WoWPoint(2750.656,7275.125,56.88971),
            new WoWPoint(2876.296,7345.341,68.40714),
            new WoWPoint(2992.7,7285.511,76.28207),
            new WoWPoint(2876.296,7345.341,68.40714),
            new WoWPoint(2750.656,7275.125,56.88971),
            new WoWPoint(2622.945,7211.146,51.8148),
            new WoWPoint(2487.033,7161.68,53.81307),
            new WoWPoint(2350.716,7112.065,55.81729),
            new WoWPoint(2229.76,7035.707,56.8204),
            new WoWPoint(2117.726,6945.991,56.88276),
            new WoWPoint(2004.504,6855.326,56.94577),
            new WoWPoint(1908.589,6749.804,56.76795),
            new WoWPoint(1836.197,6626.048,50.11084),
            new WoWPoint(1763.134,6500.424,45.32064),
            new WoWPoint(1691.343,6374.819,44.41555),
            new WoWPoint(1627.841,6247.294,45.3622),
            new WoWPoint(1640.533,6103.294,51.23118),
            new WoWPoint(1653.259,5958.924,57.11529),
            new WoWPoint(1666.019,5814.153,63.01569),
            new WoWPoint(1678.711,5670.154,68.88467),
            new WoWPoint(1691.436,5525.783,74.76878),
            new WoWPoint(1704.497,5382.203,80.62263),
            new WoWPoint(1751.906,5245.666,76.82143),
            new WoWPoint(1830.544,5129.574,75.88148),
            new WoWPoint(1933.623,5027.523,75.9446),
            new WoWPoint(2068.127,5001.674,78.02212)
        };
        #endregion
        
        static private void findMounts()
        {
            if (mounts.Count == 0)
            {
                WoWSpell SwiftFlightForm = WoWSpell.FromId(40120);
                WoWSpell FlightForm = WoWSpell.FromId(33943);

                if (SpellManager.HasSpell(SwiftFlightForm))
                {
                    mounts.Add(SwiftFlightForm.Id, SwiftFlightForm.Name);
                    return;
                }
                else if (SpellManager.HasSpell(FlightForm))
                {
                    mounts.Add(FlightForm.Id, FlightForm.Name);
                    return;
                }
                else
                {
                    foreach (MountHelper.MountWrapper flyingMount in MountHelper.FlyingMounts)
                    {
                        mounts.Add(flyingMount.CreatureSpellId, flyingMount.Name);
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Does things on init.
        /// </summary>
        static public void Init(System.EventArgs args)
        {
            runTimer.Start();

            if (!StyxWoW.FlightChecksDisabled)
            {
                Logging.Write(System.Drawing.Color.Red, "{0} - You don't have lifetime subscription or valid paid plugin key. Stopping..", TimeNow);
                TreeRoot.Stop();
            }

            if (mounts.Count == 0 && TreeRoot.IsRunning)
            {
                Logging.Write(System.Drawing.Color.Red, "{0} - Can not find any flying mount..", TimeNow);
                TreeRoot.Stop();
            }

            //slog("ZONE: " + StyxWoW.Me.ZoneId + " / " + StyxWoW.Me.ZoneText);
            if (StyxWoW.Me.ZoneId == 3537 || StyxWoW.Me.ZoneId == 33)
            {}
            else
            {
                Logging.Write(System.Drawing.Color.Red, "{0} - You are not in the Borean Tundra or Northern Stranglethorn, please go there!", TimeNow);
                TreeRoot.Stop();
            }
        }

        /// <summary>
        /// Does things on final.
        /// </summary>
        static public void Final(System.EventArgs args)
        {

        }

        private Composite _root;
        public override Composite Root
        {
            get
            {
                return _root ?? (_root =
                    new Sequence(
                    CreateFollowBehaviour(),

                    new PrioritySelector(

                        new Decorator(ret => StyxWoW.Me == null || !StyxWoW.IsInGame || !StyxWoW.IsInWorld,
                            new Sequence(
                                new Action(ret => Logging.Write(System.Drawing.Color.Red, "{0} - [INVALID] Waiting..", TimeNow)),
                                new ActionSleep(10000))),

                        new Decorator(ret => StyxWoW.Me.OnTaxi || StyxWoW.Me.IsOnTransport,
                            new Sequence(
                                new Action(ret => Logging.Write(System.Drawing.Color.Red, "{0} - [On Taxi] Waiting..", TimeNow)),
                                new ActionSleep(10000))),

                        new Decorator(ret => StyxWoW.Me.IsFalling,
                            new Sequence(
                                new Action(ret => Logging.Write(System.Drawing.Color.Red, "{0} - [Falling] Waiting..", TimeNow)),
                                new ActionSleep(500))),

                        new Decorator(ret => StyxWoW.Me.Dead || StyxWoW.Me.IsGhost,
                            LevelBot.CreateDeathBehavior()),

                        //new Decorator(ret => !StyxWoW.Me.Mounted && StyxWoW.Me.Combat,
                        //    new Sequence(
                        //        new Action(ret => Logging.Write(System.Drawing.Color.Red, "{0} - [COMBAT] Waiting..", TimeNow)),
                        //        new Action(ret => LevelBot.CreateCombatBehavior())
                        //        )),
                        
                        new Decorator(ret => StyxWoW.Me.Mounted && StyxWoW.Me.Combat,
                            new Sequence(
                                new Action(ret => FlyUp())
                                )),

                        //new Decorator(ret => !StyxWoW.Me.IsFlying,
                        //LevelBot.CreateVendorBehavior()
                        //),

                #region Buffs
                        new Decorator(ret => !StyxWoW.Me.IsFlying,
                            new PrioritySelector(
                    // Use the bt
                                new Decorator(ctx => RoutineManager.Current.PreCombatBuffBehavior != null,
                                    RoutineManager.Current.PreCombatBuffBehavior),
                    // don't use the bt
                                new Decorator(
                                    ctx => RoutineManager.Current.NeedPreCombatBuffs,
                                    new Sequence(
                                        new Action(ret => TreeRoot.StatusText = "Applying pre-combat buffs"),
                                        new Action(ret => RoutineManager.Current.PreCombatBuff())
                                        )))),

                #endregion

                        new Decorator(ret => (!Me.Combat && !Me.Mounted && (getNode() == null || getNode().Location.Distance(Me.Location) > 18) || Me.IsSwimming),//mount
                            new Action(ret => mount())
                            ),

                        new Decorator(ret => StyxWoW.Me.HealthPercent < 70 && StyxWoW.Me.IsAlive && StyxWoW.Me.Mounted,
                            new Sequence(
                                new Action(ret => FlyToRest()),
                                new Action(ret => WaitForHP())
                                )
                            ),

                        CreateInteractBehaviour(),

                        new Decorator(ret => getNode() == null && Me.Mounted,

                        CreateMoveBehaviour()
                        )
                        )
                    ));
            }
        }

        public static Composite CreateFollowBehaviour()
        {
            return new Action(ret => PlayerFollow());
        }

        public static void PlayerFollow()
        {
            if (!sw.IsRunning)
            {
                sw.Start();
            }

            if (!useHearth)
            {
                useHearthAlways = false;
            }

            if (swHearth.Elapsed.TotalSeconds > 0)
            {
                hearthstoneCooldownTime = 1800 - (int)Math.Floor((double)swHearth.Elapsed.TotalSeconds); // Hearthstone Cooldown time in seconds
            }
            else
            {
                hearthstoneCooldownTime = 0;
            }

            if (sw.Elapsed.TotalSeconds > 5)
            {
                sw.Reset();
                sw.Start();

                #region Player Follow
                PlayerManager.update();
                if (PlayerManager.needExit(maxTimeFollowed) && !(Battlegrounds.IsInsideBattleground))
                {
                    string followedMessage = "Followed for too long by player " + PlayerManager.exitingPlayer.Name + ". Followed for " + PlayerManager.players[PlayerManager.exitingPlayer] + " seconds!";
                    if (useHearth)
                    {
                        // Attempt to hearth out first
                        Logging.Write(followedMessage);
                        if (hearthstoneCooldownTime == 0)
                        {
                            useHearthstone();
                            // Reset the hearthstone cooldown timer
                            swHearth.Reset();
                            swHearth.Start();
                        }
                        else
                        {
                            Logging.Write("Waiting for Hearthstone... " + hearthstoneCooldownTime + " seconds remaining");
                        }
                    }
                    if (useHearthAlways == false)
                    {
                        // Exit WoW *after* Hearthing
                        exitWoW(followedMessage);
                    }

                }
                #endregion                

            }
        }

       static void exitWoW(string message)//Made by MaiN
        {
            Logging.Write("Exiting wow! " + message);
            Lua.DoString("ForceQuit()");  // Lua.DoString doesn't seem to run if bot is stopped, thus I placed it here.
            if (Styx.Logic.BehaviorTree.TreeRoot.IsRunning)
            {
                Styx.Logic.BehaviorTree.TreeRoot.Stop();
            }
            int id = ObjectManager.Wow.ProcessId;
            int tickCount = Environment.TickCount;
            while (Environment.TickCount - tickCount < 10000 && Process.GetProcessesByName("Wow").Where(proc => proc.Id == id).ToList().Count > 0)
            {
                Thread.Sleep(200);
            }

            Process process;
            if ((process = Process.GetProcessesByName("Wow").Where(proc => proc.Id == id).FirstOrDefault()) != null)
            {
                process.Kill();
            }
            Application.Exit();
        }

        static bool useHearthstone()//Made by Tombstone
        {
            Logging.Write("Attempting to use Hearthstone!");
            // Search bags for hearthstone
            WoWItem hearthStone = ObjectManager.Me.BagItems.Where(o => o.Entry == 6948).FirstOrDefault();
            if (hearthStone != null)
            {
                hearthStone.Interact();
                Logging.Write("Pausing for 20 seconds");
                Thread.Sleep(20000);
                return true;
            }
            else
            {
                exitWoW("Unable to find Hearthstone in bags!");
                return false;
            }
        }




        public static void FlyUp()
        {
            dlog("fly up ...");
            KeyboardManager.PressKey((char)Keys.Space);
            Thread.Sleep(ran.Next(300, 600));
            KeyboardManager.ReleaseKey((char)Keys.Space);
        }

        public static void FlyToRest()
        {
            ObjectManager.Update();
            WoWMovement.ClickToMove(StyxWoW.Me.Location.Add(0,0,(float)60));
        }

        public static void WaitForHP()
        {
            Logging.Write("Resting!");
            while (StyxWoW.Me.HealthPercent < 70)
            {
                Thread.Sleep(250);
                if (StyxWoW.Me.Combat)
                {
                    return;
                }
            }
        }

        #region CreateInteractBehaviour
        public static Composite CreateInteractBehaviour()
        {
            return new Decorator(ret => (isNode() && getNode().IsAlive), //!StyxWoW.Me.Combat
                new Sequence(
                    //new Action(ret => dlog("moveToHarvestNode")),
                    new Action(ret => moveToHarvestNode()),
                    //new Action(ret => dlog("dismount?")),

                    //new Action(ret => dlog("interact?")),
                    new Decorator(ret => getNode() != null && getNode().IsAlive && getNode().Location.Distance(StyxWoW.Me.Location) < CharacterSettings.Instance.PullDistance,
                        new Sequence(
                        new Action(ret => InteractNode())//,
                        //new Action(ret => FlyToRest())
                        )
                    ))                    
               );
        }


        public static void moveToHarvestNode()
        {
            WoWUnit wgo = getNode();
            if (StyxWoW.Me.Mounted ||
                StyxWoW.Me.Class == WoWClass.DeathKnight || (StyxWoW.Me.Class == WoWClass.Druid/* && (StyxWoW.Me.GetAuraById(768) != null || StyxWoW.Me.GetAuraById(5487) != null)*/) || 
                StyxWoW.Me.Class == WoWClass.Paladin || StyxWoW.Me.Class == WoWClass.Rogue || StyxWoW.Me.Class == WoWClass.Warrior)
            {
                WoWPoint savePoint = WoWPoint.Empty;
                if (StyxWoW.Me.Mounted)
                {
                    //savePoint = getSaveLocation(wgo.Location, 5, CharacterSettings.Instance.PullDistance, 40);
                    savePoint = new WoWPoint(wgo.X, wgo.Y, (wgo.Z + 5));
                }
                
                if (StyxWoW.Me.Mounted && savePoint != WoWPoint.Empty)
                {
                    while (savePoint.Distance(StyxWoW.Me.Location) > 3 && !StyxWoW.Me.IsSwimming)
                    {
                        StuckDetector();
                        TreeRoot.StatusText = "CritterKillSquad: move to " + wgo.Name + " | dis: " + (int)savePoint.Distance(StyxWoW.Me.Location) + "yds";
                        WoWMovement.ClickToMove(savePoint);
                        Thread.Sleep(100);
                    }
                }
                else
                {
                    while (wgo.Distance > 4 && !StyxWoW.Me.IsSwimming)
                    {
                        StuckDetector();
                        TreeRoot.StatusText = "CritterKillSquad: move to " + wgo.Name + " | dis: " + (int)wgo.Distance + "yds";
                        WoWMovement.ClickToMove(wgo.Location);
                        Thread.Sleep(100);
                    }
                }

                stuckSW.Stop(); stuckSW.Reset();
                WoWMovement.MoveStop();
                if (StyxWoW.Me.Mounted)
                    dismount();
            }
            else
            {
                while (!wgo.InLineOfSight && !StyxWoW.Me.IsSwimming && wgo.Distance >= CharacterSettings.Instance.PullDistance)
                {
                    TreeRoot.StatusText = "CritterKillSquad: move to " + wgo.Name + " | dis: " + (int)wgo.Distance;
                    StuckDetector();
                    WoWMovement.ClickToMove(wgo.Location);
                    Thread.Sleep(100);
                }
                WoWMovement.MoveStop();
            }
        }

        public static bool isNode()
        {
            if (getNode() == null)
            {
                return false;
            }
            return true;
        }

        public static double XYDist(WoWUnit wgo)
        {
            return Math.Sqrt(Math.Pow(wgo.Location.X - StyxWoW.Me.Location.X, 2) + Math.Pow(wgo.Location.Y - StyxWoW.Me.Location.Y, 2));
        }

        public static bool nodeIsBlacklisted(WoWPoint wp)
        {
            List<Stopwatch> sw = blackList.Keys.ToList();
            for (int i = blackList.Count - 1; i >= 0; i--)
            {
                if (sw[i].Elapsed.TotalMinutes >= blackListTime)
                {
                    blackList.Remove(sw[i]);
                }
            }
            foreach (WoWPoint wowp in blackList.Values)
            {
                if (wp.Distance(wowp) < 50)
                {
                    return true;
                }
            }
            return false;
        }
        public static WoWUnit getNode()
        {
            ObjectManager.Update();
            List<WoWUnit> nodes = ObjectManager.GetObjectsOfType<WoWUnit>();
            double xyDist = double.MaxValue;
            WoWUnit bestNode = null;
            foreach (WoWUnit wgo in nodes)
            {
                if ((wgo.Entry == 28440 || wgo.Entry == 2914 || wgo.Entry == 3300 || wgo.Entry == 4953) && !wgo.IsAlive && KillList.Contains(wgo.Guid) && wgo.KilledByMe)
                {
                        ++KillListCount;
                        KillList.Remove(wgo.Guid);
                }

                if ((wgo.Entry == 28440 || wgo.Entry == 2914 || wgo.Entry == 3300 || wgo.Entry == 4953) && wgo.IsAlive)
                {
                    if (!KillList.Contains(wgo.Guid))
                        KillList.Add(wgo.Guid);

                    double dist = XYDist(wgo);
                    if (xyDist > dist)
                    {
                        if (dist < DistToHarvest)
                        {
                            xyDist = dist;
                            bestNode = wgo;
                        }
                    }
                }
            }
            if (bestNode != null)
                TreeRoot.StatusText = "CritterKillSquad: found " + bestNode.Name + " | dis: " + (int)bestNode.Distance + "yds";
            return bestNode;
        }

        public static WoWUnit getNode(WoWUnit unit, int maxAOEradius, int minNodes)
        {
            TreeRoot.StatusText = "CritterKillSquad: search for best AOE " + unit.Name + " ...";
            ObjectManager.Update();
            List<WoWUnit> nodes = ObjectManager.GetObjectsOfType<WoWUnit>();
            WoWUnit bestAOEnode = null;
            AOEcount = 0;
            int crBestCount = 0;
            foreach (WoWUnit cr in nodes)
            {
                if ((cr.Entry == 28440 || cr.Entry == 2914 || cr.Entry == 3300 || cr.Entry == 4953) && cr.Distance <= 30 && cr.Location.Distance2D(unit.Location) <= maxAOEradius && cr.IsAlive /*&& !cr.IsMoving*/)
                {
                    int crCount = 0;
                    foreach (WoWUnit cr2 in nodes)
                    {
                        if (cr != cr2 && (cr2.Entry == 28440 || cr2.Entry == 2914 || cr2.Entry == 3300 || cr2.Entry == 4953) && cr2.IsAlive && cr.Location.Distance2D(cr2.Location) <= maxAOEradius)
                            crCount++;
                    }
                    if (crBestCount <= crCount && crCount >= minNodes)
                    {
                        crBestCount = crCount;
                        bestAOEnode = cr;
                    }
                }
            }
            if (bestAOEnode != null)
            {
                AOEcount = crBestCount;
                TreeRoot.StatusText = "CritterKillSquad: found best AOE " + bestAOEnode.Name + "  (≈ " + crBestCount + ") | dis: " + (int)bestAOEnode.Distance + "yds";
            }
            else if (bestAOEnode == null)
                TreeRoot.StatusText = "CritterKillSquad: cancelling AOE";
            return bestAOEnode;
        }

        public static void dismount()
        {
            if (StyxWoW.Me.HoverHeight > 10)
            {
                return;
            }
            TreeRoot.StatusText = "CritterKillSquad: Dismount";
            Logging.WriteNavigator(System.Drawing.Color.Red, "{0} - Navigation: Dismount. Current Location {1}", TimeNow, StyxWoW.Me.Location);
            WoWMovement.Move(WoWMovement.MovementDirection.Descend);
            //Lua.DoString("Dismount()");
            Mount.Dismount();
            Thread.Sleep(1000);
        }

        public static void InteractNode()
        {
            stuckSW.Reset(); stuckSW.Stop();
            TreeRoot.StatusText = "CritterKillSquad: Interact with node ...";
            WoWUnit wgo = getNode();
            if (wgo == null)
            {
                return;
            }
            if (wgo.Distance >= CharacterSettings.Instance.PullDistance)
            {
                return;
            }

            if (Me.Class != WoWClass.Druid)
                FightRoutine.Routine(wgo);
            else
                if (wgo != null && wgo.IsAlive && !StyxWoW.Me.IsSwimming && wgo.Distance <= CharacterSettings.Instance.PullDistance)
                {
                    TreeRoot.StatusText = "CritterKillSquad: Interact with " + wgo.Name;
                    //wgo.Interact();
                    wgo.Target();
                    wgo.Face();
                    Thread.Sleep(300);
                    RoutineManager.Current.Pull();
                    Thread.Sleep(500);
                    RoutineManager.Current.Combat();
                    Thread.Sleep(800);
                }
        }

        public static void mount()
        {
            bool flyup = true;
            if (StyxWoW.Me.HoverHeight > 10)
            {
                return;
            }

            if (Me.Location.Distance(new WoWPoint(1946.125, 5005.813, -2.276326)) < 5 && Me.IsSwimming)
            {
                while(Me.Location.Distance(new WoWPoint(1942.231, 5018.066, -2.167384)) > 1 && (Me.IsSwimming || !Me.Mounted))
                    if (Me.Location.Distance(new WoWPoint(1942.231, 5018.066, -2.167384)) > 1)
                    {
                        WoWMovement.ClickToMove(new WoWPoint(1942.231, 5018.066, -2.167384));
                    }
                    else
                    {
                        mount();
                    }
            }

            if (Me.Class == WoWClass.Druid && Me.IsSwimming && mounts.Count == 1)
            #region dudu water stuck
            {
                string MountName = mounts.Values.ElementAt(0);
                int spellid = mounts.Keys.ElementAt(0);

                Logging.Write(System.Drawing.Color.Red, "We are in the water trying to mount up...");
                TreeRoot.StatusText = "CritterKillSquad: We are in the water trying to mount up...";


                KeyboardManager.PressKey((char)Keys.Space);
                Thread.Sleep(ran.Next(500, 1200));
                KeyboardManager.ReleaseKey((char)Keys.Space);

                for (int i = 1, imax = 15; i <= imax && !Me.Mounted; i++)
                {
                    Logging.Write(System.Drawing.Color.Red, "{2} - attempt {0} of {1} ...", i, imax, TimeNow);
                    KeyboardManager.KeyUpDown((char)Keys.Space);
                    Thread.Sleep(150);
                    WoWSpell.FromId(spellid).Cast();
                    Thread.Sleep(2000);
                }

                if (!Me.Mounted && Me.IsSwimming)
                {
                    System.Media.SystemSounds.Exclamation.Play();
                    Thread.Sleep(800);
                    System.Media.SystemSounds.Exclamation.Play();
                    Logging.Write(System.Drawing.Color.Red, "Sorry... we are stuck... logout in 20sec...");
                    TreeRoot.StatusText = "CritterKillSquad: Sorry... we are stuck... logout in 20sec...";
                    Lua.DoString("/logout");
                    Thread.Sleep(500);
                    TreeRoot.Stop();
                }


                KeyboardManager.PressKey((char)Keys.Space);
                Thread.Sleep(ran.Next(1500, 3000));
                KeyboardManager.ReleaseKey((char)Keys.Space);
                flyup = false;
            }
            #endregion
            else if (Me.IsSwimming)
            {
                KeyboardManager.PressKey((char)Keys.Space);
                Thread.Sleep(ran.Next(500, 1200));
                KeyboardManager.ReleaseKey((char)Keys.Space);
                Thread.Sleep(1500);
            }

            WoWMovement.MoveStop();
            int tries = 0;
            while (flyup && (!StyxWoW.Me.Mounted || (StyxWoW.Me.Mounted && !StyxWoW.Me.IsFlying)))
            {
                StuckDetector();
                if (StyxWoW.Me.Combat || StyxWoW.Me.Dead)
                {
                    return;
                }
                if (tries > 4)
                {
                    Logging.Write(System.Drawing.Color.Red, "Failing to mount!");
                    dlog("Failing to mount!");
                }
                if (!StyxWoW.Me.Mounted)
                {
                    string MountName = string.Empty;
                    int spellid = 0;
                    if (mounts.Count == 1)
                    {
                        MountName = mounts.Values.ElementAt(0);
                        spellid = mounts.Keys.ElementAt(0);
                    }
                    else if (mounts.Count > 1)
                    {
                        int rnd = ran.Next(1, mounts.Count) - 1;
                        MountName = mounts.Values.ElementAt(rnd);
                        spellid = mounts.Keys.ElementAt(rnd);
                    }
                    TreeRoot.StatusText = "CritterKillSquad: Mounting " + MountName;
                    //Lua.DoString("CastSpellByName(\"" + MountName + "\")");
                    WoWSpell.FromId(spellid).Cast();
                    Thread.Sleep(3000);
                    tries++;
                    flyup = false;
                }
                if (StyxWoW.Me.Mounted)
                {
                    KeyboardManager.PressKey((char)Keys.Space);
                    Thread.Sleep(ran.Next(400, 700));
                    KeyboardManager.ReleaseKey((char)Keys.Space);
                    flyup = false;
                    StopStuckDetector();
                }
            }
            WoWMovement.MoveStop();
            StopStuckDetector();
            
        }
        #endregion

        #region CreateMoveBehaviour

        public static Composite CreateMoveBehaviour()
        {
            return new Action(ret => moveToNode());
        }
        #endregion

        public static void moveToNode()
        {
            TreeRoot.StatusText = "CritterKillSquad: move to next node..";
            BotCKS.slog("{0} kills now! move to next node..",KillListCount);
            StuckDetector();
            WoWMovement.ClickToMove(profile[GetNextNode()]);
            Thread.Sleep(100);
            return;
        }

        static int lastNode = profile.Count + 10;
        public static int GetNextNode()
        {
            if (lastNode == profile.Count + 10)
            {
                lastNode = GetClosestNode()-1;
            }
            if (lastNode + 1 >= profile.Count)
            {
                lastNode = -1;
            }
            if (profile[lastNode+1].Distance(StyxWoW.Me.Location) < 10)
            {
                lastNode = lastNode + 1;
            }
            if (profile[lastNode + 1].Distance(StyxWoW.Me.Location) > 200)
            {
                lastNode = GetClosestNode() - 1;
            }
            return lastNode + 1;

            //if (profile[closestNode].Distance(StyxWoW.Me.Location) < 10)
            //{
            //    lastNode = closestNode;
            //}
            //return lastNode + 1;
        }

        public static int GetClosestNode()
        {
            double d = double.MaxValue;
            int node = 0;
            for (int i = 0; i < profile.Count; i++)
            {
                if (profile[i].Distance(StyxWoW.Me.Location) < d)
                {
                    d = profile[i].Distance(StyxWoW.Me.Location);
                    node = i;
                }
            }
            return node;
        }

        public static void Target()
        {
            //List<WoWUnit> wus = ObjectManager.GetObjectsOfType<WoWUnit>();
            //foreach (WoWUnit wu in wus)
            //{
            //    if (wu.IsTargetingMeOrPet && wu.Attackable)
            //    {
            //        wu.Target();
            //        return;
            //    }
            //}
        }



        private static Composite CreateCombatBehaviour()
        {
            
            return new Decorator(ret => (StyxWoW.Me.Combat && !StyxWoW.Me.Mounted),
                             new PrioritySelector(
                                 new Decorator(ret => StyxWoW.Me.CurrentTarget == null,
                                        new Action(ret => Target())
                                     ),
                                 new Decorator(ret => (!StyxWoW.Me.IsFacing(StyxWoW.Me.CurrentTarget.Location)),
                                     new Action(ret => WoWMovement.ClickToMove(StyxWoW.Me.CurrentTarget.Location))
                                     ),

            #region Heal

new PrioritySelector(
                // Use the Behavior
                                     new Decorator(ctx => RoutineManager.Current.HealBehavior != null,
                                         new Sequence(
                                             RoutineManager.Current.HealBehavior,
                                             new Action(delegate { return RunStatus.Success; })
                                             )),

                                     // Don't use the Behavior
                                     new Decorator(ctx => RoutineManager.Current.NeedHeal,
                                         new Sequence(
                                             new Action(ret => TreeRoot.StatusText = "Healing"),
                                             new Action(ret => RoutineManager.Current.Heal())
                                             ))),

            #endregion

            #region Combat Buffs

 new PrioritySelector(
                // Use the Behavior
                                     new Decorator(ctx => RoutineManager.Current.CombatBuffBehavior != null,
                                                 new Sequence(
                                                     RoutineManager.Current.CombatBuffBehavior,
                                                     new Action(delegate { return RunStatus.Success; })
                                                     )
                                         ),

                                     // Don't use the Behavior
                                     new Decorator(ctx => RoutineManager.Current.NeedCombatBuffs,
                                                 new Sequence(
                                                     new Action(ret => TreeRoot.StatusText = "Applying Combat Buffs"),
                                                     new Action(ret => RoutineManager.Current.CombatBuff())
                                                     ))),

            #endregion

            #region Combat

 new PrioritySelector(
                // Use the Behavior
                                     new Decorator(ctx => RoutineManager.Current.CombatBehavior != null,
                                         new PrioritySelector(
                                             RoutineManager.Current.CombatBehavior,
                                             new Action(delegate { return RunStatus.Success; })
                                             )),

                                     // Don't use the Behavior
                                     new Sequence(
                                         new Action(ret => TreeRoot.StatusText = "Combat"),
                                         new Action(ret => RoutineManager.Current.Combat())))

            #endregion


));
        }

        public static void MoveToBody()
        {
            while (StyxWoW.Me.Location.Distance(StyxWoW.Me.CorpsePoint) > 5)
            {
                StuckDetector();
                Navigator.MoveTo(StyxWoW.Me.CorpsePoint);
                Thread.Sleep(100);
            }
        }

        public static void Retrieve()
        {
            Lua.DoString("RetrieveCorpse()");
        }

        public static void Release()
        {
            Lua.DoString("RepopMe()");
        }

        public static Composite CreateCorpseBehaviour()
        {
            return new Decorator(ret => !StyxWoW.Me.IsAlive,
                new Sequence(
                    new Action (ret=>Release()),
                    new Action(ret => MoveToBody()),
                    new Action(ret => Retrieve())
                    )
                    );
        }

        static public WoWPoint getSaveLocation(WoWPoint Location, int minDist, int maxDist, int traceStep)
        {
            Logging.WriteNavigator("{0} - Navigation: Looking for save Location around {1}.", TimeNow, Location);

            float _PIx2 = 3.14159f * 2f;

            for (int i = 0, x = minDist; i < traceStep && x < maxDist; i++)
            {
                WoWPoint p = Location.RayCast((i * _PIx2) / traceStep, x);

                p.Z = getGroundZ(p);
                WoWPoint pLoS = p;
                pLoS.Z = p.Z + 0.5f;

                if (p.Z != float.MinValue && StyxWoW.Me.Location.Distance(p) > 1)
                {
                    if (getHighestSurroundingSlope(p) < 1.2f && GameWorld.IsInLineOfSight(pLoS, Location) /*&& Navigator.CanNavigateFully(StyxWoW.Me.Location, Location)*/)
                    {
                        Logging.WriteNavigator("{0} - Navigation: Moving to {1}. Distance: {2}", TimeNow, p, Location.Distance(p));
                        return p;
                    }
                }

                if (i == (traceStep - 1))
                {
                    i = 0;
                    x++;
                }
            }
            Logging.Write(System.Drawing.Color.Red, "{0} - No valid points returned by RayCast...", TimeNow);
            return WoWPoint.Empty;
        }

        /// <summary>
        /// Credits to exemplar.
        /// </summary>
        /// <returns>Z-Coordinates for PoolPoints so we don't jump into the water.</returns>
        public static float getGroundZ(WoWPoint p) // by iggi66
        {
            WoWPoint ground = WoWPoint.Empty;

            GameWorld.TraceLine(new WoWPoint(p.X, p.Y, (p.Z + 4)), new WoWPoint(p.X, p.Y, (p.Z - 0.8f)), GameWorld.CGWorldFrameHitFlags.HitTestGroundAndStructures/* | GameWorld.CGWorldFrameHitFlags.HitTestBoundingModels | GameWorld.CGWorldFrameHitFlags.HitTestWMO*/, out ground);

            if (ground != WoWPoint.Empty)
            {
                Logging.WriteDebug("{0} - Ground Z: {1}.", TimeNow, ground.Z);
                return ground.Z;
            }
            Logging.WriteDebug("{0} - Ground Z returned float.MinValue.", TimeNow);
            return float.MinValue;
        }

        /// <summary>
        /// Credits to funkescott.
        /// </summary>
        /// <returns>Highest slope of surrounding terrain, returns 100 if the slope can't be determined</returns>
        public static float getHighestSurroundingSlope(WoWPoint p)
        {
            Logging.WriteNavigator("{0} - Navigation: Sloapcheck on Point: {1}", TimeNow, p);
            float _PIx2 = 3.14159f * 2f;
            float highestSlope = -100;
            float slope = 0;
            int traceStep = 15;
            float range = 0.5f;
            WoWPoint p2;
            for (int i = 0; i < traceStep; i++)
            {
                p2 = p.RayCast((i * _PIx2) / traceStep, range);
                p2.Z = getGroundZ(p2);
                slope = Math.Abs(getSlope(p, p2));
                if (slope > highestSlope)
                {
                    highestSlope = (float)slope;
                }
            }
            Logging.WriteNavigator("{0} - Navigation: Highslope {1}", TimeNow, highestSlope);
            return Math.Abs(highestSlope);
        }

        /// <summary>
        /// Credits to funkescott.
        /// </summary>
        /// <param name="p1">from WoWPoint</param>
        /// <param name="p2">to WoWPoint</param>
        /// <returns>Return slope from WoWPoint to WoWPoint.</returns>
        public static float getSlope(WoWPoint p1, WoWPoint p2)
        {
            float rise = p2.Z - p1.Z;
            float run = (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

            return rise / run;
        }

        #region stuck helper
        public static void StopStuckDetector()
        {
            stuckSW.Stop(); stuckSW.Reset();
        }
        static Stopwatch stuckSW = new Stopwatch();
        static WoWPoint stuckWP;
        public static void StuckDetector()
        {
            if (!StyxWoW.Me.Dead)
            {
                if (!stuckSW.IsRunning)
                {
                    stuckSW.Start();
                    stuckWP = StyxWoW.Me.Location;
                }
                else if (stuckSW.Elapsed.TotalSeconds > 5 || stuckWP.Distance(StyxWoW.Me.Location) > 5)
                {
                    stuckSW.Reset();
                    stuckSW.Start();
                    stuckWP = StyxWoW.Me.Location;
                }
                else if (stuckWP.Distance(StyxWoW.Me.Location) < 5 && ((stuckSW.Elapsed.Seconds > 2 && !StyxWoW.Me.IsSwimming) || stuckSW.Elapsed.Seconds > 10))
                {//is stuck
                    StuckBehaviour();
                    stuckSW.Reset();
                    stuckSW.Start();
                    stuckWP = StyxWoW.Me.Location;
                }
            }
        }
        static bool lastRight = true;
        public static void StuckBehaviour()
        {
            //if (StyxWoW.Me.IsSwimming)
            //{
            //    KeyboardManager.PressKey((char)KeyboardManager.eVirtualKeyMessages.VK_SPACE);
            //    Thread.Sleep(1000);
            //    KeyboardManager.ReleaseKey((char)KeyboardManager.eVirtualKeyMessages.VK_SPACE);
            //    Thread.Sleep(1000);
            //}
            //System.Media.SystemSounds.Exclamation.Play();
            Logging.Write("I am stuck! Trying to unstuck!");
            if (Me.Mounted)
            {
                WoWMovement.MoveStop();
                Thread.Sleep(200);
                KeyboardManager.PressKey((char)KeyboardManager.eVirtualKeyMessages.VK_DOWN);
                Thread.Sleep(1000);
                KeyboardManager.ReleaseKey((char)KeyboardManager.eVirtualKeyMessages.VK_DOWN);
                Thread.Sleep(200);
                KeyboardManager.PressKey((char)KeyboardManager.eVirtualKeyMessages.VK_SPACE);
                Thread.Sleep(1000);
                KeyboardManager.ReleaseKey((char)KeyboardManager.eVirtualKeyMessages.VK_SPACE);
                Thread.Sleep(200);
                char direction;
                if (lastRight)
                {
                    direction = (char)KeyboardManager.eVirtualKeyMessages.VK_LEFT;
                    lastRight = false;
                }
                else
                {
                    direction = (char)KeyboardManager.eVirtualKeyMessages.VK_RIGHT;
                    lastRight = true;
                }
                KeyboardManager.PressKey(direction);
                Thread.Sleep(500);
                KeyboardManager.ReleaseKey(direction);
                Thread.Sleep(200);
                KeyboardManager.PressKey((char)KeyboardManager.eVirtualKeyMessages.VK_UP);
                Thread.Sleep(1000);
                KeyboardManager.ReleaseKey((char)KeyboardManager.eVirtualKeyMessages.VK_DOWN);
                Thread.Sleep(200);
            }
            else
            {
                mount();
            }
            StopStuckDetector();
            moveToHarvestNode();
            WoWMovement.MoveStop();
        }
        #endregion
    
    
    }
    public class FightRoutine
    {
        public static void Routine(WoWUnit _unit)
        {
            while (_unit != null && _unit.IsAlive && !StyxWoW.Me.IsSwimming && _unit.Distance <= CharacterSettings.Instance.PullDistance && !StyxWoW.Me.Mounted)
            {
                WoWSpell AutoAtack = WoWSpell.FromId(6603);

                #region PALDIN
                if (StyxWoW.Me.Class == WoWClass.Paladin)
                {
                    TreeRoot.StatusText = "CritterKillSquad: slay node ...";
                    WoWUnit AOEunit = BotCKS.getNode(_unit, 9, 2);
                    WoWSpell Weihe = WoWSpell.FromId(26573);
                    WoWSpell HammerDR = WoWSpell.FromId(53595);
                    WoWSpell HZorn = WoWSpell.FromId(2812);
                    WoWSpell KStoß = WoWSpell.FromId(35395);
                    WoWSpell GSturm = WoWSpell.FromId(53385);
                    WoWSpell SiegelDW = WoWSpell.FromId(31801);
                    WoWSpell Richturteil = WoWSpell.FromId(20271);


                    // need buff?
                    if (SpellManager.HasSpell(SiegelDW.Id) && !StyxWoW.Me.HasAura(SiegelDW.Name) && !Styx.StyxWoW.GlobalCooldown)
                    {
                        TreeRoot.StatusText = "CritterKillSquad: need buff " + SiegelDW.Name + " ...";
                        BotCKS.slog("need buff " + SiegelDW.Name);
                        SiegelDW.Cast();
                        Thread.Sleep(BotCKS.ran.Next(400, 800));
                    }

                    if (AOEunit != null)
                    {
                        if (AOEunit.Distance >= 3)
                        {
                            MoveToNode(AOEunit, 3);
                        }

                        TreeRoot.StatusText = "CritterKillSquad: search for best " + Weihe.Name + " point ...";
                        if (SpellManager.HasSpell(GSturm) && !GSturm.Cooldown && !Styx.StyxWoW.GlobalCooldown && StyxWoW.Me.HolyPowerPercent > 0)
                        {
                            TreeRoot.StatusText = "CritterKillSquad: cast " + GSturm.Name + " on ≈ " + BotCKS.AOEcount + " " + AOEunit.Name;
                            BotCKS.slog("Cast " + GSturm.Name + " on ≈ " + BotCKS.AOEcount + " " + AOEunit.Name);
                            GSturm.Cast();

                        }
                        else if (SpellManager.HasSpell(HammerDR) && !HammerDR.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                        {
                            if (AOEunit.Distance >= 3)
                                MoveToNode(AOEunit, 3);

                            if (!StyxWoW.Me.GotTarget || !StyxWoW.Me.CurrentTarget.IsAlive || StyxWoW.Me.CurrentTarget != AOEunit)
                            {
                                TreeRoot.StatusText = "CritterKillSquad: face " + AOEunit.Name + " ...";
                                AOEunit.Target();
                                AOEunit.Face();
                                Thread.Sleep(BotCKS.ran.Next(200, 500));
                            }
                            TreeRoot.StatusText = "CritterKillSquad: Cast " + HammerDR.Name + " on ≈ " + BotCKS.AOEcount + " " + AOEunit.Name;
                            BotCKS.slog("Cast " + HammerDR.Name + " on ≈ " + BotCKS.AOEcount + " " + AOEunit.Name);
                            HammerDR.Cast();
                            StyxWoW.Me.ClearTarget();
                        }
                        else if (SpellManager.HasSpell(Weihe) && !Weihe.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                        {
                            TreeRoot.StatusText = "CritterKillSquad: cast " + Weihe.Name + " on ≈ " + BotCKS.AOEcount + " " + AOEunit.Name;
                            BotCKS.slog("Cast " + Weihe.Name + " on ≈ " + BotCKS.AOEcount + " " + AOEunit.Name);
                            Weihe.Cast();
                        }
                        else if (SpellManager.HasSpell(HZorn) && !HZorn.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                        {
                            TreeRoot.StatusText = "CritterKillSquad: Cast " + HZorn.Name + " on ≈ " + BotCKS.AOEcount + " " + AOEunit.Name;
                            BotCKS.slog("Cast " + HZorn.Name + " on ≈ " + BotCKS.AOEcount + " " + AOEunit.Name);
                            HZorn.Cast();
                        }
                        else
                        {
                            TreeRoot.StatusText = "CritterKillSquad: [WAIT] " + Weihe.Name + " & " + HammerDR.Name + " & " + HZorn.Name + " on cooldown ...";
                            Thread.Sleep(300);
                            //continue;
                        }
                        Thread.Sleep(BotCKS.ran.Next(200, 700));
                    }
                    else if (SpellManager.HasSpell(KStoß.Id) && !Styx.StyxWoW.GlobalCooldown && !KStoß.Cooldown)
                    {
                        if (_unit.Distance >= 3)
                            MoveToNode(_unit, 3);

                        TreeRoot.StatusText = "CritterKillSquad: Cast " + KStoß.Name + " ...";
                        BotCKS.slog("Cast " + KStoß.Name);

                        if (!StyxWoW.Me.GotTarget || !StyxWoW.Me.CurrentTarget.IsAlive || StyxWoW.Me.CurrentTarget != _unit)
                        {
                            TreeRoot.StatusText = "CritterKillSquad: face " + _unit.Name + " ...";
                            _unit.Target();
                            _unit.Face();
                            Thread.Sleep(BotCKS.ran.Next(200, 500));
                        }

                        KStoß.Cast();
                        Thread.Sleep(BotCKS.ran.Next(950, 1200));
                        StyxWoW.Me.ClearTarget();
                    }
                    else if (SpellManager.HasSpell(Richturteil.Id) && !Styx.StyxWoW.GlobalCooldown && !Richturteil.Cooldown && StyxWoW.Me.HasAura(SiegelDW.Name))
                    {
                        TreeRoot.StatusText = "CritterKillSquad: Cast " + Richturteil.Name + " ...";
                        BotCKS.slog("Cast " + Richturteil.Name);
                        _unit.Target();
                        Richturteil.Cast();
                        Thread.Sleep(BotCKS.ran.Next(950, 1200));
                    }
                    else if(_unit.IsAlive)
                    {
                        if (_unit.Distance >= 3)
                        {
                            BotCKS.StuckDetector();
                            MoveToNode(_unit, 3);
                        }

                        TreeRoot.StatusText = "CritterKillSquad: no Cast ready! Interact with " + _unit.Name;
                        BotCKS.slog("no Cast ready! Interact with " + _unit.Name);
                        _unit.Target(); _unit.Interact();
                        Thread.Sleep(BotCKS.ran.Next(200, 500));
                    }

                }
                #endregion

                else

                    #region SHAMAN
                    if (StyxWoW.Me.Class == WoWClass.Shaman)
                    {
                        TreeRoot.StatusText = "CritterKillSquad: slay node ...";
                        bool canErdbeben = true;
                        WoWUnit AOEunit = BotCKS.getNode(_unit, 11, 3); //AOEunit = null;

                        WoWSpell erdbeben = WoWSpell.FromId(61882);
                        WoWSpell gewitter = WoWSpell.FromId(51490);
                        WoWSpell blitzschlag = WoWSpell.FromId(403);

                        if (AOEunit != null && AOEunit.IsAlive && !Styx.StyxWoW.GlobalCooldown && ((SpellManager.HasSpell(gewitter) && !gewitter.Cooldown) || (SpellManager.HasSpell(erdbeben) && !erdbeben.Cooldown)))
                        {
                            if (SpellManager.HasSpell(erdbeben) && !erdbeben.Cooldown && AOEunit != null && canErdbeben && StyxWoW.Me.ManaPercent > 35)
                            {
                                if (AOEunit != null && (AOEunit.Distance >= CharacterSettings.Instance.PullDistance || (AOEunit.Distance <= CharacterSettings.Instance.PullDistance && !_unit.InLineOfSight)))
                                {
                                    MoveToNode(CharacterSettings.Instance.PullDistance, AOEunit);
                                }

                                erdbeben.Cast();
                                Thread.Sleep(BotCKS.ran.Next(200, 500));
                                if (!LegacySpellManager.ClickRemoteLocation(AOEunit.Location))
                                {
                                    string txt = String.Format("{0} FAILED: cancelling {0}!!", erdbeben.Name);
                                    BotCKS.slog(txt);
                                    TreeRoot.StatusText = "CritterKillSquad: " + txt;

                                    if (StyxWoW.Me.IsCasting)
                                        SpellManager.StopCasting();

                                    canErdbeben = false;
                                    continue;
                                }
                                else
                                {
                                    string txt = String.Format("{0} successful: click {0} on ≈ {2} {1}", erdbeben.Name, BotCKS.AOEcount, AOEunit.Name);
                                    BotCKS.slog(txt);
                                    TreeRoot.StatusText = "CritterKillSquad: " + txt;
                                    Thread.Sleep(BotCKS.ran.Next(2280, 2600));
                                }

                            }
                            else if (SpellManager.HasSpell(gewitter) && !gewitter.Cooldown && AOEunit != null && !Styx.StyxWoW.GlobalCooldown)
                            {
                                if (AOEunit.Distance >= 3)
                                {
                                    MoveToNode(AOEunit, 3);
                                }

                                TreeRoot.StatusText = "CritterKillSquad: cast " + gewitter.Name + " ...";
                                gewitter.Cast();
                            }
                            Thread.Sleep(BotCKS.ran.Next(200, 700));
                        }
                        else
                        {
                            WoWMovement.MoveStop();
                            while (_unit != null && _unit.IsAlive && !StyxWoW.Me.IsSwimming && _unit.InLineOfSight && _unit.Distance <= CharacterSettings.Instance.PullDistance)
                            {
                                if (!StyxWoW.Me.GotTarget || !StyxWoW.Me.CurrentTarget.IsAlive || StyxWoW.Me.CurrentTarget != _unit)
                                {
                                    TreeRoot.StatusText = "CritterKillSquad: face " + _unit.Name + " ...";
                                    _unit.Target();
                                    _unit.Face();
                                    Thread.Sleep(200);
                                }
                                TreeRoot.StatusText = "CritterKillSquad: attacking " + _unit.Name;
                                if (!Styx.StyxWoW.GlobalCooldown && !StyxWoW.Me.IsCasting)
                                    blitzschlag.Cast(); //RoutineManager.Current.Combat(); _unit.Interact();
                                Thread.Sleep(BotCKS.ran.Next(100, 300));
                            }
                        }

                    }
                    #endregion
                    else
                #region Priest
                if (StyxWoW.Me.Class == WoWClass.Priest)
                {
                    TreeRoot.StatusText = "CritterKillSquad: slay node ...";
                    WoWUnit AOEunit = BotCKS.getNode(_unit, 11, 2); //AOEunit = null;

                    WoWSpell HNova = WoWSpell.FromId(15237);

                    if (AOEunit != null && SpellManager.HasSpell(HNova.Id) && StyxWoW.Me.ManaPercent >= 15 && !HNova.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                    {
                        if (AOEunit.Distance >= 3)
                            MoveToNode(AOEunit, 3);

                        TreeRoot.StatusText = "CritterKillSquad: cast " + HNova.Name + " on ≈ " + BotCKS.AOEcount;
                        BotCKS.slog("cast " + HNova.Name + " on ≈ " + BotCKS.AOEcount);
                        HNova.Cast();
                        Thread.Sleep(BotCKS.ran.Next(800, 1500));

                    }
                    else if (_unit.IsAlive)
                    {
                        if (_unit.Distance >= 3)
                        {
                            BotCKS.StuckDetector();
                            MoveToNode(_unit, 3);
                        }

                        TreeRoot.StatusText = "CritterKillSquad: " + HNova.Name + " is not ready! Interact with " + _unit.Name;
                        BotCKS.slog(HNova.Name + " is not ready! Interact with " + _unit.Name);
                        _unit.Target(); _unit.Interact(); 
                        
                        if(!StyxWoW.Me.IsAutoAttacking)
                        AutoAtack.Cast();

                        Thread.Sleep(BotCKS.ran.Next(200, 500));
                    }

                }
                #endregion
                else

                    #region Hunter
                    if (StyxWoW.Me.Class == WoWClass.Hunter)
                    {
                        TreeRoot.StatusText = "CritterKillSquad: slay node ...";
                        WoWUnit AOEunit = BotCKS.getNode(_unit, 11, 2);
                        WoWPoint m = WoWPoint.Empty;                 

                        WoWSpell MultiShot = WoWSpell.FromId(2643);
                        WoWSpell AutoShot = WoWSpell.FromId(75);       

                        if (AOEunit != null && SpellManager.HasSpell(MultiShot.Id) && !MultiShot.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                        {
                            while (AOEunit.Distance <= 10)
                            {
                                if (m == WoWPoint.Empty)
                                    m = BotCKS.getSaveLocation(AOEunit.Location, 11, 20, 60);

                                WoWMovement.ClickToMove(m);
                            }

                            TreeRoot.StatusText = "CritterKillSquad: cast " + MultiShot.Name + " on ≈ " + BotCKS.AOEcount;
                            BotCKS.slog("cast " + MultiShot.Name + " on ≈ " + BotCKS.AOEcount);

                            AOEunit.Target();
                            AOEunit.Face();
                            MultiShot.Cast();
                            Thread.Sleep(BotCKS.ran.Next(200, 500));
                            m = WoWPoint.Empty;
                        }
                        else if (_unit.IsAlive)
                        {
                            while (_unit.Distance <= 10)
                            {
                                if (m == WoWPoint.Empty)
                                    m = BotCKS.getSaveLocation(_unit.Location, 11, 20, 60);

                                WoWMovement.ClickToMove(m);
                            }

                            TreeRoot.StatusText = "CritterKillSquad: cast " + AutoShot.Name;
                            BotCKS.slog("cast " + AutoShot.Name);

                            _unit.Target(); _unit.Face();
                            AutoShot.Cast();
                            Thread.Sleep(BotCKS.ran.Next(800, 1500));
                        }

                    }
                    #endregion
                    else

                        #region Warlock
                        if (StyxWoW.Me.Class == WoWClass.Warlock)
                        {
                            TreeRoot.StatusText = "CritterKillSquad: slay node ...";
                            WoWUnit AOEunit = BotCKS.getNode(_unit, 12, 2);
                            bool canRainofFire = true;

                            WoWSpell Metamorphosis = WoWSpell.FromId(47241);
                            WoWSpell HandofGuldan = WoWSpell.FromId(71521);
                            WoWSpell Felstorm = WoWSpell.FromId(89751);
                            WoWSpell RainofFire = WoWSpell.FromId(5740);
                            WoWSpell ShadowBolt = WoWSpell.FromId(686);

                            if (SpellManager.HasSpell(Metamorphosis.Id) && !Metamorphosis.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                            {
                                TreeRoot.StatusText = "CritterKillSquad: cast " + Metamorphosis.Name;
                                BotCKS.slog("cast " + Metamorphosis.Name);
                                Metamorphosis.Cast();
                                Thread.Sleep(BotCKS.ran.Next(200, 500));
                            }

                            if (AOEunit != null && SpellManager.HasSpell(HandofGuldan.Id) && !HandofGuldan.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                            {
                                TreeRoot.StatusText = "CritterKillSquad: cast " + HandofGuldan.Name + " on ≈ " + BotCKS.AOEcount;
                                BotCKS.slog("cast " + HandofGuldan.Name + " on ≈ " + BotCKS.AOEcount);
                                AOEunit.Target(); AOEunit.Face();
                                HandofGuldan.Cast();
                                Thread.Sleep(BotCKS.ran.Next(2100, 3000));
                            }
                            else if (AOEunit != null && SpellManager.HasSpell(Felstorm.Id) && !Felstorm.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                            {
                                if (AOEunit.Distance >= 3)
                                    MoveToNode(AOEunit, 3);

                                TreeRoot.StatusText = "CritterKillSquad: cast " + Felstorm.Name + " on ≈ " + BotCKS.AOEcount;
                                BotCKS.slog("cast " + Felstorm.Name + " on ≈ " + BotCKS.AOEcount);
                                Felstorm.Cast();
                                Thread.Sleep(BotCKS.ran.Next(200, 500));
                            }
                            else if (AOEunit != null && SpellManager.HasSpell(RainofFire.Id) && !RainofFire.Cooldown && canRainofFire && !Styx.StyxWoW.GlobalCooldown)
                            {
                                if (AOEunit != null && (AOEunit.Distance >= CharacterSettings.Instance.PullDistance || (AOEunit.Distance <= CharacterSettings.Instance.PullDistance && !_unit.InLineOfSight)))
                                {
                                    MoveToNode(CharacterSettings.Instance.PullDistance, AOEunit);
                                }

                                RainofFire.Cast();
                                Thread.Sleep(BotCKS.ran.Next(200, 500));
                                if (!LegacySpellManager.ClickRemoteLocation(AOEunit.Location))
                                {
                                    string txt = String.Format("{0} FAILED: cancelling {0}!!", RainofFire.Name);
                                    BotCKS.slog(txt);
                                    TreeRoot.StatusText = "CritterKillSquad: " + txt;

                                    if (StyxWoW.Me.IsCasting)
                                        SpellManager.StopCasting();

                                    canRainofFire = false;
                                    continue;
                                }
                                else
                                {
                                    string txt = String.Format("{0} successful: click {0} on ≈ {2} {1}", RainofFire.Name, BotCKS.AOEcount, AOEunit.Name);
                                    BotCKS.slog(txt);
                                    TreeRoot.StatusText = "CritterKillSquad: " + txt;
                                    Thread.Sleep(BotCKS.ran.Next(2280, 3100));
                                }
                            }
                            else if (_unit.IsAlive && SpellManager.HasSpell(ShadowBolt.Id) && !ShadowBolt.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                            {
                                TreeRoot.StatusText = "CritterKillSquad: cast " + ShadowBolt.Name;
                                BotCKS.slog("cast " + ShadowBolt.Name);

                                _unit.Target(); _unit.Face();
                                ShadowBolt.Cast();
                                Thread.Sleep(BotCKS.ran.Next(200, 500));
                            }

                        }
                        #endregion

                        else

                            #region DeathKnight
                            if (StyxWoW.Me.Class == WoWClass.DeathKnight)
                            {
                                TreeRoot.StatusText = "CritterKillSquad: slay node ...";
                                bool canDeathAndDecay = true;
                                WoWUnit AOEunit = BotCKS.getNode(_unit, 11, 2);
                                WoWUnit AOEunit2 = BotCKS.getNode(_unit, 30, 2);

                                WoWSpell BloodBoil = WoWSpell.FromId(48721);
                                WoWSpell DeathAndDecay = WoWSpell.FromId(39658);

                                if (AOEunit2 != null && SpellManager.HasSpell(DeathAndDecay.Id) && !DeathAndDecay.Cooldown && !Styx.StyxWoW.GlobalCooldown && canDeathAndDecay)
                                {

                                    while (!AOEunit2.InLineOfSight)
                                    {
                                        WoWMovement.ClickToMove(AOEunit2.Location);
                                    }

                                    TreeRoot.StatusText = "CritterKillSquad: cast " + DeathAndDecay.Name + " on ≈ " + BotCKS.AOEcount;
                                    BotCKS.slog("cast " + DeathAndDecay.Name + " on ≈ " + BotCKS.AOEcount);

                                    DeathAndDecay.Cast();
                                    Thread.Sleep(BotCKS.ran.Next(200, 500));
                                    if (!LegacySpellManager.ClickRemoteLocation(AOEunit.Location))
                                    {
                                        string txt = String.Format("{0} FAILED: cancelling {0}!!", DeathAndDecay.Name);
                                        BotCKS.slog(txt);
                                        TreeRoot.StatusText = "CritterKillSquad: " + txt;

                                        if (StyxWoW.Me.IsCasting)
                                            SpellManager.StopCasting();

                                        canDeathAndDecay = false;
                                        continue;
                                    }
                                    else
                                    {
                                        string txt = String.Format("{0} successful: click {0} on ≈ {2} {1}", DeathAndDecay.Name, BotCKS.AOEcount, AOEunit.Name);
                                        BotCKS.slog(txt);
                                        TreeRoot.StatusText = "CritterKillSquad: " + txt;
                                        Thread.Sleep(BotCKS.ran.Next(700, 3100));
                                    }

                                }

                                if (AOEunit != null && SpellManager.HasSpell(BloodBoil.Id) && !BloodBoil.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                                {
                                    if (AOEunit.Distance >= 3)
                                    {
                                        MoveToNode(AOEunit, 3);
                                    }

                                    TreeRoot.StatusText = "CritterKillSquad: cast " + BloodBoil.Name + " on ≈ " + BotCKS.AOEcount;
                                    BotCKS.slog("cast " + BloodBoil.Name + " on ≈ " + BotCKS.AOEcount);

                                    AOEunit.Target(); AOEunit.Face();
                                    BloodBoil.Cast();
                                    Thread.Sleep(BotCKS.ran.Next(200, 500));
                                }
                                else if (_unit.IsAlive && !StyxWoW.Me.IsAutoAttacking)
                                {
                                    if (_unit.Distance >= 3)
                                    {
                                        MoveToNode(_unit, 3);
                                    }

                                    TreeRoot.StatusText = "CritterKillSquad: cast " + AutoAtack.Name;
                                    BotCKS.slog("cast " + AutoAtack.Name);
                                    _unit.Target(); _unit.Face();
                                    AutoAtack.Cast();
                                    Thread.Sleep(BotCKS.ran.Next(200, 500));
                                }

                            }
                            #endregion

                            else

                                #region Rogue
                                if (StyxWoW.Me.Class == WoWClass.Rogue)
                                {
                                    TreeRoot.StatusText = "CritterKillSquad: slay node ...";
                                    WoWUnit AOEunit = BotCKS.getNode(_unit, 9, 2);

                                    WoWSpell FanOfKnives = WoWSpell.FromId(51723);

                                    if (AOEunit != null && SpellManager.HasSpell(FanOfKnives.Id) && !FanOfKnives.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                                    {
                                        if (AOEunit.Distance >= 3)
                                        {
                                            MoveToNode(AOEunit, 3);
                                        }

                                        TreeRoot.StatusText = "CritterKillSquad: cast " + FanOfKnives.Name + " on ≈ " + BotCKS.AOEcount;
                                        BotCKS.slog("cast " + FanOfKnives.Name + " on ≈ " + BotCKS.AOEcount);

                                        FanOfKnives.Cast();
                                        Thread.Sleep(BotCKS.ran.Next(400, 800));
                                    }
                                    else if (_unit.IsAlive)
                                    {
                                        if (_unit.Distance >= 3)
                                        {
                                            BotCKS.StuckDetector();
                                            MoveToNode(_unit, 3);
                                        }

                                        TreeRoot.StatusText = "CritterKillSquad: cast " + AutoAtack.Name;
                                        BotCKS.slog("cast " + AutoAtack.Name);

                                        _unit.Target(); _unit.Face();

                                        if(!StyxWoW.Me.IsAutoAttacking)
                                        AutoAtack.Cast();
                                    }
                                }
                                #endregion

                                else

                                    #region Mage
                                    if (StyxWoW.Me.Class == WoWClass.Mage)
                                    {
                                        TreeRoot.StatusText = "CritterKillSquad: slay node ...";
                                        WoWUnit AOEunit = BotCKS.getNode(_unit, 11, 2);

                                        WoWSpell ArcaneExplosion = WoWSpell.FromId(1449);

                                        if (AOEunit != null && SpellManager.HasSpell(ArcaneExplosion.Id) && !ArcaneExplosion.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                                        {
                                            if (AOEunit.Distance >= 3)
                                            {
                                                MoveToNode(AOEunit, 3);
                                            }

                                            TreeRoot.StatusText = "CritterKillSquad: cast " + ArcaneExplosion.Name + " on ≈ " + BotCKS.AOEcount;
                                            BotCKS.slog("cast " + ArcaneExplosion.Name + " on ≈ " + BotCKS.AOEcount);

                                            ArcaneExplosion.Cast();
                                            Thread.Sleep(BotCKS.ran.Next(200, 500));
                                        }
                                        else if (_unit.IsAlive)
                                        {
                                            if (_unit.Distance >= 3)
                                            {
                                                BotCKS.StuckDetector();
                                                MoveToNode(_unit, 3);
                                            }

                                            _unit.Target(); _unit.Face();

                                            if(!StyxWoW.Me.IsAutoAttacking)
                                            AutoAtack.Cast();
                                        }
                                    }
                                    #endregion

                                    else

                                        #region Warrior
                                        if (StyxWoW.Me.Class == WoWClass.Warrior)
                                        {
                                            TreeRoot.StatusText = "CritterKillSquad: slay node ...";
                                            WoWUnit AOEunit = BotCKS.getNode(_unit, 9, 2);

                                            WoWSpell BattleShout = WoWSpell.FromId(6673);
                                            WoWSpell ThunderClap = WoWSpell.FromId(6343);
                                            WoWSpell HeroicLeap = WoWSpell.FromId(6544);

                                            if (SpellManager.HasSpell(BattleShout.Id) && !BattleShout.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                                            {
                                                TreeRoot.StatusText = "CritterKillSquad: cast " + BattleShout.Name;
                                                BotCKS.slog("cast " + BattleShout.Name);

                                                BattleShout.Cast();
                                                Thread.Sleep(BotCKS.ran.Next(200, 500));                                            
                                            }

                                            if (AOEunit != null && SpellManager.HasSpell(ThunderClap.Id) && !ThunderClap.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                                            {
                                                if (AOEunit.Distance >= 3)
                                                {
                                                    MoveToNode(AOEunit, 3);
                                                }

                                                TreeRoot.StatusText = "CritterKillSquad: cast " + ThunderClap.Name + " on ≈ " + BotCKS.AOEcount;
                                                BotCKS.slog("cast " + ThunderClap.Name + " on ≈ " + BotCKS.AOEcount);

                                                ThunderClap.Cast();
                                                Thread.Sleep(BotCKS.ran.Next(200, 500));
                                            }
                                            else if (AOEunit != null && SpellManager.HasSpell(HeroicLeap.Id) && !HeroicLeap.Cooldown && !Styx.StyxWoW.GlobalCooldown)
                                            {
                                                if (AOEunit.Distance >= 3)
                                                {
                                                    MoveToNode(AOEunit, 3); 
                                                }

                                                TreeRoot.StatusText = "CritterKillSquad: cast " + ThunderClap.Name + " on ≈ " + BotCKS.AOEcount;
                                                BotCKS.slog("cast " + ThunderClap.Name + " on ≈ " + BotCKS.AOEcount);

                                                HeroicLeap.Cast();
                                                Thread.Sleep(BotCKS.ran.Next(200, 500)); 
                                            }
                                            else if (_unit.IsAlive)
                                            {
                                                if (_unit.Distance >= 3)
                                                {
                                                    BotCKS.StuckDetector();
                                                    MoveToNode(_unit, 3);
                                                }

                                                _unit.Target(); _unit.Face();
                                                if(!StyxWoW.Me.IsAutoAttacking)
                                                AutoAtack.Cast();

                                            }
                                        }
                                        #endregion


            }
            WoWMovement.MoveStop();
            BotCKS.StopStuckDetector();
                BotCKS.AOEcount = 0;
        }



        private static void MoveToNode(WoWUnit _wgo, int _maxDistance)
        {
            while (_wgo.Distance >= _maxDistance)
            {
                TreeRoot.StatusText = "CritterKillSquad: Move to best AOE point ... | dis: " + (int)_wgo.Distance + "yds";
                BotCKS.StuckDetector();

                WoWMovement.ClickToMove(_wgo.Location);

                Thread.Sleep(100);
            }
            BotCKS.StopStuckDetector();
            WoWMovement.MoveStop();
        }

        private static void MoveToNode(int _InLineOfSightDistance, WoWUnit _wgo)
        {
            while (_wgo != null && (_wgo.Distance >= _InLineOfSightDistance || (_wgo.Distance <= _InLineOfSightDistance && !_wgo.InLineOfSight)))
            {
                TreeRoot.StatusText = "CritterKillSquad: Move to best AOE point ... | dis: " + (int)_wgo.Distance + "yds";
                BotCKS.StuckDetector();

                WoWMovement.ClickToMove(_wgo.Location);

                Thread.Sleep(100);
            }
            BotCKS.StopStuckDetector();
            WoWMovement.MoveStop();
        }

    }

    public static class PlayerManager
    {
        static public Dictionary<WoWPlayer, double> players = new Dictionary<WoWPlayer, double>();
        private static Stopwatch sw = new Stopwatch();
        public static WoWPlayer exitingPlayer;
        public static void update()
        {
            double lastRun = sw.Elapsed.TotalSeconds;
            sw.Reset();
            sw.Start();
            Dictionary<WoWPlayer, double> newplayers = new Dictionary<WoWPlayer, double>();
            foreach (WoWPlayer player in ObjectManager.GetObjectsOfType<WoWPlayer>())
            {
                if (players.ContainsKey(player))
                {
                    newplayers.Add(player, players[player] + lastRun);
                    if (players[player] > BotCKS.maxTimeFollowed * 0.8)
                    {
                        Logging.Write("Been followed for " + newplayers[player] + " seconds by " + player.Name);
                    }
                }
                else
                {
                    newplayers.Add(player, 0);
                }
            }
            players = newplayers;
        }

        public static bool needExit(int time)
        {
            foreach (KeyValuePair<WoWPlayer, double> pt in players)
            {
                if (pt.Value > time)
                {
                    exitingPlayer = pt.Key;
                    return true;
                }
            }
            return false;
        }
    }

}
