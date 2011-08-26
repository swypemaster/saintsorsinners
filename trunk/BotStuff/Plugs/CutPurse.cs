/*
 * CutPurse by Kamilche
 * 
 * This is an add-on that will:
 * 
 *   1. Open containers containing items, such as clams.
 *   2. Combine scraps of borean leather, motes of air, etc.
 *   3. Substitute foods when your preferred food runs out.
 *
 * If you're a rogue, it will also:
 * 
 *   4. Open junkboxes/lockboxes and extract their contents.
 *   5. Poison both your weapons as necessary.
 *   
 * If you're a rogue, get the glyph that makes 'Pick Lock' take
 * no casting time. It goes much faster that way, and that's what
 * this code was tested with.
 *   
 * 2010/11/12   v1.0.0.0 - First version.
 * 
 */
namespace CutPurse
{
    using Styx;
    using Styx.Combat.CombatRoutine;
    using Styx.Helpers;
    using Styx.Logic;
    using Styx.Logic.Combat;
    using Styx.Logic.Pathing;
    using Styx.Logic.Inventory;
    using Styx.Plugins.PluginClass;
    using Styx.WoWInternals;
    using Styx.WoWInternals.WoWObjects;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Windows.Forms;

    public class CutPurse : HBPlugin
    {
        public override string Name { get { return "CutPurse"; } }
        public override string Author { get { return "Kamilche"; } }
        public override Version Version { get { return _version; } }
        private readonly Version _version = new Version(1, 0, 0, 0);
        private string[] mainhandpoisons = { "Crippling Poison", "Deadly Poison", "Wound Poison", "Instant Poison", "Mind-numbing Poison", "Bloodboil Poison" };
        private string[] offhandpoisons = { "Wound Poison", "Deadly Poison", "Instant Poison", "Crippling Poison", "Mind-numbing Poison", "Bloodboil Poison" };


        private static uint[] _lockboxes = new uint[] {
            16882,  // Battered Junkbox
            63349,  // Flame-Scarred Junkbox
            16885,  // Heavy Junkbox
            8249,   // Junkboxes Needed
            43575,  // Reinforced Junkbox
            29569,  // Strong Junkbox
            16884,  // Sturdy Junkbox
            16883,  // Worn Junkbox
            30454,  // Ar'tor's Lockbox
            30646,  // Borak's Lockbox
            5760,   // Eternium Lockbox
            43622,  // Froststeel Lockbox
            7870,   // Gallywix's Lockbox
            30429,  // Grom'tor's Lockbox
            4633,   // Heavy Bronze Lockbox
            4634,   // Iron Lockbox
            31952,  // Khorium Lockbox
            121264, // Lucius's Lockbox
            12192,  // Mist Veil's Lockbox
            175166, // Mist Veil's Lockbox
            5758,   // Mithril Lockbox
            19425,  // Mysterious Lockbox
            4632,   // Ornate Bronze Lockbox
            178244, // Practice Lockbox
            4638,   // Reinforced Steel Lockbox
            187875, // Salrand's Lockbox
            12191,  // Silver Dawning's Lockbox
            175165, // Silver Dawning's Lockbox
            175802, // Small Lockbox
            141980, // Spectral Lockbox
            4637,   // Steel Lockbox
            177790, // Strange Lockbox
            4636,   // Strong Iron Lockbox
            42288,  // Summon Salvaged Lockbox
            5759,   // Thorium Lockbox
            45986,  // Tiny Titanium Lockbox
            43624,  // Titanium Lockbox
            2718,   // Trelane's Lockbox
        };

        private static uint[,] _data = new uint[,] { 
            {1, 5523},   // Small Barnacled Clam
            {1, 5524},   // Thick-shelled Clam
            {1, 7973},   // Big-mouth Clam
            {1, 24476},  // Jaggal Clam
            {5, 33567},  // Borean Leather Scraps
            {1, 36781},  // Darkwater Clam
            {1, 44700},  // Brooding Darkwater Clam
            {1, 45909},  // Giant Darkwater Clam
            {10, 37700}, // Crystallized Air
            {10, 37701}, // Crystallized Earth
            {10, 37702}, // Crystallized Fire
            {10, 37703}, // Crystallized Shadow
            {10, 37704}, // Crystallized Life
            {10, 37705}, // Crystallized Water
            {10, 22572}, // Mote of Air
            {10, 22573}, // Mote of Earth 
            {10, 22574}, // Mote of Fire
            {10, 22575}, // Mote of Life
            {10, 22576}, // Mote of Mana
            {10, 22577}, // Mote of Shadow
            {10, 22578}, // Mote of Water
            {1, 3352},   // Ooze-covered Bag
            {1, 20768},  // Oozing Bag
            {1, 20767},  // Scum Covered Bag
            {1, 44663},  // Abandoned Adventurer's Satchel
            {1, 27511},  // Inscribed Scrollcase
            {1, 20708},  // Tightly Sealed Trunk
            {1, 6351},   // Dented Crate
            {1, 6352},   // Waterlogged Crate
            {1, 6353},   // Small Chest
            {1, 6356},   // Battered Chest
            {1, 6357},   // Sealed Crate
            {1, 13874},  // Heavy Crate
            {1, 21113},  // Watertight Trunk
            {1, 21150},  // Iron Bound Trunk
            {1, 21228},  // Mithril Bound Trunk
            {1, 27513},  // Curious Crate
            {1, 27481},  // Heavy Supply Crate
            {1, 44475},  // Reinforced Crate
	    {5, 52977},  // Savage Leather Scraps
	    {1, 67495},  // Strange Bloated Stomach
        };

        private static Stopwatch sw = new Stopwatch();

        private void log(String fmt, params object[] args)
        {
            String s = String.Format(fmt, args);
            log(Color.DarkBlue, fmt, args);
        }

        private void log(Color color, String fmt, params object[] args)
        {
            String s = String.Format(fmt, args);
            Styx.Helpers.Logging.Write(color, String.Format("[{0}]: {1}", Name, s));
        }


        public override void Pulse()
        {
            if (!sw.IsRunning)
            {
                sw.Start();
                log("Active");
            }

            if (ObjectManager.Me.Combat)
            {

                // Dismount if you're still on a horse
                if (!ObjectManager.Me.IsMoving && ObjectManager.Me.Mounted)
                    Mount.Dismount();
            }

            // If it hasn't been 30 seconds, leave.
            if (sw.Elapsed.TotalSeconds < 30 || 
                Battlegrounds.IsInsideBattleground || 
                ObjectManager.Me.Mounted || 
                ObjectManager.Me.Combat ||
                ObjectManager.Me.Dead)
                return;

            // Find best food and drink in backpack
            WoWItem drink = Styx.Logic.Inventory.Consumable.GetBestDrink(false);
            WoWItem food = Styx.Logic.Inventory.Consumable.GetBestFood(false);
            if (drink != null)
                LevelbotSettings.Instance.DrinkName = drink.Name;
            if (food != null)
                LevelbotSettings.Instance.FoodName = food.Name;

            // Unlock and open items
            CheckInventoryItems();

            // Poison knives
            if (ObjectManager.Me.Class == WoWClass.Rogue)
                PoisonKnives();

            // Reset timer so it will all start over again in 5 seconds.
            sw.Reset();
            sw.Start();

        }

        private void CheckInventoryItems()
        {
            // Check all items in inventory
            foreach (WoWItem item in ObjectManager.GetObjectsOfType<WoWItem>())
            {
                // Check to see if it can be lockpicked
                if (SpellManager.HasSpell("Pick Lock") && _lockboxes.Contains(item.Entry))
                {
                    SpellManager.Cast("Pick Lock");
                    StyxWoW.SleepForLagDuration();
                    String s = String.Format("UseItemByName(\"{0}\")", item.Name);
                    log("Unlocking {0} guid {1}", item.Name, item.Guid);
                    Lua.DoString(s);
                    StyxWoW.SleepForLagDuration();
                    StyxWoW.SleepForLagDuration();
                    while (ObjectManager.Me.IsCasting)
                        Thread.Sleep(50);
                    log("Opening {0} guid {1}", item.Name, item.Guid);
                    Lua.DoString(s);
                    StyxWoW.SleepForLagDuration();
                }
                else
                {
                    // Check to see if it can be opened or used or combined.
                    for (int i = 0; i <= _data.GetUpperBound(0); i++)
                    {
                        if (_data[i, 1] == item.Entry)
                        {
                            int cnt = Convert.ToInt32(Lua.GetReturnValues(String.Format("return GetItemCount(\"{0}\")", item.Name), Name + ".lua")[0]);
                            int max = (int)(cnt / _data[i, 0]);
                            for (int j = 0; j < max; j++)
                            {
                                String s = String.Format("UseItemByName(\"{0}\")", item.Name);
                                log("Using {0} we have {1}", item.Name, cnt);
                                Lua.DoString(s);
                                StyxWoW.SleepForLagDuration();
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void PoisonKnives()
        {
            LocalPlayer Me = ObjectManager.Me;
            bool needpoison = false;
            WoWItem mainhand = Me.Inventory.Equipped.MainHand;
            WoWItem offhand = Me.Inventory.Equipped.OffHand;
            
            // If you're not at least level 10, you can't poison knives anyway.
            if (Me.Level < 10)
                return;

            // Leave early if neither weapon needs poison.
            if (mainhand != null && mainhand.TemporaryEnchantment.Id == 0)
                needpoison = true;
            if (offhand != null && offhand.TemporaryEnchantment.Id == 0)
                needpoison = true;
            if (needpoison == false)
                return;

            // Get the poisons available
            String mainhandpoison = FirstAvailableItem(mainhandpoisons);
            String offhandpoison = FirstAvailableItem(offhandpoisons);

            // Leave early if no poison is available
            if (mainhandpoison == null)
                log(Color.Red, "Out of main hand poison! Buy more.");
            if (offhandpoison == null)
                log(Color.Red, "Out of off hand poison! Buy more.");
            if (mainhandpoison == null && offhandpoison == null)
                return;
 
            // Stop moving
            WoWMovement.MoveStop();
            StyxWoW.SleepForLagDuration();

            // Get off horse
            if (Me.Mounted)
            {
                Mount.Dismount();
                StyxWoW.SleepForLagDuration();
            }

            // Apply poison to main weapon
            if (mainhandpoison != null)
                ApplyMainPoison(mainhandpoison);

            // Apply poison to offhand weapon
            if (offhandpoison != null)
                ApplyOffhandPoison(offhandpoison);
        }

        private string FirstAvailableItem(params String[] itemnames)
        {
            List<WoWItem> inventory = ObjectManager.GetObjectsOfType<WoWItem>(false);
            Dictionary<String, int> d = new Dictionary<string, int>();

            // Build list of items in inventory with their counts.
            foreach (WoWItem item in inventory)
            {
                int cnt = 0;
                if (d.ContainsKey(item.Name))
                    cnt = d[item.Name];
                cnt += Convert.ToInt32(Lua.GetReturnValues(String.Format("return GetItemCount(\"{0}\")", item.Entry), Name + ".lua")[0]);
                d[item.Name] = cnt;
            }

            // For each item in desired items, check to see if it's in inventory and has
            // a count > 0.
            foreach (String name in itemnames)
            {
                if (d.ContainsKey(name) && d[name] > 0)
                    return name;
            }

            return null;
        }

        private void ApplyMainPoison(string poison)
        {
            WoWItem item = ObjectManager.Me.Inventory.Equipped.MainHand;
            if (item != null && item.TemporaryEnchantment.Id == 0)
            {
                log("Applying poison {0} to main weapon {1}", poison, item.Name);
                Lua.DoString(String.Format("UseItemByName(\"{0}\")", poison));
                StyxWoW.SleepForLagDuration();
                Lua.DoString("UseInventoryItem(16)");
                Thread.Sleep(4000);
           }
        }

        private void ApplyOffhandPoison(string poison)
        {
            WoWItem item = ObjectManager.Me.Inventory.Equipped.OffHand;
            if (item != null && item.TemporaryEnchantment.Id == 0)
            {
                log("Applying poison {0} to offhand weapon {1}", poison, item.Name);
                Lua.DoString(String.Format("UseItemByName(\"{0}\")", poison));
                StyxWoW.SleepForLagDuration();
                Lua.DoString("UseInventoryItem(17)");
                Thread.Sleep(4000);
            }
        }

    }
}
