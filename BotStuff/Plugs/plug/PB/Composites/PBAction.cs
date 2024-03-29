﻿using System;
using System.Windows.Forms;
using System.Xml.Serialization;
using Styx.WoWInternals.WoWObjects;
using Action = TreeSharp.Action;
using ObjectManager = Styx.WoWInternals.ObjectManager;
using System.Collections.Specialized;
using HighVoltz.Dynamic;

namespace HighVoltz.Composites
{
    public interface IPBComposite : ICloneable
    {
        PropertyBag Properties { get; }
        string Name { get; }
        string Title { get; }
        System.Drawing.Color Color { get; }
        string Help { get; }
        void Reset();
        bool IsDone { get; }
        void OnProfileLoad(System.Xml.Linq.XElement element);
        void OnProfileSave(System.Xml.Linq.XElement element);
    }

    #region PBAction
    public abstract class PBAction : Action, IPBComposite
    {
        protected Professionbuddy Pb;
        protected LocalPlayer Me = ObjectManager.Me;
        protected PBAction()
        {
            HasRunOnce = false;
            Pb = Professionbuddy.Instance;
// ReSharper disable DoNotCallOverridableMethodsInConstructor
            Properties = new PropertyBag();
// ReSharper restore DoNotCallOverridableMethodsInConstructor
            Expressions = new ListDictionary();
        }
        virtual public string Help { get { return ""; } }
        virtual public string Name { get { return "PBAction"; } }
        virtual public string Title { get { return string.Format("({0})", Name); } }
        virtual public ListDictionary Expressions { get; private set; }
        [XmlIgnore]
        System.Drawing.Color _color = System.Drawing.Color.Black;
        virtual public System.Drawing.Color Color {
            get { return _color; }
            set { _color = value; }
        }
        protected PropertyGrid PropertyGrid { get { return MainForm.IsValid ? MainForm.Instance.ActionGrid : null; } }
        protected void RefreshPropertyGrid()
        {
            if (PropertyGrid != null)
            {
                PropertyGrid.Refresh();
            }
        }
        protected void RegisterDynamicProperty(string propName)
        {
            Properties[propName].PropertyChanged += DynamicPropertyChanged;
        }
        public virtual bool IsDone { get; protected set; }
        protected bool HasRunOnce { get; set; }
        /// <summary>
        /// If overriding this method call base method or set HasRunOnce to false.
        /// </summary>
        protected virtual void RunOnce()
        {
            HasRunOnce = true;
        }
        public virtual PropertyBag Properties { get; protected set; }

        public virtual object Clone()
        {
            return this;
        }

        public virtual void Reset()
        {
            IsDone = false;
            HasRunOnce = false;
        }

        public virtual void OnProfileLoad(System.Xml.Linq.XElement element) { }

        public virtual void OnProfileSave(System.Xml.Linq.XElement element) { }

        void DynamicPropertyChanged(object sender, MetaPropArgs e)
        {
            ((IDynamicProperty)e.Value).AttachedComposite = this;
            DynamicCodeCompiler.CodeWasModified = true;
        }
    }
    #endregion
}
