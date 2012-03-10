using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Styx;
using Styx.Logic;
using Styx.WoWInternals;
using System.Globalization;
using System.Threading;
using Styx.Logic.Pathing;
using Styx.Helpers;
using System.IO;
using System.Media;

namespace ZapGB2Recorder
{
    public partial class Form1 : Form
    {
        public static List<string> hotspotsList, mailboxList, vendorsList, blackspotsList;
        public Thread backgroundThread, recordingThread;
        public WoWPoint oldLocation = new WoWPoint(0, 0, 0);
        public bool isRecording = false;

        public Form1()
        {
            InitializeComponent();



            hotspotsList = new List<string>();
            mailboxList = new List<string>();
            vendorsList = new List<string>();
            blackspotsList = new List<string>();

            backgroundThread = new Thread(new ThreadStart(BackgroundPULSE));
            backgroundThread.IsBackground = true;
            backgroundThread.Start();


            LoadSettings();

            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            richTextBox1.TextChanged += new EventHandler(richTextBox1_TextChanged);
            onTop.CheckedChanged += new EventHandler(onTop_CheckedChanged);



            Log("Initializing completed");
        }

        public void onTop_CheckedChanged(object sender, EventArgs arg)
        {
            if (onTop.Checked == true)
            {
                this.TopMost = true;
            }
            else
            {
                this.TopMost = false;
            }
        }

        public void PlaySound(string fileName)
        {
            new SoundPlayer(Logging.ApplicationPath + "\\Plugins\\ZapGB2Recorder\\Sound\\" + fileName).Play();
        }

        public void richTextBox1_TextChanged(object sender, EventArgs arg)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
            richTextBox1.Refresh();
        }

        //Writes to the log
        public void Log(string msg)
        {
            richTextBox1.Text += "[" + DateTime.Now.ToLongTimeString() + "] - " + msg + Environment.NewLine;
        }

        //Saves the settings
        public void SaveSettings()
        {
            //Setting sell settings

            #region SellBlue

            if (sellBlue.Checked == true)
            {
                ZapGB2Recorder.Properties.Settings.Default.sellBlue = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.sellBlue = false;
            }

            #endregion

            #region SellGreen
            if (sellGreen.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.sellGreen = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.sellGreen = false;
            }
            #endregion

            #region SellGrey
            if (sellGrey.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.sellGrey = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.sellGrey = false;
            }
            #endregion

            #region SellPurple
            if (sellPurple.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.sellPurple = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.sellPurple = false;
            }
            #endregion

            #region SellWhite
            if (sellWhite.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.sellWhite = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.sellWhite = false;
            }
            #endregion

            //Setting mail settings

            #region MailBlue

            if (mailBlue.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.mailBlue = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.mailBlue = false;
            }

            #endregion

            #region MailGreen

            if (mailGreen.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.mailGreen = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.mailGreen = false;
            }

            #endregion

            #region MailGrey

            if (mailGrey.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.mailGrey = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.mailGrey = false;
            }

            #endregion

            #region MailPurple

            if (mailPurple.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.mailPurple = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.mailPurple = false;
            }

            #endregion

            #region MailWhite

            if (mailWhite.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.mailWhite = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.mailWhite = false;
            }

            #endregion

            //Setting misc settings

            #region PlaySound

            if (playSound.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.playSound = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.playSound = false;
            }

            #endregion

            #region OnTop

            if (onTop.Checked)
            {
                ZapGB2Recorder.Properties.Settings.Default.onTop = true;
            }
            else
            {
                ZapGB2Recorder.Properties.Settings.Default.onTop = false;
            }

            #endregion

            #region MinimumDurability

            ZapGB2Recorder.Properties.Settings.Default.minimumDurability = minDurabilityTextbox.Text;

            #endregion

            #region MinimumFreeBagSlots

            ZapGB2Recorder.Properties.Settings.Default.minimumFreeBagSlots = minBagSlotTextbox.Text;

            #endregion

            #region RecordingSleepTime

            ZapGB2Recorder.Properties.Settings.Default.recorderSleepTime = recordingTimeTextbox.Text;

            #endregion

            #region AddIfMoved

            ZapGB2Recorder.Properties.Settings.Default.addIfMoved = addIfMovedTextbox.Text;

            #endregion

            #region BlackspotRadius

            ZapGB2Recorder.Properties.Settings.Default.blackspotRadius = Convert.ToInt32(numericUpDown1.Value);

            #endregion


            ZapGB2Recorder.Properties.Settings.Default.Save();
            Log("Settings saved");
        }

        //Loads the settings
        public void LoadSettings()
        {
            //Load sell settings

            #region SellBlue
            if (ZapGB2Recorder.Properties.Settings.Default.sellBlue == true)
            {
                sellBlue.Checked = true;
            }
            else
            {
                sellBlue.Checked = false;
            }
            #endregion

            #region SellGreen
            if (ZapGB2Recorder.Properties.Settings.Default.sellGreen == true)
            {
                sellGreen.Checked = true;
            }
            else
            {
                sellGreen.Checked = false;
            }
            #endregion

            #region SellGrey
            if (ZapGB2Recorder.Properties.Settings.Default.sellGrey == true)
            {
                sellGrey.Checked = true;
            }
            else
            {
                sellGrey.Checked = false;
            }
            #endregion

            #region SellPurple
            if (ZapGB2Recorder.Properties.Settings.Default.sellPurple == true)
            {
                sellPurple.Checked = true;
            }
            else
            {
                sellPurple.Checked = false;
            }
            #endregion

            #region SellWhite
            if (ZapGB2Recorder.Properties.Settings.Default.sellWhite == true)
            {
                sellWhite.Checked = true;
            }
            else
            {
                sellWhite.Checked = false;
            }
            #endregion

            //Load mail settings

            #region MailBlue
            if (ZapGB2Recorder.Properties.Settings.Default.mailBlue == true)
            {
                mailBlue.Checked = true;
            }
            else
            {
                mailBlue.Checked = false;
            }
            #endregion

            #region MailGreen
            if (ZapGB2Recorder.Properties.Settings.Default.mailGreen == true)
            {
                mailGreen.Checked = true;
            }
            else
            {
                mailGreen.Checked = false;
            }
            #endregion

            #region MailGrey
            if (ZapGB2Recorder.Properties.Settings.Default.mailGrey == true)
            {
                mailGrey.Checked = true;
            }
            else
            {
                mailGrey.Checked = false;
            }
            #endregion

            #region MailPurple
            if (ZapGB2Recorder.Properties.Settings.Default.mailPurple == true)
            {
                mailPurple.Checked = true;
            }
            else
            {
                mailPurple.Checked = false;
            }
            #endregion

            #region MailWhite
            if (ZapGB2Recorder.Properties.Settings.Default.mailWhite == true)
            {
                mailWhite.Checked = true;
            }
            else
            {
                mailWhite.Checked = false;
            }
            #endregion

            //Load misc settings

            #region PlaySound
            if (ZapGB2Recorder.Properties.Settings.Default.playSound == true)
            {
                playSound.Checked = true;
            }
            else
            {
                playSound.Checked = false;
            }
            #endregion

            #region OnTop
            if (ZapGB2Recorder.Properties.Settings.Default.onTop == true)
            {
                onTop.Checked = true;
                this.TopMost = true;
            }
            else
            {
                onTop.Checked = false;
            }
            #endregion

            #region MinimumDurability

            minDurabilityTextbox.Text = ZapGB2Recorder.Properties.Settings.Default.minimumDurability;

            #endregion

            #region MinimumFreeBagSlots

            minBagSlotTextbox.Text = ZapGB2Recorder.Properties.Settings.Default.minimumFreeBagSlots;

            #endregion

            #region RecordingSleepTime

            recordingTimeTextbox.Text = ZapGB2Recorder.Properties.Settings.Default.recorderSleepTime;

            #endregion

            #region AddIfMoved

            addIfMovedTextbox.Text = ZapGB2Recorder.Properties.Settings.Default.addIfMoved;

            #endregion

            #region BlackspotRadius

            numericUpDown1.Value = ZapGB2Recorder.Properties.Settings.Default.blackspotRadius;

            #endregion

            Log("Settings loaded");
        }

        //Saves the settings when a setting is changed under the setting tab
        public void SettingsChanged(object sender, EventArgs arg)
        {
            SaveSettings();
        }

        //On form closing
        public void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Hide the form when it's closing instead of closing it!
            Class1.isHidden = true;
            SaveSettings();
            e.Cancel = true;
            this.Hide();
        }

        //Thread to update the labels and shizzle in the background
        public void BackgroundPULSE()
        {
            Log("BackgroundPULSE Started");

            while (true)
            {
                Thread.Sleep(50);

                blackspotLabel.Text = "Blackspots: " + blackspotsList.Count().ToString();
                vendorLabel.Text = "Vendors: " + vendorsList.Count().ToString();
                hotspotLabel.Text = "Hotspots: " + hotspotsList.Count().ToString();
                mailboxLabel.Text = "Mailboxes: " + mailboxList.Count().ToString();
            }
        }

        private void addBlackspotButton_Click(object sender, EventArgs e)
        {

        }

        private void clearBlackspotsButton_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Clear all blackspots?", this.Text) == true)
            {
                blackspotsList.Clear();
            }
        }

        private void clearHotspotsButton_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Clear all hotspots?", this.Text) == true)
            {
                hotspotsList.Clear();
            }
        }

        private void addMailboxButton_Click(object sender, EventArgs e)
        {

        }

        private void clearMailboxesButton_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Clear all mailboxes?", this.Text) == true)
            {
                mailboxList.Clear();
            }
        }

        private void addVendorsButton_Click(object sender, EventArgs e)
        {

        }

        private void clearVendorsButton_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Clear all hotspots?", this.Text) == true)
            {
                hotspotsList.Clear();
                UpdateListBox1();
            }
        }

        public void RecordingPULSE()
        {
            while (isRecording)
            {
                if (notTheSame())
                {
                    if (playSound.Checked == true)
                    {
                        PlaySound("bloop.wav");
                    }

                    Log("Adding: " + getHotspot());
                    hotspotsList.Add(getHotspot());

                    oldLocation = new WoWPoint(ObjectManager.Me.Location.X, ObjectManager.Me.Location.Y, ObjectManager.Me.Location.Z);

                    //Change recording sleep time!
                    Thread.Sleep(Convert.ToInt32(recordingTimeTextbox.Text));

                    UpdateListBox1();
                }
                else
                {
                    string temp = distanceToMove().ToString().Split(',')[0] + " yards";
                    moveLabel.Text = "Move: " + temp;
                    Thread.Sleep(100);
                }
            }
        }

        public double distanceToMove()
        {
            return Convert.ToInt32(addIfMovedTextbox.Text) - ObjectManager.Me.WorldLocation.Distance(oldLocation);
        }

        public bool notTheSame()
        {
            return (ObjectManager.Me.Location.Distance(oldLocation) > Convert.ToInt32(addIfMovedTextbox.Text));
        }

        public bool MessageBoxHelp(string text, string title)
        {
            DialogResult dialog = MessageBox.Show(text, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dialog == DialogResult.Yes)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string getHotspot()
        {
            return string.Format(CultureInfo.CreateSpecificCulture("en-US"), "<Hotspot X=\"{0}\" Y=\"{1}\" Z=\"{2}\" />", ObjectManager.Me.Location.X, ObjectManager.Me.Location.Y, ObjectManager.Me.Location.Z);
        }

        private void startRecordingButton_Click(object sender, EventArgs e)
        {
            if (!isRecording)
            {
                try
                {
                    //We're not recording, so lets start!
                    isRecording = true;
                    recordingThread = new Thread(new ThreadStart(RecordingPULSE));
                    recordingThread.IsBackground = true;
                    recordingThread.Start();

                    recordLabel.Text = "Recording: Yes";
                }
                catch
                {

                }
            }
            else
            {
                //Print error
            }
        }

        private void stopRecordingButton_Click(object sender, EventArgs e)
        {
            if (isRecording)
            {
                //We're  recording, so lets stop!
                isRecording = false;
                recordLabel.Text = "Recording: No";
            }
            else
            {
                //Print error
            }
        }

        public void SaveProfile()
        {
            if(hotspotsList.Count() >= 1)
            {
                int randomNumber = new Random().Next(1, 99999);
                string path = Logging.ApplicationPath + @"\" + ObjectManager.Me.ZoneText + "[" + randomNumber.ToString() + "]" + ".xml";

                while (File.Exists(path))
                {
                    //Will generate another name if file allready exists!
                    randomNumber = new Random().Next(1, 99999);
                    path = Logging.ApplicationPath + @"\" + ObjectManager.Me.ZoneText + "[" + randomNumber.ToString() + "]" + ".xml";
                }


                StreamWriter writer = new StreamWriter(path);
                Log("Writing profile: " + path);


                //BEGINNING OF THE PROFILE WRITING
                writer.WriteLine("<HBProfile>");
                writer.WriteLine("<Name>" + ObjectManager.Me.ZoneText + "</Name>");
                writer.WriteLine("<MinDurability>" + minDurabilityTextbox.Text + "</MinDurability>");
                writer.WriteLine("<MinFreeBagSlots>" + minBagSlotTextbox.Text + "</MinFreeBagSlots>");
                writer.WriteLine("");

                writer.WriteLine("<MinLevel>1</MinLevel>");
                writer.WriteLine("<MaxLevel>86</MaxLevel>");
                writer.WriteLine("<Factions>99999</Factions>");
                writer.WriteLine("");


                #region Mail
                if (mailGrey.Checked == true)
                {
                    writer.WriteLine("<MailGrey>true</MailGrey>");
                }
                else
                {
                    writer.WriteLine("<MailGrey>false</MailGrey>");
                }

                if (mailWhite.Checked == true)
                {
                    writer.WriteLine("<MailWhite>true</MailWhite>");
                }
                else
                {
                    writer.WriteLine("<MailWhite>false</MailWhite>");
                }

                if (mailGreen.Checked == true)
                {
                    writer.WriteLine("<MailGreen>true</MailGreen>");
                }
                else
                {
                    writer.WriteLine("<MailGreen>false</MailGreen>");
                }

                if (mailBlue.Checked == true)
                {
                    writer.WriteLine("<MailBlue>true</MailBlue>");
                }
                else
                {
                    writer.WriteLine("<MailBlue>false</MailBlue>");
                }

                if (mailPurple.Checked == true)
                {
                    writer.WriteLine("<MailPurple>true</MailPurple>");
                }
                else
                {
                    writer.WriteLine("<MailPurple>false</MailPurple>");
                }
                #endregion

                writer.WriteLine("");

                #region SellOptions

                if (sellGrey.Checked == true)
                {
                    writer.WriteLine("<SellGrey>true</SellGrey>");
                }
                else
                {
                    writer.WriteLine("<SellGrey>false</SellGrey>");
                }

                if (sellWhite.Checked == true)
                {
                    writer.WriteLine("<SellWhite>true</SellWhite>");
                }
                else
                {
                    writer.WriteLine("<SellWhite>false</SellWhite>");
                }

                if (sellGreen.Checked == true)
                {
                    writer.WriteLine("<SellGreen>true</SellGreen>");
                }
                else
                {
                    writer.WriteLine("<SellGreen>false</SellGreen>");
                }

                if (sellBlue.Checked == true)
                {
                    writer.WriteLine("<SellBlue>true</SellBlue>");
                }
                else
                {
                    writer.WriteLine("<SellBlue>false</SellBlue>");
                }

                if (sellPurple.Checked == true)
                {
                    writer.WriteLine("<SellPurple>true</SellPurple>");
                }
                else
                {
                    writer.WriteLine("<SellPurple>false</SellPurple>");
                }

                #endregion

                writer.WriteLine("");

                #region Misc

                writer.WriteLine("<Vendors>");

                foreach (string vend in vendorsList)
                    writer.WriteLine(vend);

                writer.WriteLine("</Vendors>");
                writer.WriteLine("");

                writer.WriteLine("<Mailboxes>");

                foreach (string mail in mailboxList)
                    writer.WriteLine(mail);

                writer.WriteLine("</Mailboxes>");
                writer.WriteLine("");


                writer.WriteLine("<Blackspots>");

                foreach (string black in blackspotsList)
                    writer.WriteLine(black);

                writer.WriteLine("</Blackspots>");
                writer.WriteLine("");

                writer.WriteLine("<Hotspots>");

                //HOTSPOTS
                foreach (string loc in hotspotsList)
                    writer.WriteLine(loc);


                #endregion

                writer.WriteLine("</Hotspots>");
                writer.WriteLine("</HBProfile>");
                writer.Close();


                Log("Profile writing completed!");

            }
            else
            {
                MessageBox.Show("You dont have any hotspots to save!");
            }
        }

        private void saveProfileButton_Click(object sender, EventArgs e)
        {

        }

        private void saveSettingsButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void clearEverythingButton_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Are you standing in the middle of the blackspot and have choosen the prefered radius?\nCurrent radius = " + numericUpDown1.Value.ToString(), this.Text) == true)
            {
                string tempBlackspot = string.Format(CultureInfo.CreateSpecificCulture("en-US"), "<Blackspot X=\"{0}\" Y=\"{1}\" Z=\"{2}\" Radius=\"{3}\" />", ObjectManager.Me.Location.X, ObjectManager.Me.Location.Y, ObjectManager.Me.Location.Z, numericUpDown1.Value.ToString());
                Log("Added: " + tempBlackspot);
                blackspotsList.Add(tempBlackspot);
                UpdateListBox2();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Are you standing next to a mailbox?", this.Text) == true)
            {
                string tempMailbox = "<Mailbox" + getHotspot().Replace("<Hotspot", "");
                Log("Adding: " + tempMailbox);
                mailboxList.Add(tempMailbox);
                UpdateListBox4();
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //IsRepairMerchant
            if (ObjectManager.Me.GotTarget == true)
            {
                if (ObjectManager.Me.CurrentTarget.IsRepairMerchant == true)
                {
                    string tempVendor = string.Format(CultureInfo.CreateSpecificCulture("en-US"), "<Vendor Name=\"{0}\" Entry=\"{1}\" Type=\"{2}\" X=\"{3}\" Y=\"{4}\" Z=\"{5}\" />",
                        ObjectManager.Me.CurrentTarget.Name, ObjectManager.Me.CurrentTarget.Entry.ToString(), "Repair", ObjectManager.Me.CurrentTarget.X.ToString(), ObjectManager.Me.CurrentTarget.Y.ToString(),
                        ObjectManager.Me.CurrentTarget.Z.ToString());

                    if (MessageBoxHelp("Do you want to add this NPC as a vendor?\n\n" + tempVendor, this.Text) == true)
                    {
                        Log("Adding: " + tempVendor);
                        vendorsList.Add(tempVendor);
                        UpdateListBox3();
                    }
                }
                else
                {
                    Log(ObjectManager.Me.CurrentTarget.Name + " is not an RepairMerchant");
                    MessageBox.Show(ObjectManager.Me.CurrentTarget.Name + " is not an RepairMerchant");
                }
            }
            else if (ObjectManager.Me.GotTarget != true)
            {
                MessageBox.Show("You dont have any target!");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveProfile();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Clear EVERYTHING?", this.Text) == true)
            {
                hotspotsList.Clear();
                blackspotsList.Clear();
                mailboxList.Clear();
                vendorsList.Clear();

                Log("Cleared everything from the current profile");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Clear all mailboxes?", this.Text) == true)
            {
                mailboxList.Clear();
                UpdateListBox4();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Clear all blackspots?", this.Text) == true)
            {
                blackspotsList.Clear();
            }

            UpdateListBox2();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Clear all vendors?", this.Text) == true)
            {
                vendorsList.Clear();
            }

            UpdateListBox3();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            UpdateListBox1();
        }

        public void UpdateListBox1()
        {
            listBox1.Items.Clear();

            foreach (string q in hotspotsList)
            {
                listBox1.Items.Add(q);
            }
        }

        public void UpdateListBox2()
        {
            listBox2.Items.Clear();

            foreach (string q in blackspotsList)
            {
                listBox2.Items.Add(q);
            }
        }

        public void UpdateListBox3()
        {
            listBox3.Items.Clear();

            foreach (string q in vendorsList)
            {
                listBox3.Items.Add(q);
            }
        }

        public void UpdateListBox4()
        {
            listBox4.Items.Clear();

            foreach (string q in mailboxList)
            {
                listBox4.Items.Add(q);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                if (MessageBoxHelp("Remove selected hotspot?\n\n" + listBox1.SelectedItem.ToString(), this.Text) == true)
                {
                    Form1.hotspotsList.Remove(listBox1.SelectedItem.ToString());
                    UpdateListBox1();
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedItems.Count > 0)
            {
                if (MessageBoxHelp("Remove selected blackspot?\n\n" + listBox2.SelectedItem.ToString(), this.Text) == true)
                {
                    blackspotsList.Remove(listBox2.SelectedItem.ToString());
                    UpdateListBox2();
                }
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (listBox3.SelectedItems.Count > 0)
            {
                if (MessageBoxHelp("Remove selected vendor?\n\n" + listBox3.SelectedItem.ToString(), this.Text) == true)
                {
                    vendorsList.Remove(listBox3.SelectedItem.ToString());
                    UpdateListBox3();
                }
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (listBox4.SelectedItems.Count > 0)
            {
                if (MessageBoxHelp("Remove selected mailbox?\n\n" + listBox4.SelectedItem.ToString(), this.Text) == true)
                {
                    mailboxList.Remove(listBox4.SelectedItem.ToString());
                    UpdateListBox4();
                }
            }
        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Are you standing in the middle of the blackspot and have choosen the prefered radius?\nCurrent radius = " + numericUpDown1.Value.ToString(), this.Text) == true)
            {
                string tempBlackspot = string.Format(CultureInfo.CreateSpecificCulture("en-US"), "<Blackspot X=\"{0}\" Y=\"{1}\" Z=\"{2}\" Radius=\"{3}\" />", ObjectManager.Me.Location.X, ObjectManager.Me.Location.Y, ObjectManager.Me.Location.Z, numericUpDown1.Value.ToString());
                Log("Added: " + tempBlackspot);
                blackspotsList.Add(tempBlackspot);
                UpdateListBox2();
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Are you standing next to a mailbox?", this.Text) == true)
            {
                string tempMailbox = "<Mailbox" + getHotspot().Replace("<Hotspot", "");
                Log("Adding: " + tempMailbox);
                mailboxList.Add(tempMailbox);
                UpdateListBox4();
            }
        }

        private void button13_Click_1(object sender, EventArgs e)
        {
            //IsRepairMerchant
            if (ObjectManager.Me.GotTarget == true)
            {
                if (ObjectManager.Me.CurrentTarget.IsRepairMerchant == true)
                {
                    string tempVendor = string.Format(CultureInfo.CreateSpecificCulture("en-US"), "<Vendor Name=\"{0}\" Entry=\"{1}\" Type=\"{2}\" X=\"{3}\" Y=\"{4}\" Z=\"{5}\" />",
                        ObjectManager.Me.CurrentTarget.Name, ObjectManager.Me.CurrentTarget.Entry.ToString(), "Repair", ObjectManager.Me.CurrentTarget.X.ToString(), ObjectManager.Me.CurrentTarget.Y.ToString(),
                        ObjectManager.Me.CurrentTarget.Z.ToString());

                    if (MessageBoxHelp("Do you want to add this NPC as a vendor?\n\n" + tempVendor, this.Text) == true)
                    {
                        Log("Adding: " + tempVendor);
                        vendorsList.Add(tempVendor);
                        UpdateListBox3();
                    }
                }
                else
                {
                    Log(ObjectManager.Me.CurrentTarget.Name + " is not an RepairMerchant");
                    MessageBox.Show(ObjectManager.Me.CurrentTarget.Name + " is not an RepairMerchant");
                }
            }
            else if (ObjectManager.Me.GotTarget != true)
            {
                MessageBox.Show("You dont have any target!");
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Reverse all the hotspots?", this.Text) == true)
            {
                hotspotsList.Reverse();
                UpdateListBox1();
                Log("Reversed hotspots");
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Load hotspots from an existing profile? \n\n Please not that this will DELETE the current recorded hotspots!", this.Text) == true)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                DialogResult results = new DialogResult();

                results = dialog.ShowDialog();

                if (results == DialogResult.OK)
                {
                    hotspotsList.Clear();
                    UpdateListBox1();

                    string tempLine;

                    StreamReader file = new System.IO.StreamReader(@dialog.FileName);
                    while ((tempLine = file.ReadLine()) != null)
                    {
                        if (tempLine.Contains("<Hotspot "))
                        {
                            if(!hotspotsList.Contains(tempLine))
                            {
                                hotspotsList.Add(tempLine);
                            }
                        }
                    }

                    UpdateListBox1();
                    Log("Loaded hotspots from profile");
                }
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Load blackspots from an existing profile?", this.Text) == true)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                DialogResult results = new DialogResult();

                results = dialog.ShowDialog();

                if (results == DialogResult.OK)
                {
                    string tempLine;

                    StreamReader file = new System.IO.StreamReader(@dialog.FileName);
                    while ((tempLine = file.ReadLine()) != null)
                    {
                        if (tempLine.Contains("<Blackspot "))
                        {
                            if (!blackspotsList.Contains(tempLine))
                            {
                                blackspotsList.Add(tempLine);
                            }
                        }
                    }

                    UpdateListBox2();
                    Log("Loaded blackspots from profile");
                }
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Load vendors from an existing profile?", this.Text) == true)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                DialogResult results = new DialogResult();

                results = dialog.ShowDialog();

                if (results == DialogResult.OK)
                {
                    string tempLine;

                    StreamReader file = new System.IO.StreamReader(@dialog.FileName);
                    while ((tempLine = file.ReadLine()) != null)
                    {
                        if (tempLine.Contains("<Vendor "))
                        {
                            if (!vendorsList.Contains(tempLine))
                            {
                                vendorsList.Add(tempLine);
                            }
                        }
                    }

                    UpdateListBox3();
                    Log("Loaded vendors from profile");
                }
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelp("Load mailboxes from an existing profile?", this.Text) == true)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                DialogResult results = new DialogResult();

                results = dialog.ShowDialog();

                if (results == DialogResult.OK)
                {
                    string tempLine;

                    StreamReader file = new System.IO.StreamReader(@dialog.FileName);
                    while ((tempLine = file.ReadLine()) != null)
                    {
                        if (tempLine.Contains("<Mailbox "))
                        {
                            if (!mailboxList.Contains(tempLine))
                            {
                                mailboxList.Add(tempLine);
                            }
                        }
                    }

                    UpdateListBox4();
                    Log("Loaded mailboxes from profile");
                }
            }
        }
    }
}
