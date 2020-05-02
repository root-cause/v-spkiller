using System;
using System.IO;
using System.Xml;
using GTA;
using SPKiller.Enums;

namespace SPKiller.Managers
{
    public static class SaveManager
    {
        #region Constants
        private const int SaveVersion = 1;
        private static readonly string SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SPKiller");
        private static readonly string SaveFile = Path.Combine(SaveDirectory, "save.xml");
        #endregion

        #region Save data
        public static int VanIndex { get; private set; } = 0;
        public static KillerFlags Flags { get; private set; } = KillerFlags.None;
        #endregion

        #region Config
        public static int ClueReward { get; private set; } = 5000;
        public static int KillReward { get; private set; } = 50000;
        public static bool UseFMM { get; private set; } = true;
        #endregion

        #region Private methods
        private static void MakeDefaultSave()
        {
            VanIndex = LocationManager.GetRandomVanIndex();
            Flags = KillerFlags.None;

            Save();
        }

        private static void ReadConfig()
        {
            ScriptSettings config = ScriptSettings.Load(Path.Combine("scripts", "SPKiller_Config.ini"));

            ClueReward = config.GetValue("CONFIG", "CLUE_REWARD", 5000);
            KillReward = config.GetValue("CONFIG", "KILL_REWARD", 50000);
            UseFMM = config.GetValue("CONFIG", "USE_FMM_AS_KILLER", true);
        }
        #endregion

        #region Public methods
        public static KillerStage Load()
        {
            if (!Directory.Exists(SaveDirectory) || !File.Exists(SaveFile))
            {
                MakeDefaultSave();
            }

            // Load config
            ReadConfig();

            // Load save file
            XmlDocument doc = new XmlDocument();
            doc.Load(SaveFile);

            int saveVersion = Convert.ToInt32(doc.SelectSingleNode("/SerialKillerSave/@version")?.Value);
            if (SaveVersion != saveVersion)
            {
                throw new NotImplementedException("Save version handling is not implemented.");
            }

            // Load van location
            XmlNode vanLocation = doc.SelectSingleNode("//VanLocation");
            if (vanLocation != null)
            {
                VanIndex = Convert.ToInt32(vanLocation.InnerText);
            }
            else
            {
                throw new Exception("VanLocation not found in save file.");
            }

            // Load flags
            XmlNode flags = doc.SelectSingleNode("//Flags");
            if (Enum.TryParse(flags?.InnerText, out KillerFlags newFlags))
            {
                Flags = newFlags;
            }
            else
            {
                throw new Exception("Failed to load Flags from save file.");
            }

            // Find current stage
            if (HasFlag(KillerFlags.KilledKiller))
            {
                return KillerStage.Complete;
            }
            else
            {
                if (HasFlag(KillerFlags.FoundLimb | KillerFlags.FoundWriting | KillerFlags.FoundHandprint | KillerFlags.FoundMachete))
                {
                    return HasFlag(KillerFlags.FoundVan) ? KillerStage.SearchingKiller : KillerStage.SearchingVan;
                }
                else
                {
                    return KillerStage.SearchingClues;
                }
            }
        }

        public static void AddFlag(KillerFlags flag)
        {
            Flags |= flag;
        }

        public static bool HasFlag(KillerFlags flag)
        {
            return (Flags & flag) == flag;
        }

        public static void RemoveFlag(KillerFlags flag)
        {
            Flags &= ~flag;
        }

        public static void Save()
        {
            XmlDocument doc = new XmlDocument();

            XmlElement root = doc.CreateElement("SerialKillerSave");
            root.SetAttribute("version", SaveVersion.ToString());

            // Van location
            XmlNode vanLocation = doc.CreateElement("VanLocation");
            vanLocation.InnerText = VanIndex.ToString();

            // Flags
            XmlNode flagData = doc.CreateElement("Flags");
            flagData.InnerText = Flags.ToString();

            // Save
            root.AppendChild(vanLocation);
            root.AppendChild(flagData);
            doc.AppendChild(root);

            Directory.CreateDirectory(SaveDirectory);
            doc.Save(SaveFile);
        }
        #endregion
    }
}