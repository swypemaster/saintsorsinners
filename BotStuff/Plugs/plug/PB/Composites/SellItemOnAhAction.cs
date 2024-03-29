﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Xml.Linq;
using HighVoltz.Dynamic;
using Styx;
using TreeSharp;
using Styx.Helpers;
using Styx.WoWInternals;
using System.Diagnostics;
using Styx.Logic.Pathing;
using Styx.WoWInternals.WoWObjects;
using System.Drawing.Design;

using ObjectManager = Styx.WoWInternals.ObjectManager;

namespace HighVoltz.Composites
{

    #region SellItemOnAhAction

    sealed class SellItemOnAhAction : PBAction
    {
        public enum RunTimeType { _12_Hours = 1, _24_Hours, _48_Hours, }
        public enum AmountBasedType { Amount, Everything }
        [PbXmlAttribute]
        public bool UseCategory
        {
            get { return (bool)Properties["UseCategory"].Value; }
            set { Properties["UseCategory"].Value = value; }
        }
        public WoWItemClass Category
        {
            get { return (WoWItemClass)Properties["Category"].Value; }
            set
            {
                Properties["Category"].Value = value;
            }
        }
        public object SubCategory
        {
            get
            { // since subCategory type is sometimes set last we need to wait at a later peried to actually convert the enum value.
                return Properties["SubCategory"].Value;
            }
            set
            {
                Properties["SubCategory"].Value = value;
            }
        }

        [PbXmlAttribute]
        public RunTimeType RunTime
        {
            get { return (RunTimeType)Properties["RunTime"].Value; }
            set { Properties["RunTime"].Value = value; }
        }
        [PbXmlAttribute]
        public AmountBasedType AmountType
        {
            get { return (AmountBasedType)Properties["AmountType"].Value; }
            set { Properties["AmountType"].Value = value; }
        }
        [PbXmlAttribute]
        [PbXmlAttribute("ItemName")]
        public string ItemID
        {
            get { return (string)Properties["ItemID"].Value; }
            set { Properties["ItemID"].Value = value; }
        }
        [PbXmlAttribute]
        [TypeConverterAttribute(typeof(PropertyBag.GoldEditorConverter))]
        public PropertyBag.GoldEditor MinBuyout
        {
            get { return (PropertyBag.GoldEditor)Properties["MinBuyout"].Value; }
            set { Properties["MinBuyout"].Value = value; }
        }
        [PbXmlAttribute]
        [TypeConverterAttribute(typeof(PropertyBag.GoldEditorConverter))]
        public PropertyBag.GoldEditor MaxBuyout
        {
            get { return (PropertyBag.GoldEditor)Properties["MaxBuyout"].Value; }
            set { Properties["MaxBuyout"].Value = value; }
        }
        [PbXmlAttribute]
        public uint StackSize
        {
            get { return (uint)Properties["StackSize"].Value; }
            set { Properties["StackSize"].Value = value; }
        }
        [PbXmlAttribute]
        public uint IgnoreStackSizeBelow
        {
            get { return (uint)Properties["IgnoreStackSizeBelow"].Value; }
            set { Properties["IgnoreStackSizeBelow"].Value = value; }
        }
        [PbXmlAttribute]
        [TypeConverter(typeof(DynamicProperty<int>.DynamivExpressionConverter))]
        public DynamicProperty<int> Amount
        {
            get { return (DynamicProperty<int>)Properties["Amount"].Value; }
            set { Properties["Amount"].Value = value; }
        }
        [PbXmlAttribute]
        public float BidPrecent
        {
            get { return (float)Properties["BidPrecent"].Value; }
            set { Properties["BidPrecent"].Value = value; }
        }
        [PbXmlAttribute]
        public double UndercutPrecent
        {
            get { return (double)Properties["UndercutPrecent"].Value; }
            set { Properties["UndercutPrecent"].Value = value; }
        }
        [PbXmlAttribute]
        public bool AutoFindAh
        {
            get { return (bool)Properties["AutoFindAh"].Value; }
            set { Properties["AutoFindAh"].Value = value; }
        }
        [PbXmlAttribute]
        public bool PostIfBelowMinBuyout
        {
            get { return (bool)Properties["PostIfBelowMinBuyout"].Value; }
            set { Properties["PostIfBelowMinBuyout"].Value = value; }
        }
        WoWPoint _loc;
        [PbXmlAttribute]
        public string Location
        {
            get { return (string)Properties["Location"].Value; }
            set { Properties["Location"].Value = value; }
        }

        public SellItemOnAhAction()
        {
            Properties["ItemID"] = new MetaProp("ItemID", typeof(string), new DisplayNameAttribute("Item ID List"));
            Properties["RunTime"] = new MetaProp("RunTime", typeof(RunTimeType), new DisplayNameAttribute("Auction Duration"));
            Properties["MinBuyout"] = new MetaProp("MinBuyout", typeof(PropertyBag.GoldEditor),
                new DisplayNameAttribute("Min Buyout"), new TypeConverterAttribute(typeof(PropertyBag.GoldEditorConverter)));
            Properties["MaxBuyout"] = new MetaProp("MaxBuyout", typeof(PropertyBag.GoldEditor),
                new DisplayNameAttribute("Max Buyout"), new TypeConverterAttribute(typeof(PropertyBag.GoldEditorConverter)));
            Properties["Amount"] = new MetaProp("Amount", typeof(DynamicProperty<int>),
                new TypeConverterAttribute(typeof(DynamicProperty<int>.DynamivExpressionConverter)));
            Properties["StackSize"] = new MetaProp("StackSize", typeof(uint));
            Properties["IgnoreStackSizeBelow"] = new MetaProp("IgnoreStackSizeBelow", typeof(uint), new DisplayNameAttribute("Ignore StackSize Below"));
            Properties["AmountType"] = new MetaProp("AmountType", typeof(AmountBasedType), new DisplayNameAttribute("Sell"));
            Properties["AutoFindAh"] = new MetaProp("AutoFindAh", typeof(bool), new DisplayNameAttribute("Auto find AH"));
            Properties["Location"] = new MetaProp("Location", typeof(string), new EditorAttribute(typeof(PropertyBag.LocationEditor), typeof(UITypeEditor)));
            Properties["BidPrecent"] = new MetaProp("BidPrecent", typeof(float));
            Properties["UndercutPrecent"] = new MetaProp("UndercutPrecent", typeof(double));
            Properties["UseCategory"] = new MetaProp("UseCategory", typeof(bool), new DisplayNameAttribute("Use Category"));
            Properties["Category"] = new MetaProp("Category", typeof(WoWItemClass), new DisplayNameAttribute("Item Category"));
            Properties["SubCategory"] = new MetaProp("SubCategory", typeof(WoWItemTradeGoodsClass), new DisplayNameAttribute("Item SubCategory"));
            Properties["PostIfBelowMinBuyout"] = new MetaProp("PostIfBelowMinBuyout", typeof(bool), new DisplayNameAttribute("Post if Below MinBuyout"));

            ItemID = "";
            MinBuyout = new PropertyBag.GoldEditor("0g10s0c");
            MaxBuyout = new PropertyBag.GoldEditor("100g0s0c");
            RunTime = RunTimeType._24_Hours;
            Amount = new DynamicProperty<int>(this, "10");
            RegisterDynamicProperty("Amount");
            StackSize = 20u;
            IgnoreStackSizeBelow = 1u;
            AmountType = AmountBasedType.Everything;
            AutoFindAh = true;
            BidPrecent = 95f;
            UndercutPrecent = 0.1;
            _loc = WoWPoint.Zero;
            Location = _loc.ToInvariantString();
            UseCategory = true;
            Category = WoWItemClass.TradeGoods;
            SubCategory = WoWItemTradeGoodsClass.None;
            PostIfBelowMinBuyout = true;

            Properties["AutoFindAh"].PropertyChanged += AutoFindAHChanged;
            Properties["AmountType"].PropertyChanged += SellItemToAhActionPropertyChanged;
            Properties["Location"].PropertyChanged += LocationChanged;
            Properties["UseCategory"].PropertyChanged += UseCategoryChanged;
            Properties["Category"].PropertyChanged += CategoryChanged;

            Properties["ItemID"].Show = false;
            Properties["Amount"].Show = false;
            Properties["Location"].Show = false;
        }

        #region Callbacks
        void LocationChanged(object sender, MetaPropArgs e)
        {
            _loc = Util.StringToWoWPoint((string)((MetaProp)sender).Value);
            Properties["Location"].PropertyChanged -= LocationChanged;
            Properties["Location"].Value = string.Format("{0}, {1}, {2}", _loc.X, _loc.Y, _loc.Z);
            Properties["Location"].PropertyChanged += LocationChanged;
            RefreshPropertyGrid();
        }

        void AutoFindAHChanged(object sender, MetaPropArgs e)
        {
            Properties["Location"].Show = !AutoFindAh;
            RefreshPropertyGrid();
        }

        void SellItemToAhActionPropertyChanged(object sender, MetaPropArgs e)
        {
            Properties["Amount"].Show = AmountType != AmountBasedType.Everything;
            RefreshPropertyGrid();
        }

        void UseCategoryChanged(object sender, MetaPropArgs e)
        {
            if (UseCategory)
            {
                Properties["ItemID"].Show = false;
                Properties["Category"].Show = true;
                Properties["SubCategory"].Show = true;
            }
            else
            {
                Properties["ItemID"].Show = true;
                Properties["Category"].Show = false;
                Properties["SubCategory"].Show = false;
            }
            RefreshPropertyGrid();
        }

        void CategoryChanged(object sender, MetaPropArgs e)
        {
            object subCategory = Callbacks.GetSubCategory(Category);
            Properties["SubCategory"] = new MetaProp("SubCategory", subCategory.GetType(),
                new DisplayNameAttribute("Item SubCategory"));
            SubCategory = subCategory;
            RefreshPropertyGrid();
        }

        #endregion

        List<AuctionEntry> _toScanItemList;
        List<AuctionEntry> _toSellItemList;
        protected override RunStatus Run(object context)
        {
            if (!IsDone)
            {
                if (Lua.GetReturnVal<int>("if AuctionFrame and AuctionFrame:IsVisible() then return 1 else return 0 end ", 0) == 0)
                {
                    MoveToAh();
                }
                else
                {
                    if (_toScanItemList == null)
                    {
                        _toScanItemList = BuildScanItemList();
                        _toSellItemList = new List<AuctionEntry>();
                    }
                    if (_toScanItemList.Count == 0 && _toSellItemList.Count == 0)
                    {
                        _toScanItemList = null;
                        IsDone = true;
                        return RunStatus.Failure;
                    }
                    if (_toScanItemList.Count > 0)
                    {
                        AuctionEntry ae = _toScanItemList[0];
                        bool scanDone = ScanAh(ref ae);
                        _toScanItemList[0] = ae; // update
                        if (scanDone)
                        {
                            uint lowestBo = ae.LowestBo;
                            if (lowestBo > MaxBuyout.TotalCopper)
                                ae.Buyout = MaxBuyout.TotalCopper;
                            else if (lowestBo < MinBuyout.TotalCopper)
                                ae.Buyout = MinBuyout.TotalCopper;
                            else
                                ae.Buyout = lowestBo - (uint)Math.Ceiling((lowestBo * UndercutPrecent / 100d));
                            ae.Bid = (uint)(ae.Buyout * BidPrecent / 100d);
                            bool enoughItemsPosted = AmountType == AmountBasedType.Amount && ae.MyAuctions >= Amount;
                            bool tooLowBuyout = !PostIfBelowMinBuyout && lowestBo < MinBuyout.TotalCopper;

                            Professionbuddy.Debug("PB: PostIfBelowMinBuyout:{0} ", PostIfBelowMinBuyout, MinBuyout.TotalCopper);
                            Professionbuddy.Debug("PB: lowestBo:{0}  MinBuyout.TotalCopper: {1}", lowestBo, MinBuyout.TotalCopper);
                            Professionbuddy.Debug("PB: tooLowBuyout:{0} enoughItemsPosted: {1}", enoughItemsPosted, enoughItemsPosted);

                            if (!enoughItemsPosted && !tooLowBuyout)
                            {
                                _toSellItemList.Add(ae);
                            }
                            else
                                Professionbuddy.Log("Skipping {0} since {1}",
                                    ae.Name, tooLowBuyout ? string.Format("lowest buyout:{0} is below MinBuyout:{1}",
                                    AuctionEntry.GoldString(lowestBo), MinBuyout) :
                                    string.Format("{0} items from me are already posted. Max amount is {1}",
                                    ae.MyAuctions, Amount));
                            _toScanItemList.RemoveAt(0);
                        }
                        if (_toScanItemList.Count == 0)
                            Professionbuddy.Debug("Finished scanning for items");
                    }
                    if (_toSellItemList.Count > 0)
                    {
                        if (SellOnAh(_toSellItemList[0]))
                        {
                            Professionbuddy.Log("Selling {0}", _toSellItemList[0]);
                            _toSellItemList.RemoveAt(0);
                        }
                    }
                }
                return RunStatus.Success;
            }
            return RunStatus.Failure;
        }

        #region Auction House

        // indexes are {0}=LowestBuyout ,{1}=MyAuctionNum ,{2}=ItemID, {3}=IgnoreStackSizeBelow, {4}=MaxStackSize
        public const string ScanAHFormatLua =
            "local A,totalA= GetNumAuctionItems('list') " +
            "local me = GetUnitName('player') " +
            "local auctionInfo = {{{0},{1}}} " +
            "for index=1, A do " +
                "local name,_,cnt,_,_,_,_,minBid,_,buyout,_,_,owner,sold,id=GetAuctionItemInfo('list', index) " +
                "if id == {2} and owner ~= me and cnt >= {3} and buyout > 0 and buyout/cnt <  auctionInfo[1] then " +
                    "auctionInfo[1] = floor(buyout/cnt) " +
                "end " +
                "if id == {2} and owner == me and cnt <= {4} then auctionInfo[2] = auctionInfo[2] + 1 end " +
            "end " +
            "return unpack(auctionInfo) ";

        readonly Stopwatch _queueTimer = new Stopwatch();
        int _totalAuctions;
        int _page;
        bool ScanAh(ref AuctionEntry ae)
        {
            bool scanned = false;
            if (!_queueTimer.IsRunning)
            {
                string lua = string.Format("QueryAuctionItems(\"{0}\" ,nil,nil,nil,nil,nil,{1}) return 1",
                    ae.Name.ToFormatedUTF8(), _page);
                Lua.GetReturnVal<int>(lua, 0);
                Professionbuddy.Debug("Searching AH for {0}", ae.Name);
                _queueTimer.Start();
            }
            else if (_queueTimer.ElapsedMilliseconds <= 10000)
            {
                using (new FrameLock())
                {
                    if (Lua.GetReturnVal<int>("if CanSendAuctionQuery('list') == 1 then return 1 else return 0 end ", 0) == 1)
                    {
                        _queueTimer.Stop();
                        _queueTimer.Reset();
                        _totalAuctions = Lua.GetReturnVal<int>("return GetNumAuctionItems('list')", 1);
                        string lua = string.Format(ScanAHFormatLua, ae.LowestBo, ae.MyAuctions, ae.Id, IgnoreStackSizeBelow,StackSize);
                        List<string> retVals = Lua.GetReturnValues(lua);
                        uint.TryParse(retVals[0], out ae.LowestBo);
                        uint.TryParse(retVals[1], out ae.MyAuctions);
                        if (++_page >= (int)Math.Ceiling((double)_totalAuctions / 50))
                            scanned = true;
                    }
                }
            }
            else
            {
                scanned = true;
            }
            // reset to default values in preparations for next scan
            if (scanned)
            {
                Professionbuddy.Debug("lowest buyout {0}", ae.LowestBo);
                _queueTimer.Stop();
                _queueTimer.Reset();
                _totalAuctions = 0;
                _page = 0;
            }
            return scanned;
        }


        // indexs are {0}=ItemID, {1}=amount, {2}=StackSize, {3}=bid, {4}=buyout, {5}=duration(1-3)
        const string SellOnAHLuaFormat =
                    "local itemID = {0} " +
                    "local amount = {1} " +
                    "local bid = {3} " +
                    "local bo = {4} " +
                    "local runtime = {5} " +
                    "local stack = {2} " +
                    "local sold = 0 " +
                    "local leftovers = 0 " +
                    "local numItems = GetItemCount(itemID) " +
                    "if numItems == 0 then return -1 end " +
                    "if AuctionProgressFrame:IsVisible() == nil then " +
                        "AuctionFrameTab3:Click() " +
                        "local _,_,_,_,_,_,_,maxStack= GetItemInfo(itemID) " +
                        "if maxStack < stack then stack = maxStack end " +
                        "if amount * stack > numItems then " +
                          "amount = floor(numItems/stack) " +
                          "if amount <= 0 then " +
                             "amount = 1 " +
                             "stack = numItems " +
                          "else " +
                             "leftovers = numItems-(amount*stack) " +
                          "end " +
                       "end " +
                       "for bag = 0,4 do " +
                          "for slot=GetContainerNumSlots(bag),1,-1 do " +
                             "local id = GetContainerItemID(bag,slot) " +
                             "local _,c,l = GetContainerItemInfo(bag, slot) " +
                             "if id == itemID and l == nil then " +
                                "PickupContainerItem(bag, slot) " +
                                "ClickAuctionSellItemButton() " +
                                "StartAuction(bid*stack, bo*stack, runtime,stack,amount) " +
                                "return leftovers " +
                             "end " +
                          "end " +
                       "end " +
                    "else " +
                       "return -1 " +
                    "end ";
        bool _posted;
        int _leftOver;
        bool SellOnAh(AuctionEntry ae)
        {
            if (!_posted)
            {
                int subAmount = AmountType == AmountBasedType.Amount ? Amount - (int)ae.MyAuctions : Amount;
                int amount = AmountType == AmountBasedType.Everything ?
                    (_leftOver == 0 ? int.MaxValue : _leftOver) :
                    (_leftOver == 0 ? subAmount : _leftOver);
                string lua = string.Format(SellOnAHLuaFormat, ae.Id, amount, StackSize, ae.Bid, ae.Buyout, (int)RunTime);
                var ret = Lua.GetReturnVal<int>(lua, 0);
                if (ret != -1) // returns -1 if waiting for auction to finish posting..
                    _leftOver = ret;
                if (_leftOver == 0)
                    _posted = true;
            }
            //wait for auctions to finish listing before moving on
            if (_posted)
            {
                bool ret = Lua.GetReturnVal<int>("if AuctionProgressFrame:IsVisible() == nil then return 1 else return 0 end ", 0) == 1;
                if (ret) // we're done listing this item so reset to default values
                {
                    _posted = false;
                    _leftOver = 0;
                }
                return ret;
            }
            return false;
        }

        #endregion

        void MoveToAh()
        {
            WoWPoint movetoPoint = _loc;
            WoWUnit auctioneer;
            if (AutoFindAh || movetoPoint == WoWPoint.Zero)
            {
                auctioneer = ObjectManager.GetObjectsOfType<WoWUnit>().Where(o => o.IsAuctioneer && o.IsAlive)
                    .OrderBy(o => o.Distance).FirstOrDefault();
            }
            else
            {
                auctioneer = ObjectManager.GetObjectsOfType<WoWUnit>().Where(o => o.IsAuctioneer
                    && o.Location.Distance(_loc) < 5)
                    .OrderBy(o => o.Distance).FirstOrDefault();
            }
            if (auctioneer != null)
                movetoPoint = WoWMathHelper.CalculatePointFrom(Me.Location, auctioneer.Location, 3);
            else if (movetoPoint == WoWPoint.Zero)
                movetoPoint = MoveToAction.GetLocationFromDB(MoveToAction.MoveToType.NearestAH, 0);
            if (movetoPoint == WoWPoint.Zero)
            {
                Logging.Write("Unable to location Auctioneer, Maybe he's dead?");
            }
            if (movetoPoint.Distance(ObjectManager.Me.Location) > 4.5)
            {
                Util.MoveTo(movetoPoint);
            }
            else if (auctioneer != null)
            {
                auctioneer.Interact();
            }
        }

        List<AuctionEntry> BuildScanItemList()
        {
            var tmpItemlist = new List<AuctionEntry>();
            List<WoWItem> itemList;
            if (UseCategory)
            {
                itemList = ObjectManager.Me.BagItems.
                    Where(i => !i.IsSoulbound && !i.IsConjured && !i.IsDisabled &&
                        !Pb.ProtectedItems.Contains(i.Entry) &&
                        i.ItemInfo.ItemClass == Category && SubCategoryCheck(i)).ToList();
                foreach (var item in itemList)
                {
                    if (!_containsItem(item, tmpItemlist))
                        tmpItemlist.Add(new AuctionEntry(item.Name, item.Entry, 0, 0));
                }
            }
            else
            {
                string[] entries = ItemID.Split(',');
                if (entries.Length > 0)
                {
                    foreach (var entry in entries)
                    {
                        uint id;
                        uint.TryParse(entry.Trim(), out id);
                        itemList = ObjectManager.Me.BagItems.Where(i => !i.IsSoulbound && !i.IsConjured && i.Entry == id).ToList();
                        if (itemList.Count > 0)
                        {
                            tmpItemlist.Add(new AuctionEntry(itemList[0].Name, itemList[0].Entry, 0, 0));
                        }
                    }
                }
                else
                {
                    Professionbuddy.Err("No ItemIDs are specified");
                    IsDone = true;
                }
            }
            return tmpItemlist;
        }

        readonly Func<WoWItem, IEnumerable<AuctionEntry>, bool> _containsItem = (i, en) => en.Count(ae => ae.Id == i.Entry) > 0;

        bool SubCategoryCheck(WoWItem item)
        {
            int sub = Convert.ToInt32(SubCategory);
            if (sub == -1 || sub == 0)
                return true;
            var prop = item.ItemInfo.GetType().GetProperties().FirstOrDefault(t => t.PropertyType == SubCategory.GetType());
            if (prop != null)
            {
                object val = prop.GetValue(item.ItemInfo, null);
                if (val != null && (int)val == sub)
                    return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            _toScanItemList = null;
            _toSellItemList = null;
        }
        public override string Name
        {
            get { return "Sell Item To AH"; }
        }
        public override string Title
        {
            get
            {
                int sub = Convert.ToInt32(SubCategory);
                return string.Format("{0}: {1}{2}", Name, UseCategory ?
                    string.Format("{0} {1}", Category,
                    (SubCategory != null && sub != -1 && sub != 0) ?
                    "(" + SubCategory + ")" : "") : ItemID,
                    AmountType == AmountBasedType.Amount ? " x" + Amount : "");
            }
        }
        public override string Help
        {
            get
            {
                return "This action will sell a specific item that matches ID or all items that belong to a category and optionally sub catagory to the Auction house. If Cheapest item on AH is below minimum buyout then the item is listed at min buyout, else if the cheapest item is higher then the max buyout then the item is listed at max buyout, otherwise it's listed at lowest buyout minus undercut precent.ItemID takes a comma separated list of item IDs. All stacks of items below the IgnoreStackSizeBelow are ignored when undercutting auctions";
            }
        }
        public override object Clone()
        {
            return new SellItemOnAhAction
                       {
                           ItemID = this.ItemID,
                           MinBuyout = new PropertyBag.GoldEditor(this.MinBuyout.ToString()),
                           MaxBuyout = new PropertyBag.GoldEditor(this.MaxBuyout.ToString()),
                           Amount = this.Amount,
                           StackSize = this.StackSize,
                           IgnoreStackSizeBelow = this.IgnoreStackSizeBelow,
                           AmountType = this.AmountType,
                           AutoFindAh = this.AutoFindAh,
                           Location = this.Location,
                           UndercutPrecent = this.UndercutPrecent,
                           BidPrecent = this.BidPrecent,
                           RunTime = this.RunTime,
                           UseCategory = this.UseCategory,
                           Category = this.Category,
                           SubCategory = this.SubCategory,
                           PostIfBelowMinBuyout = this.PostIfBelowMinBuyout,
                       };
        }

        public override void OnProfileLoad(XElement element)
        {
            var cat = element.Attribute("Category");
            var subCatAttr = element.Attribute("SubCategory");
            var subCatTypeAttr = element.Attribute("SubCategoryType");
            if (cat != null)
            {
                Category = (WoWItemClass)Enum.Parse(typeof(WoWItemClass), cat.Value);
                cat.Remove();
            }
            if (subCatAttr != null && subCatTypeAttr != null)
            {
                Type subCategoryType;
                if (subCatTypeAttr.Value != "SubCategoryType")
                {
                    string typeName = string.Format("Styx.{0}", subCatTypeAttr.Value);
                    subCategoryType = Assembly.GetEntryAssembly().GetType(typeName);
                }
                else
                    subCategoryType = typeof(SubCategoryType);
                SubCategory = Enum.Parse(subCategoryType, subCatAttr.Value);
                subCatAttr.Remove();
                subCatTypeAttr.Remove();
            }
        }

        public override void OnProfileSave(XElement element)
        {
            element.Add(new XAttribute("Category", Category.ToString()));
            element.Add(new XAttribute("SubCategoryType", SubCategory.GetType().Name));
            element.Add(new XAttribute("SubCategory", SubCategory.ToString()));
        }
    }
    #endregion
}
