using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using Styx.Logic.BehaviorTree;
using Styx.Logic.Profiles.Quest;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.Database;
using Styx.Helpers;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Styx.Bot.Quest_Behaviors
{
    public class WaitForChatEvent : CustomForcedBehavior
    {
        public WaitForChatEvent(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
                ChatMsg = GetAttributeAsString("ChatMsg", true, null) ?? "";
				Objective = GetAttributeAsInteger("Objective", false, 0, int.MaxValue, null) ?? 0;
				QuestId = (uint?)GetAttributeAsQuestId("QuestId", false, null) ?? 0;
			}

			catch (Exception except)
			{
				UtilLogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
										+ "\nFROM HERE:\n"
										+ except.StackTrace + "\n");
				IsAttributeProblem = true;
			}
        }

        public string           ChatMsg { get; private set; }
		public uint 			QuestId { get; private set; }
		public int              Objective { get; private set; }
		private bool            isBehaviorDone;
        private Composite       _root;
        public bool objDone { get { return Lua.GetReturnVal<int>(string.Format("a,b,c=GetQuestLogLeaderBoard({0},GetQuestLogIndexByID({1}));if c==1 then return 1 else return 0 end", Objective , QuestId), 0) == 1; } }



        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
			return _root ?? (_root =
				new PrioritySelector(
					new Decorator(ret => Objective != 0 && QuestId != 0 && objDone,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                                new WaitContinue(120,
                                    new Action(delegate
                                    {
										Lua.Events.DetachEvent("CHAT_MSG_RAID_BOSS_WHISPER", HandleRBW);
										Lua.Events.DetachEvent("CHAT_MSG_MONSTER_EMOTE", HandleME);
										Lua.Events.DetachEvent("CHAT_MSG_MONSTER_SAY", HandleMS);
										Lua.Events.DetachEvent("CHAT_MSG_MONSTER_YELL", HandleMY);
										Lua.Events.DetachEvent("CHAT_MSG_RAID_BOSS_EMOTE", HandleRBE);
                                        isBehaviorDone = true;
                                        return RunStatus.Success;
                                    }
								)
							)
                        )
					)
				)
			);
        }


        
		private void HandleRBW(object sender, LuaEventArgs args)
        {
			Logging.Write(""+args.Args[0]);
			if (args.Args[0].ToString() == ChatMsg)
			{
				isBehaviorDone = true;
			}
        }
		private void HandleRBE(object sender, LuaEventArgs args)
        {
			Logging.Write(""+args.Args[0]);
			if (args.Args[0].ToString() == ChatMsg)
			{
				isBehaviorDone = true;
			}
        }
		private void HandleMS(object sender, LuaEventArgs args)
        {
			Logging.Write(""+args.Args[0]);
			if (args.Args[0].ToString() == ChatMsg)
			{
				isBehaviorDone = true;
			}
		}
		private void HandleMY(object sender, LuaEventArgs args)
        {
			Logging.Write(""+args.Args[0]);
			if (args.Args[0].ToString() == ChatMsg)
			{
				isBehaviorDone = true;
			}
		}
		private void HandleME(object sender, LuaEventArgs args)
        {
			Logging.Write(""+args.Args[0]);
			if (args.Args[0].ToString() == ChatMsg)
			{
				isBehaviorDone = true;
			}
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
			TreeRoot.GoalText = "Waiting for a chat event";
            if (!IsDone)
            {
				Lua.Events.AttachEvent("CHAT_MSG_RAID_BOSS_WHISPER", HandleRBW);
				Lua.Events.AttachEvent("CHAT_MSG_MONSTER_EMOTE", HandleME);
				Lua.Events.AttachEvent("CHAT_MSG_MONSTER_SAY", HandleMS);
				Lua.Events.AttachEvent("CHAT_MSG_MONSTER_YELL", HandleMY);
				Lua.Events.AttachEvent("CHAT_MSG_RAID_BOSS_EMOTE", HandleRBE);
            }
        }

        #endregion
		public override bool IsDone
        {
            get
            {
                return (isBehaviorDone);
            }
        }
    }
}
