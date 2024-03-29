﻿// Behavior originally contributed by Bobby53.
//
// DOCUMENTATION:
//     
//
using System;
using System.Collections.Generic;
using System.Threading;

using Styx.Logic.BehaviorTree;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;
using Action = TreeSharp.Action;


namespace Styx.Bot.Quest_Behaviors
{
    public class GoThruPortal : CustomForcedBehavior
    {
        /// <summary>
        /// Supports walk through portals in a way that does not result
        /// in red error messages in WoW or in the HB log/debug files.
        /// 
        /// xyz should be a position as close as possible to portal entrance 
        /// without entering.
        /// 
        /// ##Syntax##
        /// [Optional] QuestId: The id of the quest (0 is default)
        /// [Optional] QuestName: The name of the quest.
        /// [Optional] Timeout: time in milliseconds it allows for completing (10000 is default)
        /// X,Y,Z: used with current location to create a vector it moves along
        /// 
        /// ##Example##
        /// use RunTo to get start position, then GoThruPortal to run throuhg xyz vector
        /// on way through portal.
        /// 
        ///     <RunTo X="4646.201" Y="-3685.043" Z="954.2496" />
        ///     <CustomBehavior File="GoThruPortal" X="4656.928" Y="-3685.472" Z="957.185" />
        /// 
        /// </summary>
       public GoThruPortal(Dictionary<string, string> args)
            : base(args)
        {
			try
			{
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                MovePoint   = GetXYZAttributeAsWoWPoint("", true, null) ?? WoWPoint.Empty;
                QuestId     = GetAttributeAsQuestId("QuestId", false, null) ?? 0;
                QuestRequirementComplete = GetAttributeAsEnum<QuestCompleteRequirement>("QuestCompleteRequirement", false, null) ?? QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog    = GetAttributeAsEnum<QuestInLogRequirement>("QuestInLogRequirement", false, null) ?? QuestInLogRequirement.InLog;
                Timeout     = GetAttributeAsInteger("Timeout", false, 1, 60000, null) ?? 10000;

                GetAttributeAsString_NonEmpty("QuestName", false, null);    // not used - documentation purposes only

                Timeout += System.Environment.TickCount;
                MovePoint   = WoWMovement.CalculatePointFrom(MovePoint, -15);
                ZoneText    = StyxWoW.Me.ZoneText;
			}

			catch (Exception except)
			{
				// Maintenance problems occur for a number of reasons.  The primary two are...
				// * Changes were made to the behavior, and boundary conditions weren't properly tested.
				// * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
				// In any case, we pinpoint the source of the problem area here, and hopefully it
				// can be quickly resolved.
				UtilLogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
										+ "\nFROM HERE:\n"
										+ except.StackTrace + "\n");
				IsAttributeProblem = true;
			}
        }


        // Attributes provided by caller
        public WoWPoint                 MovePoint { get; private set; }
        public int                      QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement    QuestRequirementInLog { get; private set; }
        public int                      Timeout { get; private set; }

        // Private variables for internal state
        private bool                _isBehaviorDone;
        private bool                _isInPortal;
        private Composite           _root;

        // Private properties
        private LocalPlayer         Me { get { return (ObjectManager.Me); } }
        private string              ZoneText { get; set; }



        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(

                    // first state catches if we are done just in case
                    new Decorator(ret => _isBehaviorDone,
                        new Action(delegate
                        {
                            return RunStatus.Success;
                        })),

                    // if we hit the load screen and we are back in game
                    new Decorator(ret => _isInPortal && ObjectManager.IsInGame && StyxWoW.Me != null,
                        new Action(delegate
                        {
                            _isBehaviorDone = true;
                            UtilLogMessage("info", "Went thru portal.");
                            Thread.Sleep(500);
                            WoWMovement.MoveStop();
                            Thread.Sleep(500);
                            return RunStatus.Success;
                        })),

                    // if zone name changed
                    new Decorator(ret => ZoneText != StyxWoW.Me.ZoneText,
                        new Action(ret => _isInPortal = true)),

                    // if load screen is visible
                    new Decorator(ret => !ObjectManager.IsInGame || StyxWoW.Me == null,
                        new Action(ret => _isInPortal = true)),

                    // if we are within 2 yards of calculated end point we should never reach
                    new Decorator(ret => MovePoint.Distance(Me.Location) < 2,
                        new Action(delegate
                        {
                            _isBehaviorDone = true;
                            WoWMovement.MoveStop();
                            UtilLogMessage("fatal", "Unable to reach end point.  Failed to go through portal.");
                            return RunStatus.Success;
                        })),

                    new Decorator(ret => Timeout != 0 && Timeout < System.Environment.TickCount,
                        new Action(delegate
                        {
                            _isBehaviorDone = true;
                            WoWMovement.MoveStop();
                            UtilLogMessage("fatal", "Timed out after {0} ms.  Failed to go through portal", Timeout);
                            return RunStatus.Success;
                        })),

                    new Decorator(ret => !StyxWoW.Me.IsMoving,
                        new Action(delegate
                        {
                            UtilLogMessage("info", "Moving to {0}", MovePoint);
                            WoWMovement.ClickToMove(MovePoint);
                            return RunStatus.Success;
                        }))
                    )
                );

        }


        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone     // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }


        public override void    OnStart()
		{
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                TreeRoot.GoalText = "Moving through Portal";
			}
		}

   		#endregion
    }
}

