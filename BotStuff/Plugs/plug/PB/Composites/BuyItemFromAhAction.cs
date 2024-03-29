﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using HighVoltz.Dynamic;
using Styx;
using Styx.Helpers;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using ObjectManager = Styx.WoWInternals.ObjectManager;

namespace HighVoltz.Composites
{
    struct BuyItemEntry
    {
        public string Name;     // localized name
        public uint Id;         // item ID 
        public uint BuyAmount;  // amount to buy
    }

    #region BuyItemFromAhAction

    sealed class BuyItemFromAhAction : PBAction
    {
        public enum ItemType
        {
            Item,
            RecipeMats,
            MaterialList,
        }
        [PbXmlAttribute]
        public ItemType ItemListType
        {
            get { return (ItemType)Properties["ItemListType"].Value; }
            set { Properties["ItemListType"].Value = value; }
        }
        [PbXmlAttribute]
        public string ItemID
        {
            get { return (string)Properties["ItemID"].Value; }
            set { Properties["ItemID"].Value = value; }
        }
        [PbXmlAttribute]
        [TypeConverterAttribute(typeof(PropertyBag.GoldEditorConverter))]
        public PropertyBag.GoldEditor MaxBuyout
        {
            get { return (PropertyBag.GoldEditor)Properties["MaxBuyout"].Value; }
            set { Properties["MaxBuyout"].Value = value; }
        }
        [PbXmlAttribute]
        [TypeConverter(typeof(DynamicProperty<int>.DynamivExpressionConverter))]
        public DynamicProperty<int> Amount
        {
            get { return (DynamicProperty<int>)Properties["Amount"].Value; }
            set { Properties["Amount"].Value = value; }
        }
        [PbXmlAttribute]
        public bool BuyAdditively
        {
            get { return (bool)Properties["BuyAdditively"].Value; }
            set { Properties["BuyAdditively"].Value = value; }
        }
        [PbXmlAttribute]
        public bool AutoFindAh
        {
            get { return (bool)Properties["AutoFindAh"].Value; }
            set { Properties["AutoFindAh"].Value = value; }
        }
        [PbXmlAttribute]
        public bool BidOnItem
        {
            get { return (bool)Properties["BidOnItem"].Value; }
            set { Properties["BidOnItem"].Value = value; }
        }
        WoWPoint _loc;
        [PbXmlAttribute]
        public string Location
        {
            get { return (string)Properties["Location"].Value; }
            set { Properties["Location"].Value = value; }
        }

        public BuyItemFromAhAction()
        {
            Properties["ItemID"] = new MetaProp("ItemID", typeof(string));
            Properties["MaxBuyout"] = new MetaProp("MaxBuyout", typeof(PropertyBag.GoldEditor),
                new DisplayNameAttribute("Max Buyout"), new TypeConverterAttribute(typeof(PropertyBag.GoldEditorConverter)));
            Properties["Amount"] = new MetaProp("Amount", typeof(DynamicProperty<int>),
                new TypeConverterAttribute(typeof(DynamicProperty<int>.DynamivExpressionConverter)));
            Properties["ItemListType"] = new MetaProp("ItemListType", typeof(ItemType), new DisplayNameAttribute("Buy ..."));
            Properties["AutoFindAh"] = new MetaProp("AutoFindAh", typeof(bool), new DisplayNameAttribute("Auto find AH"));
            Properties["BuyAdditively"] = new MetaProp("BuyAdditively", typeof(bool), new DisplayNameAttribute("Buy Additively"));

            Properties["BidOnItem"] = new MetaProp("BidOnItem", typeof(bool), new DisplayNameAttribute("Bid on Item"));
            Properties["Location"] = new MetaProp("Location", typeof(string), new EditorAttribute(typeof(PropertyBag.LocationEditor), typeof(UITypeEditor)));

            ItemID = "";
            Amount = new DynamicProperty<int>(this, "1");
            RegisterDynamicProperty("Amount");
            ItemListType = ItemType.Item;
            AutoFindAh = true;
            _loc = WoWPoint.Zero;
            Location = _loc.ToInvariantString();
            MaxBuyout = new PropertyBag.GoldEditor("100g0s0c");
            BidOnItem = false;
            BuyAdditively = true;

            Properties["AutoFindAh"].PropertyChanged += AutoFindAHChanged;
            Properties["ItemListType"].PropertyChanged += BuyItemFromAhActionPropertyChanged;
            Properties["Location"].PropertyChanged += LocationChanged;
            Properties["Amount"].Show = true;
            Properties["Location"].Show = false;
        }
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

        void BuyItemFromAhActionPropertyChanged(object sender, MetaPropArgs e)
        {
            if (ItemListType == ItemType.MaterialList)
            {
                Properties["ItemID"].Show = false;
                Properties["Amount"].Show = false;
                Properties["BuyAdditively"].Show = false;
            }
            else
            {
                Properties["ItemID"].Show = true;
                Properties["Amount"].Show = true;
                Properties["BuyAdditively"].Show = true;
            }
            RefreshPropertyGrid();
        }

        List<BuyItemEntry> _toQueueNameList;
        List<BuyItemEntry> _toBuyList;
        protected override RunStatus Run(object context)
        {
            if (!IsDone)
            {
                using (new FrameLock())
                {
                    if (Lua.GetReturnVal<int>("if AuctionFrame and AuctionFrame:IsVisible() then return 1 else return 0 end ", 0) == 0)
                    {
                        MovetoAuctioneer();
                    }
                    else
                    {
                        if (_toQueueNameList == null)
                        {
                            _toQueueNameList = BuildItemList();
                            _toBuyList = new List<BuyItemEntry>();
                        }
                        if ((_toQueueNameList == null || _toQueueNameList.Count == 0) && _toBuyList.Count == 0)
                        {
                            IsDone = true;
                            return RunStatus.Failure;
                        }
                        if (_toQueueNameList != null && _toQueueNameList.Count > 0)
                        {
                            string name = GetLocalName(_toQueueNameList[0].Id);
                            if (!string.IsNullOrEmpty(name))
                            {
                                var item = _toQueueNameList[0];
                                item.Name = name;
                                _toBuyList.Add(item);
                                _toQueueNameList.RemoveAt(0);
                            }
                        }
                        if (_toBuyList.Count > 0)
                        {
                            if (BuyFromAH(_toBuyList[0]))
                            {
                                _toBuyList.RemoveAt(0);
                            }
                        }
                    }
                }
                if (!IsDone)
                    return RunStatus.Success;
            }
            return RunStatus.Failure;
        }

        //indexs are {0}=ItemsCounter,NumOfItemToBuy, {1}=ItemID, {2}=maxBuyout, {3}=BidOnItem ? 1 : 0
        private const string BuyFromAHLuaFormat =
        "local A,totalA= GetNumAuctionItems('list') " +
        "local amountBought={0} " +
        "local want={1} " +
        "local each={3} " +
        "local useBid={4} " +
        "local buyPrice=0 " +
        "for index=1, A do " +
            "local name,_,cnt,_,_,_,_,minBid,minInc,buyout,bidNum,isHighBidder,owner,sold,id=GetAuctionItemInfo('list', index) " +
            "if useBid == 1 and buyout > each*cnt and isHighBidder == nil then " +
                "if bidNum == nil then " +
                    "buyPrice = minBid + minInc " +
                "else " +
                    "buyPrice = bidNum + minInc " +
                "end " +
            "else " +
                "buyPrice = buyout " +
            "end " +
            "if id == {2} and buyPrice > 0 and buyPrice <= each*cnt and amountBought < want then " +
                "amountBought = amountBought + cnt " +
                "PlaceAuctionBid('list', index,buyPrice) " +
            "end " +
            "if amountBought >=  want then return -1 end " +
        "end " +
        "return amountBought ";

        Stopwatch _queueTimer = new Stopwatch();
        int _totalAuctions;
        int _counter;
        int _page;
        bool BuyFromAH(BuyItemEntry bie)
        {
            bool done = false;
            if (!_queueTimer.IsRunning)
            {
                string lua = string.Format("QueryAuctionItems(\"{0}\" ,nil,nil,nil,nil,nil,{1}) return 1",
                    bie.Name.ToFormatedUTF8(), _page);
                Lua.GetReturnVal<int>(lua, 0);
                Professionbuddy.Debug("Searching AH for {0}", bie.Name);
                _queueTimer.Start();
            }
            else if (_queueTimer.ElapsedMilliseconds <= 10000)
            {
                if (Lua.GetReturnVal<int>("if CanSendAuctionQuery('list') == 1 then return 1 else return 0 end ", 0) == 1)
                {
                    _totalAuctions = Lua.GetReturnVal<int>("return GetNumAuctionItems('list')", 1);
                    _queueTimer.Stop();
                    _queueTimer.Reset();
                    if (_totalAuctions > 0)
                    {
                        string lua = string.Format(BuyFromAHLuaFormat,
                            _counter, bie.BuyAmount, bie.Id, MaxBuyout.TotalCopper, BidOnItem ? 1 : 0);
                        _counter = Lua.GetReturnVal<int>(lua, 0);
                        if (_counter == -1 || ++_page >= (int)Math.Ceiling((double)_totalAuctions / 50))
                            done = true;
                    }
                    else
                        done = true;
                }
            }
            else
            {
                done = true;
            }
            if (done)
            {
                _queueTimer = new Stopwatch();
                _totalAuctions = 0;
                _counter = 0;
                _page = 0;
            }
            return done;
        }

        void MovetoAuctioneer()
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

        string GetLocalName(uint id)
        {
            Professionbuddy.Debug("Queueing server for Item: {0}", id);
            return TradeSkillFrame.GetItemCacheName(id);
        }

        List<BuyItemEntry> BuildItemList()
        {
            var list = new List<BuyItemEntry>();
            var idList = new List<uint>();
            string[] entries = ItemID.Split(',');
            if (entries.Length > 0)
            {
                foreach (var entry in entries)
                {
                    uint temp;
                    uint.TryParse(entry.Trim(), out temp);
                    idList.Add(temp);
                }
            }
            else
            {
                Professionbuddy.Err("No ItemIDs are specified");
                IsDone = true;
            }

            switch (ItemListType)
            {
                case ItemType.Item:
                    list.AddRange(idList.Select(id => new BuyItemEntry
                                                          {
                                                              Id = id,
                                                              BuyAmount = (uint)(!BuyAdditively ? Amount - Util.GetCarriedItemCount(id) : Amount)
                                                          }));
                    break;
                case ItemType.MaterialList:
                    list.AddRange(Pb.MaterialList.Select(kv => new BuyItemEntry { Id = kv.Key, BuyAmount = (uint)kv.Value }));
                    break;
                case ItemType.RecipeMats:
                    list.AddRange(from id in idList
                                  select (from tradeskill in Pb.TradeSkillList
                                          where tradeskill.Recipes.ContainsKey(id)
                                          select tradeskill.Recipes[id]).FirstOrDefault()
                                      into recipe
                                      where recipe != null
                                      from ingred in recipe.Ingredients
                                      let toBuyAmount = (int)((ingred.Required * Amount) - Ingredient.GetInBagItemCount(ingred.ID))
                                      where toBuyAmount > 0
                                      select new BuyItemEntry { Id = ingred.ID, BuyAmount = (uint)toBuyAmount });
                    break;
            }
            return list;
        }

        public override void Reset()
        {
            base.Reset();
            _toQueueNameList = null;
            _toBuyList = null;
        }
        public override string Name
        {
            get { return "Buy Item From AH"; }
        }

        public override string Title
        {
            get
            {
                return string.Format("{0}: {1} " + (ItemListType != ItemType.MaterialList ? "x" + Amount : ""), Name,
                    ItemListType != ItemType.MaterialList ? ItemID : "Material List");
            }
        }
        public override string Help
        {
            get
            {
                return "This action will buy a either a specified item, ingredients that a recipe requires or whatevers in the material list from the Auction house if item is below specified maximum price. The ItemID is the id of the item or the recipe Id if buying materials for a recipe.'Bid On Item', if set to true will bid on an item if buyout price > maxBuyout and minBid <= maxBuyout. ItemID accepts a comma separated list of Item IDs. BuyAdditively if set to true will buy axact amount of items regardless of item count player has in bags";
            }
        }

        public override object Clone()
        {
            return new BuyItemFromAhAction
                       {
                           ItemID = this.ItemID,
                           MaxBuyout = new PropertyBag.GoldEditor(this.MaxBuyout.ToString()),
                           Amount = this.Amount,
                           ItemListType = this.ItemListType,
                           AutoFindAh = this.AutoFindAh,
                           Location = this.Location,
                           BidOnItem = this.BidOnItem,
                           BuyAdditively = this.BuyAdditively,
                       };
        }
    }
    #endregion
}
