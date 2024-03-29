﻿using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using Styx;
using Styx.Database;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using ObjectManager = Styx.WoWInternals.ObjectManager;

namespace HighVoltz.Composites
{
    #region MoveToAction
    public sealed class MoveToAction : PBAction
    {
        public enum MoveToType
        {
            Location,
            NearestMailbox,
            NearestVendor,
            NearestFlight,
            NearestAH,
            NearestRepair,
            NearestReagentVendor,
            NearestBanker,
            NearestGB,
            NpcByID
        }
        public enum NavigationType { Navigator, ClickToMove };

        WoWPoint _loc;
        [PbXmlAttribute]
        public string Location
        {
            get { return (string)Properties["Location"].Value; }
            set { Properties["Location"].Value = value; }
        }
        [PbXmlAttribute]
        public MoveToType MoveType
        {
            get { return (MoveToType)Properties["MoveType"].Value; }
            set { Properties["MoveType"].Value = value; }
        }
        [PbXmlAttribute]
        public NavigationType Pathing
        {
            get { return (NavigationType)Properties["Pathing"].Value; }
            set { Properties["Pathing"].Value = value; }
        }
        [PbXmlAttribute]
        public uint Entry
        {
            get { return (uint)Properties["Entry"].Value; }
            set { Properties["Entry"].Value = value; }
        }
        private WoWPoint _locationDb = WoWPoint.Zero;

        public MoveToAction()
        {
            Properties["Entry"] = new MetaProp("Entry", typeof(uint), new EditorAttribute(typeof(PropertyBag.EntryEditor), typeof(UITypeEditor)));
            Properties["Location"] = new MetaProp("Location", typeof(string), new EditorAttribute(typeof(PropertyBag.LocationEditor), typeof(UITypeEditor)));
            Properties["MoveType"] = new MetaProp("MoveType", typeof(MoveToType), new DisplayNameAttribute("MoveTo Type"));
            Properties["Pathing"] = new MetaProp("Pathing", typeof(NavigationType), new DisplayNameAttribute("Use"));

            Entry = 0u;
            _loc = WoWPoint.Zero;
            Location = _loc.ToInvariantString();
            MoveType = MoveToType.Location;
            Pathing = NavigationType.Navigator;

            Properties["Entry"].Show = false;
            Properties["MoveType"].PropertyChanged += MoveToActionPropertyChanged;
            Properties["Location"].PropertyChanged += LocationChanged;
        }

        void LocationChanged(object sender, MetaPropArgs e)
        {
            _loc = Util.StringToWoWPoint(Location);
            Properties["Location"].PropertyChanged -= LocationChanged;
            Properties["Location"].Value = string.Format("{0}, {1}, {2}", _loc.X, _loc.Y, _loc.Z);
            Properties["Location"].PropertyChanged += LocationChanged;
            RefreshPropertyGrid();
        }

        void MoveToActionPropertyChanged(object sender, MetaPropArgs e)
        {
            switch (MoveType)
            {
                case MoveToType.Location:
                    Properties["Entry"].Show = false;
                    Properties["Location"].Show = true;
                    Properties["Pathing"].Show = true;
                    break;
                case MoveToType.NpcByID:
                    Properties["Entry"].Show = true;
                    Properties["Location"].Show = false;
                    Properties["Pathing"].Show = false;
                    break;
                default:
                    Properties["Entry"].Show = false;
                    Properties["Location"].Show = false;
                    Properties["Pathing"].Show = false;
                    break;
            }
            RefreshPropertyGrid();
        }

        readonly Stopwatch _concludingSw = new Stopwatch();
        const float SpeedModifer = 1.8f;
        protected override RunStatus Run(object context)
        {
            if (!IsDone)
            {

                if (MoveType != MoveToType.Location)
                {
                    _loc = GetLocationFromType(MoveType, Entry);
                    if (_loc == WoWPoint.Zero)
                    {
                        if (_locationDb == WoWPoint.Zero)
                        {
                            _locationDb = GetLocationFromDB(MoveType, Entry);
                        }
                        _loc = _locationDb;
                    }
                    if (_loc == WoWPoint.Zero)
                    {
                        Professionbuddy.Err("MoveToAction Failed.. Unable to find location from Database");
                        IsDone = true;
                        return RunStatus.Failure;
                    }
                }
                if (Entry > 0 && (!ObjectManager.Me.GotTarget || ObjectManager.Me.CurrentTarget.Entry != Entry))
                {
                    WoWUnit unit = ObjectManager.GetObjectsOfType<WoWUnit>(true).FirstOrDefault(u => u.Entry == Entry);
                    if (unit != null)
                        unit.Target();
                }
                float speed = ObjectManager.Me.MovementInfo.CurrentSpeed;
                Navigator.PathPrecision = speed > 7 ? (SpeedModifer * speed) / 7f : SpeedModifer;
                if (ObjectManager.Me.Location.Distance(_loc) > Navigator.PathPrecision)
                {
                    if (Pathing == NavigationType.ClickToMove)
                        WoWMovement.ClickToMove(_loc);
                    else
                        Util.MoveTo(_loc);
                }
                else
                {
                    if (!_concludingSw.IsRunning)
                        _concludingSw.Start();
                    else if (_concludingSw.ElapsedMilliseconds >= 2000)
                    {
                        IsDone = true;
                        Professionbuddy.Log("MoveTo Action completed for type {0}", MoveType);
                        _concludingSw.Stop();
                        _concludingSw.Reset();
                    }
                }
                if (!IsDone)
                    return RunStatus.Success;
            }
            return RunStatus.Failure;
        }

        static public WoWPoint GetLocationFromType(MoveToType type, uint entry)
        {
            WoWObject obj = null;
            if (entry != 0)
            {
                obj = ObjectManager.ObjectList.FirstOrDefault(o => o.Entry == entry);
            }
            if (obj == null)
            {
                switch (type)
                {
                    case MoveToType.NearestAH:
                        obj = ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsAuctioneer && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault();
                        break;
                    case MoveToType.NearestBanker:
                        obj = ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsBanker && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault();
                        break;
                    case MoveToType.NearestFlight:
                        obj = ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsFlightMaster && u.IsFriendly && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault();
                        break;
                    case MoveToType.NearestGB:
                        obj = ObjectManager.ObjectList.Where(u => {
                            if (u is WoWUnit)
                            {
                                var un = (WoWUnit)u;
                                if (un.IsGuildBanker)
                                    return true;
                            }
                            else if (u is WoWGameObject)
                            {
                                var go = (WoWGameObject)u;
                                if (go.SubType == WoWGameObjectType.GuildBank)
                                    return true;
                            }
                            return false;
                        }).OrderBy(u => u.Distance).FirstOrDefault();
                        break;
                    case MoveToType.NearestMailbox:
                        obj = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(u => u.SubType == WoWGameObjectType.Mailbox).OrderBy(u => u.Distance).FirstOrDefault();
                        break;
                    case MoveToType.NearestReagentVendor:
                        obj = ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsReagentVendor && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault();
                        break;
                    case MoveToType.NearestRepair:
                        obj = ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsRepairMerchant && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault();
                        break;
                    case MoveToType.NearestVendor:
                        obj = ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.IsAnyVendor && u.IsAlive).OrderBy(u => u.Distance).FirstOrDefault();
                        break;
                }
            }
            if (obj != null)
            {
                if (obj is WoWUnit && (!ObjectManager.Me.GotTarget || ObjectManager.Me.CurrentTarget != obj))
                {
                    ((WoWUnit)obj).Target();
                }
                return obj.Location;
            }
            return WoWPoint.Zero;
        }

        static public WoWPoint GetLocationFromDB(MoveToType type, uint entry)
        {
            NpcResult npcResults = null;
            switch (type)
            {
                case MoveToType.NearestAH:
                    npcResults = NpcQueries.GetNearestNpc(ObjectManager.Me.FactionTemplate, ObjectManager.Me.MapId,
                                                 ObjectManager.Me.Location, UnitNPCFlags.Auctioneer);
                    break;
                case MoveToType.NearestBanker:
                    npcResults = NpcQueries.GetNearestNpc(ObjectManager.Me.FactionTemplate, ObjectManager.Me.MapId,
                                                 ObjectManager.Me.Location, UnitNPCFlags.Banker);
                    break;
                case MoveToType.NearestFlight:
                    npcResults = NpcQueries.GetNearestNpc(ObjectManager.Me.FactionTemplate, ObjectManager.Me.MapId,
                                                 ObjectManager.Me.Location, UnitNPCFlags.Flightmaster);
                    break;
                case MoveToType.NearestGB:
                    npcResults = NpcQueries.GetNearestNpc(ObjectManager.Me.FactionTemplate, ObjectManager.Me.MapId,
                                                 ObjectManager.Me.Location, UnitNPCFlags.GuildBanker);
                    break;
                case MoveToType.NearestReagentVendor:
                    npcResults = NpcQueries.GetNearestNpc(ObjectManager.Me.FactionTemplate, ObjectManager.Me.MapId,
                                                 ObjectManager.Me.Location, UnitNPCFlags.ReagentVendor);
                    break;
                case MoveToType.NearestRepair:
                    npcResults = NpcQueries.GetNearestNpc(ObjectManager.Me.FactionTemplate, ObjectManager.Me.MapId,
                                                 ObjectManager.Me.Location, UnitNPCFlags.Repair);
                    break;
                case MoveToType.NearestVendor:
                    npcResults = NpcQueries.GetNearestNpc(ObjectManager.Me.FactionTemplate, ObjectManager.Me.MapId,
                                                 ObjectManager.Me.Location, UnitNPCFlags.Vendor);
                    break;
                case MoveToType.NpcByID:
                    npcResults = NpcQueries.GetNpcById(entry);
                    break;
            }
            if (npcResults != null)
                return npcResults.Location;
            return WoWPoint.Zero;
        }
        public override string Name { get { return "Move To"; } }
        public override string Title
        {
            get
            {
                return string.Format("{0}: {1} " +
                    ((MoveType == MoveToType.Location) ? Location : ""), Name, MoveType);
            }
        }
        public override string Help
        {
            get
            {
                return "This action will move your character to the selected location or if set to go to NearestXXX it will search the object manager and NPC database for the specific unit/object and move to it. It will also target NPCs so the interact action can be used right after this action finishes without having to specify an Npc Entry in it.";
            }
        }
        public override object Clone()
        {
            return new MoveToAction { MoveType = this.MoveType, Entry = this.Entry, _loc = this._loc, Location = this.Location, Pathing = this.Pathing };
        }
    }
    #endregion
}
