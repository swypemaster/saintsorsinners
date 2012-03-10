using System.Collections.Generic;
using System.IO;
using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Profiles;
using Styx.Logic.Questing;
using System;

namespace Styx.Bot.Quest_Behaviors
{
    class LoadProfile : CustomForcedBehavior
    {
        public LoadProfile(Dictionary<string, string> args)
            : base(args)
        {
			Profile = GetAttributeAsString("Profile", false, null) ?? "";
			Remember = GetAttributeAsBoolean("Remember", false, null) ?? false;
        }
		private string Profile { get; set; }
        private bool Remember { get; set; }
		private bool _IsDone = false;

        public override void OnStart()
        {
            Logging.Write("Loading profile: {0} - {1}", Path.Combine(Path.GetDirectoryName(ProfileManager.XmlLocation), Profile), Remember);
            ProfileManager.LoadNew(Path.Combine(Path.GetDirectoryName(ProfileManager.XmlLocation), Profile), Remember);
            _IsDone = true;
        }

        public override bool IsDone
        {
            get { return _IsDone; }
        }
    }
}
