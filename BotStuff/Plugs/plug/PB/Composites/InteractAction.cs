﻿using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Styx;
using Styx.Helpers;
using Styx.Logic.Combat;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using System.Diagnostics;
using ObjectManager = Styx.WoWInternals.ObjectManager;
using System.Drawing.Design;

namespace HighVoltz.Composites
{

    #region InteractAction

    internal sealed class InteractionAction : PBAction
    {
        public enum InteractActionType
        {
            NPC,
            GameObject,
        }

        [PbXmlAttribute]
        public InteractActionType InteractType
        {
            get { return (InteractActionType)Properties["InteractType"].Value; }
            set { Properties["InteractType"].Value = value; }
        }

        [PbXmlAttribute]
        public uint Entry
        {
            get { return (uint)Properties["Entry"].Value; }
            set { Properties["Entry"].Value = value; }
        }

        [PbXmlAttribute]
        public uint InteractDelay
        {
            get { return (uint)Properties["InteractDelay"].Value; }
            set { Properties["InteractDelay"].Value = value; }
        }

        [PbXmlAttribute]
        public WoWGameObjectType GameObjectType
        {
            get { return (WoWGameObjectType)Properties["GameObjectType"].Value; }
            set { Properties["GameObjectType"].Value = value; }
        }

        [PbXmlAttribute]
        public WoWSpellFocus SpellFocus
        {
            get { return (WoWSpellFocus)Properties["SpellFocus"].Value; }
            set { Properties["SpellFocus"].Value = value; }
        }

        public InteractionAction()
        {
            Properties["Entry"] = new MetaProp("Entry", typeof(uint),
                                               new EditorAttribute(typeof(PropertyBag.EntryEditor),
                                                                   typeof(UITypeEditor)));
            Properties["InteractDelay"] = new MetaProp("InteractDelay", typeof(uint));
            Properties["InteractType"] = new MetaProp("InteractType", typeof(InteractActionType),
                                                      new DisplayNameAttribute("Interact Type"));
            Properties["GameObjectType"] = new MetaProp("GameObjectType", typeof(WoWGameObjectType),
                                                        new DisplayNameAttribute("GameObject Type"));
            Properties["SpellFocus"] = new MetaProp("SpellFocus", typeof(WoWSpellFocus),
                                                    new DisplayNameAttribute("SpellFocus"));

            InteractDelay = Entry = 0u;
            InteractType = InteractActionType.GameObject;
            GameObjectType = WoWGameObjectType.Mailbox;
            SpellFocus = WoWSpellFocus.Anvil;

            Properties["SpellFocus"].Show = false;
            Properties["InteractType"].PropertyChanged += InteractionActionPropertyChanged;
            Properties["GameObjectType"].PropertyChanged += InteractionActionPropertyChanged;
        }

        private readonly Stopwatch _interactSw = new Stopwatch();

        protected override RunStatus Run(object context)
        {
            if (!IsDone)
            {
                WoWObject obj = null;
                if (InteractType == InteractActionType.NPC)
                {
                    if (Entry != 0)
                        obj =
                            ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == Entry).OrderBy(
                                u => u.Distance).FirstOrDefault();
                    else if (ObjectManager.Me.GotTarget)
                        obj = ObjectManager.Me.CurrentTarget;
                }
                else if (InteractType == InteractActionType.GameObject)
                    obj =
                        ObjectManager.GetObjectsOfType<WoWGameObject>().OrderBy(u => u.Distance).FirstOrDefault(
                            u => (Entry > 0 && u.Entry == Entry) || (u.SubType == GameObjectType &&
                                                                     (GameObjectType != WoWGameObjectType.SpellFocus ||
                                                                      (GameObjectType == WoWGameObjectType.SpellFocus &&
                                                                       u.SpellFocus == SpellFocus))));
                if (obj != null)
                {
                    if (!obj.WithinInteractRange)
                        Util.MoveTo(WoWMathHelper.CalculatePointFrom(Me.Location, obj.Location, 3));
                    else
                    {
                        if (InteractDelay > 0 &&
                            (!_interactSw.IsRunning || _interactSw.ElapsedMilliseconds < InteractDelay))
                        {
                            _interactSw.Start();
                        }
                        else
                        {
                            _interactSw.Reset();
                            if (InteractType == InteractActionType.NPC)
                                Lua.DoString("InteractUnit({0})", obj.Entry);
                            else
                                obj.Interact();
                            IsDone = true;
                        }
                    }
                }
                if (!IsDone)
                    return RunStatus.Success;
            }
            return RunStatus.Failure;
        }

        private void InteractionActionPropertyChanged(object sender, MetaPropArgs e)
        {
            switch (GameObjectType)
            {
                case WoWGameObjectType.SpellFocus:
                    Properties["SpellFocus"].Show = true;
                    break;
                default:
                    Properties["SpellFocus"].Show = false;
                    break;
            }
            switch (InteractType)
            {
                case InteractActionType.GameObject:
                    Properties["GameObjectType"].Show = true;
                    break;
                case InteractActionType.NPC:
                    Properties["GameObjectType"].Show = false;
                    break;
                default:
                    Properties["GameObjectType"].Show = true;
                    break;
            }
            RefreshPropertyGrid();
        }

        public override string Name
        {
            get { return "Interact"; }
        }

        public override string Title
        {
            get
            {
                return string.Format("{0}: {1} " + (Entry != 0 ? Entry.ToString(CultureInfo.InvariantCulture) : ""),
                                     Name, InteractType);
            }
        }

        public override string Help
        {
            get
            {
                return
                    "This action will cause your character to interact with a specific target. InteractDelay is a time in milliseconds to way before interacting with target.This can be also used to goto objects like forge/anvil or NPCs that are within 100 feet";
            }
        }

        public override object Clone()
        {
            return new InteractionAction
                       {
                           InteractType = this.InteractType,
                           Entry = this.Entry,
                           GameObjectType = this.GameObjectType,
                           SpellFocus = this.SpellFocus,
                           InteractDelay = this.InteractDelay,
                       };
        }
    }

    #endregion
}