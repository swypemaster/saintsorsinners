﻿using TreeSharp;
using Styx.WoWInternals;
using System.Diagnostics;


namespace HighVoltz.Composites
{
    class StackItemsAction : PBAction
    {
        const string StackLua =
            "local items={} " +
            "local done = 1 " +
            "for bag = 0,4 do " +
               "for slot=1,GetContainerNumSlots(bag) do " +
                  "local id = GetContainerItemID(bag,slot) " +
                  "local _,c,l = GetContainerItemInfo(bag, slot) " +
                  "if id ~= nil then " +
                     "local n,_,_,_,_,_,_, maxStack = GetItemInfo(id) " +
                     "if c < maxStack then " +
                        "if items[id] == nil then " +
                           "items[id] = {left=maxStack-c,bag=bag,slot=slot,locked = l or 0} " +
                        "else " +
                           "if items[id].locked == 0 then " +
                              "PickupContainerItem(bag, slot) " +
                              "PickupContainerItem(items[id].bag, items[id].slot) " +
                              "items[id] = nil " +
                           "else " +
                              "items[id] = {left=maxStack-c,bag=bag,slot=slot,locked = l or 0} " +
                           "end " +
                           "done = 0 " +
                        "end " +
                    "end " +
                  "end " +
               "end " +
            "end " +
            "return done ";

        readonly Stopwatch _throttleSW = new Stopwatch();
        protected override RunStatus Run(object context)
        {
            if (!IsDone)
            {
                if (!_throttleSW.IsRunning || _throttleSW.ElapsedMilliseconds > 500)
                {
                    _throttleSW.Reset();
                    _throttleSW.Start();
                    IsDone = Lua.GetReturnVal<int>(StackLua, 0) == 1;
                }
                if (!IsDone)
                    return RunStatus.Success;
            }
            return RunStatus.Failure;
        }
        public override string Name { get { return "Stack Items"; } }
        public override string Title
        {
            get
            {
                return string.Format("{0}", Name);
            }
        }
        public override string Help
        {
            get
            {
                return "This action will stack up items in player's bags. Use this before Disenchanting/Milling/Prospecting";
            }
        }
        public override object Clone()
        {
            return new StackItemsAction();
        }
    }
}
