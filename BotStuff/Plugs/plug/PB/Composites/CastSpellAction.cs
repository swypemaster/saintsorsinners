﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using HighVoltz.Dynamic;
using Styx.Logic.Combat;
using Styx.WoWInternals;
using TreeSharp;
using Styx;
using Styx.Logic.BehaviorTree;
using Styx.WoWInternals.WoWObjects;
using ObjectManager = Styx.WoWInternals.ObjectManager;
using System.Collections.Generic;

namespace HighVoltz.Composites
{
    #region CastSpellAction
    public class RecipeConverter : ExpandableObjectConverter
    {
    }
    public sealed class CastSpellAction : PBAction
    {
        // number of times the recipe will be crafted
        public enum RepeatCalculationType
        {
            Specific,
            Craftable,
            Banker,
        }
        [PbXmlAttribute]
        public RepeatCalculationType RepeatType
        {
            get { return (RepeatCalculationType)Properties["RepeatType"].Value; }
            set { Properties["RepeatType"].Value = value; }
        }
        public int CalculatedRepeat { get { return CalculateRepeat(); } }
        [PbXmlAttribute]
        [TypeConverter(typeof(DynamicProperty<int>.DynamivExpressionConverter))]
        public DynamicProperty<int> Repeat
        {
            get { return (DynamicProperty<int>)Properties["Repeat"].Value; }
            set { Properties["Repeat"].Value = value; }
        }
        // number of times repeated.
        public int Casted
        {
            get { return (int)Properties["Casted"].Value; }
            set { Properties["Casted"].Value = value; }
        }
        // number of times repeated.
        [PbXmlAttribute]
        public uint Entry
        {
            get { return (uint)Properties["Entry"].Value; }
            set { Properties["Entry"].Value = value; }
        }
        public Recipe Recipe { get; private set; }

        [PbXmlAttribute]
        public bool CastOnItem
        {
            get { return (bool)Properties["CastOnItem"].Value; }
            set { Properties["CastOnItem"].Value = value; }
        }
        [PbXmlAttribute]
        public InventoryType ItemType
        {
            get { return (InventoryType)Properties["ItemType"].Value; }
            set { Properties["ItemType"].Value = value; }
        }
        [PbXmlAttribute]
        public uint ItemId
        {
            get { return (uint)Properties["ItemId"].Value; }
            set { Properties["ItemId"].Value = value; }
        }
        public string SpellName
        {
            get { return Recipe != null ? Recipe.Name : Entry.ToString(CultureInfo.InvariantCulture); }
        }
        public bool IsRecipe { get { return Recipe != null; } }
        // used to confim if a spell finished. set in the lua OnCastSucceeded callback.
        bool Confimed { get; set; }
        bool QueueIsRunning { get; set; }
        readonly Stopwatch _spamControl;
        uint _waitTime;
        uint _recastTime;

        public CastSpellAction()
        {
            _spamControl = new Stopwatch();
            QueueIsRunning = false;
            Properties["Casted"] = new MetaProp("Casted", typeof(int), new ReadOnlyAttribute(true));
            Properties["SpellName"] = new MetaProp("SpellName", typeof(string), new ReadOnlyAttribute(true));
            Properties["Repeat"] = new MetaProp("Repeat", typeof(DynamicProperty<int>), 
                new TypeConverterAttribute(typeof(DynamicProperty<int>.DynamivExpressionConverter)));
            Properties["Entry"] = new MetaProp("Entry", typeof(uint));
            Properties["CastOnItem"] = new MetaProp("CastOnItem", typeof(bool), new DisplayNameAttribute("Cast on Item"));
            Properties["ItemType"] = new MetaProp("ItemType", typeof(InventoryType), new DisplayNameAttribute("Item Type"));
            Properties["ItemId"] = new MetaProp("ItemId", typeof(uint));
            Properties["RepeatType"] = new MetaProp("RepeatType", typeof(RepeatCalculationType), new DisplayNameAttribute("Repeat Type"));
            // Properties["Recipe"] = new MetaProp("Recipe", typeof(Recipe), new TypeConverterAttribute(typeof(RecipeConverter)));

            Casted = 0;
            Repeat = new DynamicProperty<int>(this,"1");
            RegisterDynamicProperty("Repeat");
            Entry = 0u;
            RepeatType = RepeatCalculationType.Craftable;
            Recipe = null;
            CastOnItem = false;
            ItemType = InventoryType.Chest;
            ItemId = 0u;
            Properties["SpellName"].Value = SpellName;

            //Properties["Recipe"].Show = false;
            Properties["ItemType"].Show = false;
            Properties["ItemId"].Show = false;
            Properties["Casted"].PropertyChanged += OnCounterChanged;
            CheckTradeskillList();
            Properties["RepeatType"].PropertyChanged += CastSpellActionPropertyChanged;
            Properties["Entry"].PropertyChanged += OnEntryChanged;
            Properties["CastOnItem"].PropertyChanged += CastOnItemChanged;
        }
        void OnEntryChanged(object sender, MetaPropArgs e)
        {
            CheckTradeskillList();
        }

        void CastOnItemChanged(object sender, MetaPropArgs e)
        {
            if (CastOnItem)
            {
                Properties["ItemType"].Show = true;
                Properties["ItemId"].Show = true;
            }
            else
            {
                Properties["ItemType"].Show = false;
                Properties["ItemId"].Show = false;
            }
            RefreshPropertyGrid();
        }

        void CastSpellActionPropertyChanged(object sender, MetaPropArgs e)
        {
            IsDone = false;
            Pb.UpdateMaterials();
        }

        public CastSpellAction(Recipe recipe, int repeat, RepeatCalculationType repeatType)
            : this()
        {
            Recipe = recipe;
            Repeat = new DynamicProperty<int>(this,repeat.ToString(CultureInfo.InvariantCulture));
            Entry = recipe.ID;
            RepeatType = repeatType;
            //Properties["Recipe"].Show = true;
            Properties["SpellName"].Value = SpellName;
            Pb.UpdateMaterials();
        }

        int CalculateRepeat()
        {
            try
            {
                switch (RepeatType)
                {
                    case RepeatCalculationType.Specific:
                        return Repeat;
                    case RepeatCalculationType.Craftable:
                        return IsRecipe ? (int) Recipe.CanRepeatNum2 : Repeat;
                    case RepeatCalculationType.Banker:
                        if (IsRecipe && Pb.DataStore.ContainsKey(Recipe.CraftedItemID))
                            return Repeat - Pb.DataStore[Recipe.CraftedItemID];
                        return Repeat;
                }
                return Repeat;
            }
            catch
            {
                return 0;
            }
        }

        void OnCounterChanged(object sender, MetaPropArgs e)
        {
            RefreshPropertyGrid();
        }

        protected override RunStatus Run(object context)
        {
            if (!IsDone)
            {
                if (!IsRecipe)
                    CheckTradeskillList();
                if (Casted >= CalculatedRepeat)
                {
                    if (ObjectManager.Me.IsCasting && ObjectManager.Me.CastingSpell.Id == Entry)
                        SpellManager.StopCasting();
                    _spamControl.Stop();
                    _spamControl.Reset();
                    Lua.Events.DetachEvent("UNIT_SPELLCAST_SUCCEEDED", OnUnitSpellCastSucceeded);
                    IsDone = true;
                    return RunStatus.Failure;
                }
                // can't make recipe so stop trying.
                if (IsRecipe && Recipe.CanRepeatNum2 <= 0)
                {
                    Professionbuddy.Debug("{0} doesn't have enough material to craft.", SpellName);
                    IsDone = true;
                    return RunStatus.Failure;
                }
                if (Me.IsCasting && Me.CastingSpellId != Entry)
                    SpellManager.StopCasting();
                // we should confirm the last recipe in list so we don't make an axtra
                if (!Me.IsFlying && Casted + 1 < CalculatedRepeat || (Casted + 1 == CalculatedRepeat &&
                    (Confimed || !_spamControl.IsRunning || (_spamControl.ElapsedMilliseconds >= (_recastTime + (_recastTime / 2)) + _waitTime &&
                    !ObjectManager.Me.IsCasting))))
                {
                    if (!_spamControl.IsRunning || _spamControl.ElapsedMilliseconds >= _recastTime ||
                        (!ObjectManager.Me.IsCasting && _spamControl.ElapsedMilliseconds >= _waitTime))
                    {
                        if (ObjectManager.Me.IsMoving)
                            WoWMovement.MoveStop();
                        if (!QueueIsRunning)
                        {
                            Lua.Events.AttachEvent("UNIT_SPELLCAST_SUCCEEDED", OnUnitSpellCastSucceeded);
                            QueueIsRunning = true;
                            TreeRoot.StatusText = string.Format("Casting: {0}", IsRecipe ? Recipe.Name : Entry.ToString(CultureInfo.InvariantCulture));
                        }
                        WoWSpell spell = WoWSpell.FromId((int)Entry);
                        if (spell == null)
                        {
                            Professionbuddy.Err("Unable to find a spell with ID {0}",Entry);
                            return RunStatus.Failure; 
                        }
                        _recastTime = spell.CastTime;
                        Professionbuddy.Debug("Casting {0}, recast :{1}", spell.Name, _recastTime);
                        if (CastOnItem)
                        {
                            WoWItem item = TargetedItem;
                            if (item != null)
                                spell.CastOnItem(item);
                            else
                            {
                                Professionbuddy.Log("No item found to cast spell {0} on",
                                    IsRecipe ? Recipe.Name : Entry.ToString(CultureInfo.InvariantCulture));
                                IsDone = true;
                            }
                        }
                        else
                            spell.Cast();
                        _waitTime = StyxWoW.WoWClient.Latency * 2;
                        Confimed = false;
                        _spamControl.Reset();
                        _spamControl.Start();
                    }
                }
                if (!IsDone)
                    return RunStatus.Success;
            }
            return RunStatus.Failure;
        }

        void OnUnitSpellCastSucceeded(object obj, LuaEventArgs args)
        {
            try
            {
                if ((string)args.Args[0] == "player" && (uint)((double)args.Args[4]) == Entry)
                {
                    // confirm last recipe
                    if (Casted + 1 == CalculatedRepeat)
                    {
                        Confimed = true;
                    }
                    if (RepeatType != RepeatCalculationType.Craftable)
                        Casted++;
                    Professionbuddy.Instance.UpdateMaterials();
                    if (MainForm.IsValid)
                    {
                        MainForm.Instance.RefreshTradeSkillTabs();
                        MainForm.Instance.RefreshActionTree(typeof(CastSpellAction));
                    }
                }
            }
            catch (Exception ex) { Professionbuddy.Err(ex.ToString()); }
        }
        // check tradeskill list if spell is a recipe the player knows and updates Recipe if so.

        public void CheckTradeskillList()
        {
            if (Pb.IsTradeSkillsLoaded)
            {
                Recipe = Pb.TradeSkillList.Where(t => t.Recipes.ContainsKey(Entry)).Select(t => t.Recipes[Entry]).FirstOrDefault();
                if (IsRecipe)
                {
                    //Properties["Recipe"].Show = true;
                    Properties["SpellName"].Value = SpellName;
                    Pb.UpdateMaterials();
                }
                else
                {
                    //Properties["Recipe"].Show = false;
                    Properties["SpellName"].Value = SpellName;
                }
            }
        }

        WoWItem TargetedItem
        {
            get
            {
                return ObjectManager.Me.BagItems.Where(i => (i.ItemInfo.InventoryType == ItemType && ItemId == 0) ||
                    (ItemId > 0 && i.Entry == ItemId)).
                    OrderByDescending(i => i.ItemInfo.Level).ThenBy(i => i.Quality).FirstOrDefault();
            }
        }

        public override void Reset()
        {
            base.Reset();
            _spamControl.Stop();
            _spamControl.Reset();
            Lua.Events.DetachEvent("UNIT_SPELLCAST_SUCCEEDED", OnUnitSpellCastSucceeded);
            Casted = 0;
            QueueIsRunning = false;
            Confimed = false;
        }

        public override System.Drawing.Color Color { get { return IsRecipe ? System.Drawing.Color.DarkRed : System.Drawing.Color.Black; } }
        public override string Name { get { return IsRecipe ? "R" : "Cast Spell"; } }
        public override string Title { get { return string.Format("{0}: {1} x{2} ({3})", Name, SpellName, CalculatedRepeat, CalculatedRepeat - Casted); } }
        public override string Help
        {
            get
            {
                return "This action will cast the specified spell. This should only be used for recipes. To cast other spell use a Custom Action and SpellManager.Cast(). Repeat is the amount of times it will be casted depending on the repeat type used. if Repeat type is set to Specific it will Cast the spell Repeat times." +
                "Craftable means it will automatically set Repeat based on the number of times the recipe can be repeated with current materials in bags. Banker type uses the info from DataStore and will repeat the spell based on how many of the crafted items you have on yourself or on your alts/bank/gbank." +
                " So if for example you try to keep a stock 20 of each glyph on your bankers, your banker has 15 Glyph of Arcane Blast and you set repeat to 20, it will make 5 glyphs, The DataStore addon is required to used this mode.";
            }
        }
        public override object Clone()
        {
            return new CastSpellAction
                       {
                Entry = this.Entry,
                Repeat = this.Repeat,
                ItemType = this.ItemType,
                RepeatType = this.RepeatType,
                ItemId = this.ItemId,
                CastOnItem = this.CastOnItem
            };
        }


        public static List<CastSpellAction> GetCastSpellActionList(Composite pa)
        {
            if (pa is CastSpellAction)
                return new List<CastSpellAction> { (pa as CastSpellAction) };

            List<CastSpellAction> ret = null;
            var groupComposite = pa as GroupComposite;
            if (groupComposite != null)
            {
                foreach (Composite comp in (groupComposite).Children)
                {
                    List<CastSpellAction> tmp = GetCastSpellActionList(comp);
                    if (tmp != null)
                    {
                        // lets create a list only if we need to... (optimization)
                        if (ret == null)
                            ret = new List<CastSpellAction>();
                        ret.AddRange(tmp);
                    }
                }
            }
            return ret;
        }
    }
    #endregion

    static class WoWSpellExt
    {
        public static void CastOnItem(this WoWSpell spell, WoWItem item)
        {
            using (new FrameLock())
            {
                spell.Cast();
                Lua.DoString("UseContainerItem({0}, {1})", item.BagIndex + 1, item.BagSlot + 1);
            }
        }
    }
}
