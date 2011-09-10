// BookCollector by HighVoltz
// If you use this code in your project pleace give credit

using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Xml.Linq;

using Styx;
using Styx.Plugins.PluginClass;
using Styx.Logic.BehaviorTree;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.Logic.Pathing;
using Styx.Logic.Combat;
using Styx.WoWInternals.WoWObjects;
using Styx.Logic.Inventory.Frames.Quest;
using Styx.Logic.Questing;
using Styx.Plugins;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Common;
using Styx.Logic.Inventory.Frames.Merchant;
using Styx.Logic;
using Styx.Logic.Profiles;

namespace HighVoltz
{
    class BookCollector : HBPlugin
    {
        public override string Name { get { return "AutoBookCollector"; } }
        public override string Author { get { return "HighVoltz"; } }
        private readonly Version _version = new Version(1, 4);
        public override Version Version { get { return _version; } }
        public override string ButtonText { get { return "AutoBookCollector"; } }
        public override bool WantButton { get { return false; } }
        public static LocalPlayer Me = ObjectManager.Me;
        private static HigherLearningStruct bookStatus;
        public BookCollector()
        {

        }

        struct HigherLearningStruct
        {
            public bool IsComplete;
            public bool Introduction;
            public bool Abjuration;
            public bool Conjuration;
            public bool Divination;
            public bool Enchantment;
            public bool Illusion;
            public bool Necromancy;
            public bool Transmutation;

            public void setCriteria(int index, bool value)
            {
                switch (index)
                {
                    case 0:
                        Introduction = value;
                        break;
                    case 1:
                        Abjuration = value;
                        break;
                    case 2:
                        Conjuration = value;
                        break;
                    case 3:
                        Divination = value;
                        break;
                    case 4:
                        Enchantment = value;
                        break;
                    case 5:
                        Illusion = value;
                        break;
                    case 6:
                        Necromancy = value;
                        break;
                    case 7:
                        Transmutation = value;
                        break;
                }
            }
        }

        public override void OnButtonPress()
        {
        }

        public override void Pulse()
        {
            try
            {
                if (bookStatus.IsComplete)
                {
                    Slog("Higher Learning achievement done, Stopping bot");
                    TreeRoot.Stop();
                }
                UpdateBookStatus();
                findAndReadBook();
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                Slog("Exception in Pulse:{0}", e);
            }
        }

        public static void movetoLoc(WoWPoint loc)
        {
            Mount.MountUp();
            while (loc.Distance(Me.Location) > 4)
            {
                Navigator.MoveTo(loc);
                Thread.Sleep(100);
                if (inCombat) return;
            }
            Thread.Sleep(2000);
        }

        static public void findAndReadBook()
        {
            ObjectManager.Update();
            List<WoWGameObject> objList = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o =>
                    !Blacklist.Contains(o.Guid) && (
                    (o.Entry == 192708 && !bookStatus.Introduction) ||
                    (o.Entry == 192709 && !bookStatus.Abjuration) ||
                    (o.Entry == 192710 && !bookStatus.Conjuration) ||
                    (o.Entry == 192711 && !bookStatus.Divination) ||
                    (o.Entry == 192713 && !bookStatus.Enchantment) ||
                    (o.Entry == 192865 && !bookStatus.Illusion) ||
                    (o.Entry == 192866 && !bookStatus.Necromancy) ||
                    (o.Entry == 192867 && !bookStatus.Transmutation) ||
                    o.Entry == 192651 ||
                    o.Entry == 192871 ||
                    o.Entry == 192888 ||
                    o.Entry == 192887 ||
                    o.Entry == 192890 ||
                    o.Entry == 192874 ||
                    o.Entry == 192706 ||
                    o.Entry == 192881 ||
                    o.Entry == 192652 ||
                    o.Entry == 192895 ||
                    o.Entry == 192889 ||
                    o.Entry == 192882 ||
                    o.Entry == 192884 ||
                    o.Entry == 192870 ||
                    o.Entry == 192905 ||
                    o.Entry == 192883 ||
                    o.Entry == 192886 ||
                    o.Entry == 192869 ||
                    o.Entry == 192894 ||
                    o.Entry == 192872 ||
                    o.Entry == 192868 ||
                    o.Entry == 192710 ||
                    o.Entry == 192707)
                    )
                .OrderBy(o => o.Distance).ToList();
            foreach (WoWGameObject o in objList)
            {
                Slog("Moving towards book:" + o.Name);
                movetoLoc(o.Location);
                if (inCombat) return;
                o.Interact();
                Thread.Sleep(1000);
                while (HasNextPage)
                {
                    Lua.DoString("ItemTextNextPage()");
                    Thread.Sleep(1000);
                }
                Slog("Finshed reading: {0}, Blacklisting for 5 min",o.Name);
                Blacklist.Add(o.Guid,new TimeSpan(0,5,0));
            }
        }

        static public void UpdateBookStatus()
        {
            int truecount = 0;
            for (int i = 1; i <= 8; i++)
            {
                bool b = Lua.LuaGetReturnValue("return GetAchievementCriteriaInfo(1956, " + i.ToString() + ")", "books.lua")[3] == "1";
                bookStatus.setCriteria(i - 1, b);
                if (b) truecount++;
            }// gheto way to check if achievement is done. 
            if (truecount == 8) bookStatus.IsComplete = true;
        }

        static public void Slog(string msg, params object[] args) { Logging.Write(msg, args); }

        static public void Slog(System.Drawing.Color c, string msg, params object[] args) { Logging.Write(c, msg, args); }

        static public bool HasNextPage { get { return Lua.LuaGetReturnValue("return ItemTextHasNextPage()", "books.lua")[0] == "1"; } }

        static public bool inCombat
        {
            get
            {
                if (Me.Combat || Me.Dead || Me.IsGhost) return true;
                return false;
            }
        }
    }
}
