﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.Logic.BehaviorTree;
using System.Diagnostics;
using Styx.Logic.Inventory.Frames.LootFrame;
using Styx.Logic.Combat;
using TreeSharp;

namespace HighVoltz.Composites
{
    #region DisenchantAction

    sealed class DisenchantAction : PBAction
    {
        public enum DeActionType { Mill = 0, Prospect, Disenchant }
        public enum ItemTargetType { Specific, All }
        public enum DeItemQualites { Epic, Rare, Uncommon }
        [PbXmlAttribute]
        public DeActionType ActionType
        {
            get { return (DeActionType)Properties["ActionType"].Value; }
            set { Properties["ActionType"].Value = value; }
        }
        [PbXmlAttribute]
        public ItemTargetType ItemTarget
        {
            get { return (ItemTargetType)Properties["ItemTarget"].Value; }
            set { Properties["ItemTarget"].Value = value; }
        }
        [PbXmlAttribute]
        public DeItemQualites ItemQuality
        {
            get { return (DeItemQualites)Properties["ItemQuality"].Value; }
            set { Properties["ItemQuality"].Value = value; }
        }
        [PbXmlAttribute]
        public int ItemId
        {
            get { return (int)Properties["ItemId"].Value; }
            set { Properties["ItemId"].Value = value; }
        }
        public DisenchantAction()
        {
            Properties["ActionType"] = new MetaProp("ActionType", typeof(DeActionType),
                new DisplayNameAttribute("Action Type"));
            Properties["ItemTarget"] = new MetaProp("ItemTarget", typeof(ItemTargetType),
                new DisplayNameAttribute("Item Target"));
            Properties["ItemQuality"] = new MetaProp("ItemQuality", typeof(DeItemQualites),
                new DisplayNameAttribute("Item Quality"));
            Properties["ItemId"] = new MetaProp("ItemId", typeof(int));

            ActionType = DeActionType.Disenchant;
            ItemTarget = ItemTargetType.All;
            ItemQuality = DeItemQualites.Uncommon;
            ItemId = 0;
            Properties["ItemId"].Show = false;
            Properties["ActionType"].PropertyChanged += ActionTypeChanged;
            Properties["ItemTarget"].PropertyChanged += ItemTargetChanged;
        }

        void ActionTypeChanged(object sender, MetaPropArgs e)
        {
            Properties["ItemQuality"].Show = ActionType == DeActionType.Disenchant;
            RefreshPropertyGrid();
        }

        void ItemTargetChanged(object sender, MetaPropArgs e)
        {
            Properties["ItemId"].Show = ItemTarget == ItemTargetType.Specific;
            RefreshPropertyGrid();
        }

        readonly Stopwatch _castTimer = new Stopwatch();
        readonly Stopwatch _lootSw = new Stopwatch();
        readonly List<ulong> _blacklistedItems = new List<ulong>();
        ulong _lastItemGuid;
        uint _lastStackSize;
        int _tries;

        protected override RunStatus Run(object context)
        {
            if (!IsDone)
            {
                if (Me.IsFlying)
                    return RunStatus.Failure;
                if (_lootSw.IsRunning && _lootSw.ElapsedMilliseconds < 1000)
                    return RunStatus.Success;
                if (LootFrame.Instance != null && LootFrame.Instance.IsVisible)
                {
                    LootFrame.Instance.LootAll();
                    _lootSw.Reset();
                    _lootSw.Start();
                    return RunStatus.Success;
                }
                uint timeToWait = ((uint)ActionType * 1000) + 2500;
                if (!Me.IsCasting && (!_castTimer.IsRunning || _castTimer.ElapsedMilliseconds >= timeToWait))
                {
                    List<WoWItem> itemList = BuildItemList();
                    if (itemList == null || itemList.Count == 0)
                    {
                        IsDone = true;
                        Professionbuddy.Log("Done {0}ing", ActionType);
                    }
                    else
                    { // skip 'locked' items
                        int index = 0;
                        for (; index <= itemList.Count; index++)
                        {
                            if (!itemList[index].IsDisabled)
                                break;
                        }
                        if (index < itemList.Count)
                        {
                            if (itemList[index].Guid == _lastItemGuid && _lastStackSize == itemList[index].StackCount)
                            {
                                if (++_tries >= 3)
                                {
                                    Professionbuddy.Log("Unable to {0} {1}, BlackListing", Name, itemList[index].Name);
                                    if (!_blacklistedItems.Contains(_lastItemGuid))
                                        _blacklistedItems.Add(_lastItemGuid);
                                    return RunStatus.Success;
                                }
                            }
                            else
                            {
                                _tries = 0;
                            }
                            WoWSpell spell = WoWSpell.FromId(SpellId);
                            if (spell != null)
                            {
                                TreeRoot.GoalText = string.Format("{0}ing {1}", ActionType, itemList[index].Name);
                                Professionbuddy.Log(TreeRoot.GoalText);
                                //Lua.DoString("CastSpellByID({0}) UseContainerItem({1}, {2})",
                                //    spellId, ItemList[index].BagIndex + 1, ItemList[index].BagSlot + 1);
                                spell.CastOnItem(itemList[index]);
                                _lastItemGuid = itemList[index].Guid;
                                _lastStackSize = itemList[index].StackCount;
                                _castTimer.Reset();
                                _castTimer.Start();
                            }
                            else
                                IsDone = true;
                        }
                    }
                }
                if (!IsDone)
                    return RunStatus.Success;
            }
            return RunStatus.Failure;
        }
        int SpellId
        {
            get
            {
                switch (ActionType)
                {
                    case DeActionType.Disenchant:
                        return 13262;
                    case DeActionType.Mill:
                        return 51005;
                    case DeActionType.Prospect:
                        return 31252;
                }
                return 0;
            }
        }
        List<WoWItem> BuildItemList()
        {
            IEnumerable<WoWItem> itemQueue = from item in ObjectManager.Me.BagItems
                                             where !IsBlackListed(item) &&
                                             !Pb.ProtectedItems.Contains(item.Entry) &&
                                             ((ItemTarget == ItemTargetType.Specific && item.Entry == ItemId) ||
                                             ItemTarget == ItemTargetType.All)
                                             select item;

            switch (ActionType)
            {
                case DeActionType.Disenchant:
                    return itemQueue.Where(i => i.CanDisenchant() && CheckItemQuality(i)).ToList();
                case DeActionType.Mill:
                    return itemQueue.Where(i => i.CanMill() && i.StackCount >= 5).ToList();
                case DeActionType.Prospect:
                    return itemQueue.Where(i => i.CanProspect() && i.StackCount >= 5).ToList();
            }
            return null;
        }

        bool IsBlackListed(WoWItem item)
        {
            return _blacklistedItems.Contains(item.Guid);
        }

        bool CheckItemQuality(WoWItem item)
        {
            if (ItemQuality == DeItemQualites.Uncommon && item.Quality == WoWItemQuality.Uncommon)
                return true;
            if (ItemQuality == DeItemQualites.Rare &&
                (item.Quality == WoWItemQuality.Uncommon || item.Quality == WoWItemQuality.Rare))
                return true;
            if (ItemQuality == DeItemQualites.Epic && (item.Quality == WoWItemQuality.Uncommon ||
                item.Quality == WoWItemQuality.Rare || item.Quality == WoWItemQuality.Epic))
                return true;
            return false;
        }
        public override string Name { get { return ActionType.ToString(); } }
        public override string Title
        {
            get
            {
                return string.Format("{0}: {1} {2}", Name, ItemTarget == ItemTargetType.Specific ? ItemId.ToString(CultureInfo.InvariantCulture) : "All"
                    , ItemTarget == ItemTargetType.All && ActionType == DeActionType.Disenchant ? ItemQuality.ToString() : "");
            }
        }
        public override string Help
        {
            get
            {
                return "This action will Disenchant,Mill or Prospect items in the player's bags";
            }
        }
        public override object Clone()
        {
            return new DisenchantAction()
            {
                ActionType = this.ActionType,
                ItemTarget = this.ItemTarget,
                ItemQuality = this.ItemQuality,
                ItemId = this.ItemId
            };
        }
    }
    #endregion
    static class WoWitemExt
    {

        #region Prospect List
        // (itemId,required level)
        static readonly Dictionary<uint, int> ProspectList = new Dictionary<uint, int>
                                                                 {
            {2770, 20}, //Copper Ore
            {2771, 50}, //Tin Ore
            {2772, 125}, //Iron Ore
            {3858, 175}, //Mithril Ore
            {10620, 250}, //Thorium Ore
            {23424, 275}, //Fel Iron Ore
            {23425, 325}, //Adamantite Ore
            {36909, 350}, //Cobalt Ore
            {36912, 400}, //Saronite Ore
            {53038, 425}, //Obsidium Ore
            {36910, 450}, //Titanium Ore
            {52185, 475}, //Elementium Ore
            {52183, 500}, //Pyrite Ore
        };
        #endregion

        #region Millable Herb List
        // (itemId,required level)
        static readonly Dictionary<uint, int> MillableHerbList = new Dictionary<uint, int>
                                                                     {
            {765, 1},       //Silverleaf
            {2447, 1},      //Peacebloom
            {2449, 1},      //Earthroot
            {2450, 25},     //Briarthorn
            {2453, 25},     //Bruiseweed
            {785,  25},     //Mageroyal
            {3820, 25},     //Stranglekelp
            {2452, 25},     //Swiftthistle
            {3369, 75},     //Grave Moss
            {3356, 75},     //Kingsblood
            {3357, 75},     //Liferoot 
            {3355, 75},     //Wild Steelbloom
            {3819, 125},    //Dragon's Teeth
            {3818, 125},    //Fadeleaf
            {3821, 125},    //Goldthorn
            {3358, 125},    //Khadgar's Whisker
            {8836, 175},    //Arthas' Tears
            {8839, 175},    //Blindweed
            {4625, 175},    //Firebloom
            {8845, 175},    //Ghost Mushroom
            {8846, 175},    //Gromsblood
            {8838, 175},    //Sungrass
            {13463, 225},   //Dreamfoil
            {13464, 225},   //Golden Sansam
            {13467, 225},   //Icecap
            {13465, 225},   //Mountain Silversage
            {13466, 225},   //Sorrowmoss
            {22790, 275},   //Ancient Lichen
            {22786, 275},   //Dreaming Glory
            {22785, 275},   //Felweed
            {22793, 275},   //Mana Thistle
            {22791, 275},   //Netherbloom
            {22792, 275},   //Nightmare Vine
            {22787, 275},   //Ragveil
            {22789, 275},   //Terocone
            {36903, 325},   //Adder's Tongue
            {37921, 325},   //Deadnettle
            {39970, 325},   //Fire Leaf
            {36901, 325},   //Goldclover
            {36906, 325},   //Icethorn
            {36905, 325},   //Lichbloom
            {36907, 325},   //Talandra's Rose
            {36904, 325},   //Tiger Lily
            {52985, 450},   //Azshara's Veil
            {52983, 375},   //Cinderbloom
            {52986, 375},   //Heartblossom
            {52984, 375},   //Stormvine
            {52987, 450},   //Twilight Jasmine
            {52988, 475},   //Whiptail
        };
        #endregion

        #region Disenchant Info
        // format [skillLevel,max iLevel]
        readonly static int[,] UncommonItemDeList = new[,] {
            {1,20},
            {25,25},
            {50,30},
            {75,35},
            {100,40},
            {125,45},
            {150,50},
            {175,55},
            {200,60},
            {225,99},
            {275,120},
            {325,150},
            {350,182},
            {425,333}
                                                           };
        readonly static int[,] RareItemDeList = new[,] {
            {1,20},
            {25,25},
            {50,30},
            {75,35},
            {100,40},
            {125,45},
            {150,50},
            {175,55},
            {200,60},
            {225,99},
            {275,120},
            {325,200},
            {450,  380}
                                                       };
        readonly static int[,] EpicItemDeList = new[,] {
            {1,20},
            {25,25},
            {50,30},
            {75,35},
            {100,40},
            {125,88},
            {225,164},
            {375,284},
            {475,372}
                                                       };
        #endregion

        public static bool CanMill(this WoWItem item)
        {
            // get Players Incription skill level.
            int inscriptionLevel = ObjectManager.Me.GetSkill(SkillLine.Inscription).CurrentValue;
            // returns true if item is found in the dictionary and player meets the level requirement
            return MillableHerbList.ContainsKey(item.Entry) && MillableHerbList[item.Entry] <= inscriptionLevel;
        }
        public static bool CanProspect(this WoWItem item)
        {
            // get Players Jewelcrafting skill level.
            int jcLevel = ObjectManager.Me.GetSkill(SkillLine.Jewelcrafting).CurrentValue;
            // returns true if item is found in the dictionary and player meets the level requirement
            return ProspectList.ContainsKey(item.Entry) && ProspectList[item.Entry] <= jcLevel;
        }

        public static bool CanDisenchant(this WoWItem item)
        {

            if (item.ItemInfo.StatsCount == 0 && item.ItemInfo.RandomPropertiesId == 0)
            {
                //Professionbuddy.Log("We cannot disenchant {0} found in bag {1} at slot {2} because it has no stats.",
                //    item.Name, item.BagIndex + 1, item.BagSlot + 1);
                return false;
            }
            int enchantingLevel = ObjectManager.Me.GetSkill(SkillLine.Enchanting).CurrentValue;
            int[,] deList = null;
            if (item.Quality == WoWItemQuality.Uncommon)
                deList = UncommonItemDeList;
            else if (item.Quality == WoWItemQuality.Rare)
                deList = RareItemDeList;
            else if (item.Quality == WoWItemQuality.Epic)
                deList = EpicItemDeList;
            // returns true if item is found in the dictionary and player meets the level requirement
            if (deList != null)
            {
                int x;
                int iLevel = item.ItemInfo.Level;
                for (x = 0; x < deList.Length / 2; x++)
                {
                    if (iLevel <= deList[x, 1])
                    {
                        Professionbuddy.Log("We can disenchant {0} found in bag {1} at slot {2}",
                            item.Name, item.BagIndex + 1, item.BagSlot + 1);
                        return enchantingLevel >= deList[x, 0];
                    }
                }
            }
            Professionbuddy.Log("We cannot disenchant {0} found in bag {1} at slot {2}",
                item.Name, item.BagIndex + 1, item.BagSlot + 1);
            return false;
        }
    }
}
