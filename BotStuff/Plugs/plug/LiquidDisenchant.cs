namespace PluginLiquidDisenchant2
{
    using Styx;
    using Styx.Combat.CombatRoutine;
    using Styx.Helpers;
    using Styx.Logic;
    using Styx.Logic.Combat;
	using Styx.Logic.Inventory;
	using Styx.Logic.Inventory.Frames.Gossip;
    using Styx.Logic.Pathing;
	using Styx.Logic.Profiles;
	using Styx.Plugins.PluginClass;
    using Styx.WoWInternals;
    using Styx.WoWInternals.WoWObjects;

    using System;	
    using System.Collections.Generic;
    using System.Drawing;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using System.Xml.Linq;



	//Special Thanks to CarlMGregory for making it work better
	//Special Thanks to Hawker for the pointer to the right API change
    public class LiquidDisenchant2 : HBPlugin
    {
        public override string Name { get { return "Liquid Disenchant 2.5"; } }
        public override string Author { get { return "LiquidAtoR + CarlMGregory"; } }
        public override Version Version { get { return _version; } }
        private readonly Version _version = new Version(2, 5, 0, 1);
        public override bool WantButton { get { return true; } }
        public override string ButtonText { get { return "Disenchant"; } }
        private Thread deThread = null;

		#region When NOT to disenchant
		
        public override void Pulse()
        {
		//Thanks FPSWare for his plugins for Hunter and Druid, code from there,
		//Don't start disenchanting when the following things occur,

            if (!SpellManager.HasSpell("Disenchant") || Battlegrounds.IsInsideBattleground || ObjectManager.Me.Mounted || ObjectManager.Me.Combat) 
			{ 
			return; 
			}
            DisenchantItems();
        }

		//Revised by CarlMGregory
        public override void OnButtonPress()
		{
			for (int a = alreadyDisenchanted.Count-1; a >= 0; a--)
				{
					alreadyDisenchanted.RemoveAt(a);
				}
            DisenchantItems();
        }
		#endregion
		
        #region Disenchanting
		//Shamelessly ripped from Hawker's Demonlock code,
		//I had to dig deep into my archives for this lock's CC,
		//But I knew the disenchant code was in there.
		
        private static List<WoWItem> alreadyDisenchanted = new List<WoWItem>();

        private List<uint> ignoreItems = new List<uint>() {
            // mats
            10940, 10938, 10939, 10998, 10978, 11082, 11083, 11084, 11134, 11139, 11174, 11175,
            11176, 11177, 11178, 14343, 14344, 16202, 16203, 16204, 20725, 22445, 22446, 22447,
            22448, 22449, 22450, 34052, 34053, 34054, 34055, 34056, 34057, 46849, 49649, 52718,
			52719,

            // rods
            44452,

            // lockbox
            43622,

            // healthstones
            5509, 5510, 5511, 5512, 9421, 19004, 19005, 19006,  
            19007, 19008, 19009, 19010, 19011, 19012, 19013, 
            22103, 22104, 22105, 36889, 36890, 36891, 36892,
            36893, 36894,

            // soulstones
			5232, 16892, 16893, 16895, 16896, 22116, 36895,

            // firestones
			40773, 41169, 41170, 41171, 41172, 41173, 41174,

            // spellstones
            41191, 41192, 41193, 41194, 41195, 41196,
        };

        private void DisenchantItems()
        {
            List<WoWItem> targetItems = ObjectManager.GetObjectsOfType<WoWItem>(false);
			
            for (int a = targetItems.Count-1; a >= 0; a--)
            {
                    if (ignoreItems.Contains(targetItems[a].Entry) || alreadyDisenchanted.Contains(targetItems[a]))
                    {
                        targetItems.RemoveAt(a);
                    }
                    else if (targetItems[a].IsSoulbound || targetItems[a].IsAccountBound || ignoreItems.Contains(targetItems[a].Entry) || targetItems[a].Quality != WoWItemQuality.Uncommon)
					{
						alreadyDisenchanted.Add(targetItems[a]);
						targetItems.RemoveAt(a);
					}
            }

                if (Equals(null, targetItems)) { return; }

                foreach (WoWItem deItem in targetItems)
                {
					if(deItem.BagSlot != -1)
					{
						Styx.Helpers.Logging.Write(Color.FromName("DarkRed"), "[LiquidDisenchant]: {0} (Entry:{1}).", deItem.Name, deItem.Entry);
						Lua.DoString("CastSpellByName(\"Disenchant\")");
						Thread.Sleep(500);
						Lua.DoString("UseItemByName(\"" + deItem.Name + "\")");
						Thread.Sleep(500);

                    while (ObjectManager.Me.IsCasting)
						{
							Thread.Sleep(250);
						}

                    Thread.Sleep(2500);

                    // wait for lootframe to close
                    Stopwatch timer = new Stopwatch();
                    timer.Start();

                    while (Styx.Logic.LootTargeting.LootFrameIsOpen)
						{
							Thread.Sleep(250);

							if (timer.ElapsedMilliseconds >= 6000)
							{
								break;
							}
						}
					
						if (!ObjectManager.Me.Combat)
						{
							Thread.Sleep(1500);
							alreadyDisenchanted.Add(deItem);
						}
					}
                }
            return;
        }
        #endregion
    }
}