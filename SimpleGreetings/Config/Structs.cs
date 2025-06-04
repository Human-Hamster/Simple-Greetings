using System;

namespace SimpleGreetings.Config
{
    [Serializable]
    public class TextSettings : ICloneable
    {
        public bool textEnabled = false;
        public string innerText = string.Empty;
        public int selectedChannel = 0;

        public void Merge(TextSettings other)
        {
            this.innerText = other.innerText;
            this.textEnabled = other.textEnabled;
            this.selectedChannel = other.selectedChannel;
        }

        public object Clone()
        {
            var clone = new TextSettings();

            clone.textEnabled = textEnabled;
            clone.innerText = innerText;
            clone.selectedChannel = selectedChannel;

            return clone;
        }
    }

    [Serializable]
    public class MacroSettings
    {
        public bool macroEnabled = false;
        public int macro = 0;
        public int macroType = 0;
        public void Merge(MacroSettings other)
        {
            this.macroEnabled = other.macroEnabled;
            this.macro = other.macro;
            this.macroType = other.macroType;
        }
    }

    [Serializable]
    public class InstanceSettings
    {
        public bool enabled = false;
        public bool Roulettes = false;
        public bool Dungeons = true;
        //public bool AllianceRaid { get; set; } = false;
        //public bool NormalRaids { get; set; } = false;
        public bool Raids = false;
        internal bool Trials = false;
        internal bool OnlyActivateOnNewPartyMember = true;
        internal string[] executeOrder = ["Macro", "Text"];
        public float messageDelay = 1.0f;

        public TextSettings textSettings;
        public MacroSettings macroSettings;

        public InstanceSettings()
        {
            textSettings = new TextSettings();
            macroSettings = new MacroSettings();
        }

        public bool CheckContentType(uint contentType)
        {
            switch (contentType)
            {
                case (0):
                    return Roulettes;
                case (1):
                    return Roulettes;
                case (2):
                    return Dungeons;
                case (4):
                    return Trials;
                case (5):
                    return Raids;
            }
            return false;
        }
        public bool MacroFirst()
        {
            return 0 == Array.IndexOf(executeOrder, "Macro");
        }
    }

    [Serializable]
    public class RpSettings
    {
        public bool enabled = false;
        public TextSettings textSettings;
        public MacroSettings macroSettings;
        internal string[] executeOrder = ["Macro", "Text"];
        public float messageDelay = 1.0f;

        public RpSettings()
        {
            textSettings = new TextSettings();
            macroSettings = new MacroSettings();
        }

        public bool MacroFirst()
        {
            return 0 == Array.IndexOf(executeOrder, "Macro");
        }
    }
}
