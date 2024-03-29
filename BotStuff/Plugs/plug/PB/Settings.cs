﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Styx.WoWInternals;
using System.Xml;
using System.Runtime.Serialization;
using Styx.Helpers;
using TreeSharp;
using ObjectManager = Styx.WoWInternals.ObjectManager;

namespace HighVoltz
{
    public class ProfessionBuddySettings : Settings
    {
        public static ProfessionBuddySettings Instance { get; private set; }
        public ProfessionBuddySettings(string settingsPath)
            : base(settingsPath)
        {
            Instance = this;
            Load();
        }
        [Setting, DefaultValue("")]
        public string LastProfile { get; set; }

        [Setting, DefaultValue(null)]
        public string DataStoreTable { get; set; }

        [Setting, DefaultValue("")]
        public string WowVersion { get; set; }

        [Setting, DefaultValue(0u)]
        public uint TradeskillFrameOffset { get; set; }

        [Setting, DefaultValue("")]
        public string LastBotBase { get; set; }
    }

    public class PbProfileSettingEntry
    {
        public object Value { get; set; }
        public string Summary { get; set; }
        public string Category { get; set; }
        public bool Global { get; set; }
        public bool Hidden { get; set; }
    }

    public class PbProfileSettings
    {
        public Dictionary<string, PbProfileSettingEntry> Settings { get; private set; }

        public PbProfileSettings()
        {
            Settings = new Dictionary<string, PbProfileSettingEntry>();
        }
        public object this[string name]
        {
            get
            {
                return Settings.ContainsKey(name) ? Settings[name].Value : null;
            }
            set
            {
                Settings[name].Value = value;
                if (Professionbuddy.Instance.CurrentProfile != null)
                {
                    Save();
                    if (MainForm.IsValid)
                        MainForm.Instance.RefreshSettingsPropertyGrid();
                }
            }
        }
        string ProfileName
        {
            get
            {
                return Professionbuddy.Instance.CurrentProfile != null ?
                    Path.GetFileNameWithoutExtension(Professionbuddy.Instance.CurrentProfile.XmlPath) : "";
            }
        }
        string CharacterSettingsPath
        {
            get
            {
                return Path.Combine(Logging.ApplicationPath,
                    string.Format("Settings\\ProfessionBuddy\\{0}[{1}-{2}].xml", ProfileName,
                    ObjectManager.Me.Name, Lua.GetReturnVal<string>("return GetRealmName()", 0)));
            }
        }

        string GlobalSettingsPath
        {
            get
            {
                return Path.Combine(Logging.ApplicationPath,
                    string.Format("Settings\\ProfessionBuddy\\{0}.xml", ProfileName));
            }
        }

        public void Save()
        {
            if (Professionbuddy.Instance.CurrentProfile != null)
            {
                bool hasGlobalSettings = Settings.Any(setting => setting.Value.Global);
                bool hasCharacterSettings = Settings.Any(setting => !setting.Value.Global);
                if (hasGlobalSettings)
                    SaveGlobalSettings();
                if (hasCharacterSettings)
                    SaveCharacterSettings();
            }
        }

        void SaveCharacterSettings()
        {
            var settings = new XmlWriterSettings { Indent = true };
            using (XmlWriter writer = XmlWriter.Create(CharacterSettingsPath, settings))
            {
                var serializer = new DataContractSerializer(typeof(Dictionary<string, object>));
                Dictionary<string, object> temp = Settings.Where(setting => !setting.Value.Global).ToDictionary(kv => kv.Key, kv => kv.Value.Value);
                serializer.WriteObject(writer, temp);
            }
        }

        void SaveGlobalSettings()
        {
            var settings = new XmlWriterSettings { Indent = true };
            using (XmlWriter writer = XmlWriter.Create(GlobalSettingsPath, settings))
            {
                var serializer = new DataContractSerializer(typeof(Dictionary<string, object>));
                Dictionary<string, object> temp = Settings.Where(setting => setting.Value.Global).ToDictionary(kv => kv.Key, kv => kv.Value.Value);
                serializer.WriteObject(writer, temp);
            }
        }

        public void Load()
        {
            if (Professionbuddy.Instance.CurrentProfile != null)
            {
                Settings = new Dictionary<string, PbProfileSettingEntry>();
                LoadCharacterSettings();
                LoadGlobalSettings();
                LoadDefaultValues();
            }
        }

        void LoadCharacterSettings()
        {
            if (File.Exists(CharacterSettingsPath))
            {
                using (XmlReader reader = XmlReader.Create(CharacterSettingsPath))
                {
                    try
                    {
                        var serializer = new DataContractSerializer(typeof(Dictionary<string, object>));
                        var temp = (Dictionary<string, object>)serializer.ReadObject(reader);
                        if (temp != null)
                        {
                            foreach (var kv in temp)
                            {
                                Settings[kv.Key] = new PbProfileSettingEntry { Value = kv.Value };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Professionbuddy.Err(ex.ToString());
                    }
                }
            }
        }

        void LoadGlobalSettings()
        {
            if (File.Exists(GlobalSettingsPath))
            {
                using (XmlReader reader = XmlReader.Create(GlobalSettingsPath))
                {
                    try
                    {
                        var serializer = new DataContractSerializer(typeof(Dictionary<string, object>));
                        var temp = (Dictionary<string, object>)serializer.ReadObject(reader);
                        if (temp != null)
                        {
                            foreach (var kv in temp)
                            {
                                Settings[kv.Key] = new PbProfileSettingEntry { Value = kv.Value };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Professionbuddy.Err(ex.ToString());
                    }
                }
            }
        }

        public void LoadDefaultValues()
        {
            List<Composites.Settings> settingsList = GetDefaultSettings(Professionbuddy.Instance.PbBehavior);
            foreach (var setting in settingsList)
            {
                if (!Settings.ContainsKey(setting.SettingName))
                    Settings[setting.SettingName] = new PbProfileSettingEntry { Value = GetValue(setting.Type, setting.DefaultValue) };
                Settings[setting.SettingName].Summary = setting.Summary;
                Settings[setting.SettingName].Category = setting.Category;
                Settings[setting.SettingName].Global = setting.Global;
                Settings[setting.SettingName].Hidden = setting.Hidden;
            }
            // remove unused settings..
            Settings = Settings.Where(kv => settingsList.Any(s => s.SettingName == kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        object GetValue(TypeCode code, string value)
        {
            try
            {
                switch (code)
                {
                    case TypeCode.Boolean:
                        return bool.Parse(value);
                    case TypeCode.Byte:
                        return byte.Parse(value);
                    case TypeCode.Char:
                        return char.Parse(value);
                    case TypeCode.DateTime:
                        return DateTime.Parse(value);
                    case TypeCode.Decimal:
                        return decimal.Parse(value);
                    case TypeCode.Double:
                        return double.Parse(value);
                    case TypeCode.Int16:
                        return short.Parse(value);
                    case TypeCode.Int32:
                        return int.Parse(value);
                    case TypeCode.Int64:
                        return long.Parse(value);
                    case TypeCode.SByte:
                        return sbyte.Parse(value);
                    case TypeCode.Single:
                        return float.Parse(value);
                    case TypeCode.String:
                        return value;
                    case TypeCode.UInt16:
                        return ushort.Parse(value);
                    case TypeCode.UInt32:
                        return uint.Parse(value);
                    case TypeCode.UInt64:
                        return ulong.Parse(value);
                    default:
                        return new object();
                }
            }
            catch (Exception ex) { Logging.WriteException(ex); return null; }
        }

        public List<Composites.Settings> GetDefaultSettings(Composite comp)
        {
            var list = new List<Composites.Settings>();
            GetProfileSettings(comp, ref list);
            return list;
        }
        // recursively get all profile settings
        void GetProfileSettings(Composite comp, ref List<Composites.Settings> list)
        {
            if (comp is Composites.Settings)
                list.Add(comp as Composites.Settings);
            if (comp is GroupComposite)
            {
                foreach (var child in ((GroupComposite)comp).Children)
                    GetProfileSettings(child, ref list);
            }
        }
    }
}
