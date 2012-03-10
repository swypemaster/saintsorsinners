using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx;
using Styx.Plugins.PluginClass;

namespace ZapGB2Recorder
{
    public class Class1 : HBPlugin
    {
        public override string Author { get { return "Zapman"; } }
        public override Version Version { get { return new Version(4, 1, 0, 0); } }
        public override string Name { get { return "ZapRecorder"; } }
        public override bool WantButton { get { return true; } }
        public override string ButtonText { get { return "Show Window"; } }

        public bool hasBeenInitialized = false;
        public Form1 myForm;
        public static bool isHidden = false;

        private int numberOfForms = 0;

        public void Initialize()
        {

        }

        public override void Pulse()
        {
            if (!hasBeenInitialized)
            {
                hasBeenInitialized = true;
                Initialize();
            }
        }

        public override void OnButtonPress()
        {
            if (!Class1.isHidden)
            {
                if (numberOfForms == 0)
                {
                    //We need to create a new form
                    myForm = new Form1();
                    myForm.Show();
                    numberOfForms++;
                }
            }
            else
            {
                //Lets create open the old form
                myForm.Show();
            }
        }
    }
}
