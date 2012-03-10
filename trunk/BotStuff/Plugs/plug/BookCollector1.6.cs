// BookCollector by HighVoltz
// If you use this code in your project pleace give credit

using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

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
using Styx.Logic.AreaManagement;

namespace HighVoltz
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.minDelayText = new System.Windows.Forms.TextBox();
            this.maxDelayText = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // minDelayText
            // 
            this.minDelayText.Location = new System.Drawing.Point(8, 46);
            this.minDelayText.Name = "minDelayText";
            this.minDelayText.Size = new System.Drawing.Size(57, 20);
            this.minDelayText.TabIndex = 0;
            this.minDelayText.TextChanged += new System.EventHandler(this.minDelayText_TextChanged);
            // 
            // maxDelayText
            // 
            this.maxDelayText.Location = new System.Drawing.Point(127, 46);
            this.maxDelayText.Name = "maxDelayText";
            this.maxDelayText.Size = new System.Drawing.Size(57, 20);
            this.maxDelayText.TabIndex = 1;
            this.maxDelayText.TextChanged += new System.EventHandler(this.maxDelayText_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(-1, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 13);
            this.label1.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Min Time";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(124, 30);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Max Time";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.minDelayText);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.maxDelayText);
            this.groupBox1.Location = new System.Drawing.Point(4, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(240, 76);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Random delay in miliseconds at each Hotspot";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(251, 85);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "MainForm";
            this.Text = "Configure BookCollector";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.TextBox minDelayText;
        private System.Windows.Forms.TextBox maxDelayText;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox1;
    }

    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void minDelayText_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.MinDelayTime = int.Parse(minDelayText.Text);
            Settings.Default.Save();
        }

        private void maxDelayText_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.MaxDelayTime = int.Parse(maxDelayText.Text);
            Settings.Default.Save();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            minDelayText.Text = Settings.Default.MinDelayTime.ToString();
            maxDelayText.Text = Settings.Default.MaxDelayTime.ToString();
        }
    }
}

namespace HighVoltz
{
    class BookCollector : HBPlugin
    {
        public override string Name { get { return "BookCollector"; } }
        public override string Author { get { return "HighVoltz"; } }
        public override Version Version { get { return new Version(1, 5); } }
        public override string ButtonText { get { return "BookCollector"; } }
        public override bool WantButton { get { return true; } }
        public static MainForm GUI = new MainForm();
        public static LocalPlayer Me = ObjectManager.Me;
        private static HigherLearningStruct bookStatus;
        static GrindArea grindArea;
        static List<WoWPoint> HotspotList;
        static int HotspotIndex;
        public static WoWPoint CurrentHotspot;
        Random rnd = new Random(Environment.TickCount); 
        
                   
        public BookCollector()
        {
            Styx.BotEvents.OnBotStart += Init;
            if (TreeRoot.IsRunning)
                Init(new System.EventArgs());
        }

        public void Init(System.EventArgs args)
        {
            if (StyxSettings.Instance.EnabledPlugins.Contains(Name))
            {
                Logging.Write("BookCollector Initializing");
                grindArea = ProfileManager.CurrentProfile.GrindArea;
                HotspotList = grindArea.Hotspots.ConvertAll<WoWPoint>(hs => hs.ToWoWPoint());
                HotspotIndex = 0;
                CurrentHotspot = grindArea.CurrentHotSpot;
            }
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
            GUI.Show();
        }

        public override void Pulse()
        {
            try
            {
                PluginContainer myPlugin = PluginManager.Plugins.Where(p=>p.Plugin.Name == Name).First();
                while (!inCombat && myPlugin.Enabled && TreeRoot.IsRunning)
                {
                    if (bookStatus.IsComplete)
                    {
                        Slog("Higher Learning achievement done, Stopping bot");
                        TreeRoot.Stop();
                        return;
                    }
                    UpdateBookStatus();
                    findAndReadBook();
                    if (Me.Location.Distance(CurrentHotspot) < 3)
                    {
                        randomWait(rnd.Next(Settings.Default.MinDelayTime, Settings.Default.MaxDelayTime));
                        CurrentHotspot = NextHotspot;
                    }
                    if (Me.Location.Distance(CurrentHotspot) > 40)  Mount.MountUp();
                    Navigator.MoveTo(CurrentHotspot);
                    // pulse other plugins since we're in a loop
                    List<PluginContainer> _pluginList = PluginManager.Plugins;
                    foreach (PluginContainer _plugin in _pluginList)
                    {
                        if (_plugin.Enabled && _plugin.Plugin.Name != Name)
                            _plugin.Plugin.Pulse();
                    }
                }
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
                    o.Entry == 192891 ||
                    o.Entry == 192896 ||
                    o.Entry == 192880 ||
                    o.Entry == 192653 ||
                    o.Entry == 192885 ||
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

        public static void randomWait (int wait)
        {
            int recordedTime = Environment.TickCount;
            while (Environment.TickCount - recordedTime < wait)
            {
                Thread.Sleep(100);
            }
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

        #region hotspot

        static public WoWPoint NextHotspot
        {
            get
            {
                {
                    if (ProfileManager.CurrentProfile.GrindArea.RandomizeHotspots)
                        return ProfileManager.CurrentProfile.GrindArea.GetRandomHotspot();
                    else
                    {
                        HotspotIndex = HotspotIndex == HotspotList.Count - 1 ? 0 : HotspotIndex + 1;
                        return HotspotList[HotspotIndex];
                    }
                }
            }
        }


        static public WoWPoint getNextHotspot
        {
            get
            {
                {
                    if (ProfileManager.CurrentProfile.GrindArea.RandomizeHotspots)
                        return ProfileManager.CurrentProfile.GrindArea.GetRandomHotspot();
                    else
                    {
                        HotspotIndex = HotspotIndex == HotspotList.Count - 1 ? 0 : HotspotIndex + 1;
                        return HotspotList[HotspotIndex];
                    }
                }
            }
        }

        #endregion 
    }
}

namespace HighVoltz
{
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {

        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2000")]
        public int MinDelayTime
        {
            get
            {
                return ((int)(this["MinDelayTime"]));
            }
            set
            {
                this["MinDelayTime"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("7000")]
        public int MaxDelayTime
        {
            get
            {
                return ((int)(this["MaxDelayTime"]));
            }
            set
            {
                this["MaxDelayTime"] = value;
            }
        }
    }
}