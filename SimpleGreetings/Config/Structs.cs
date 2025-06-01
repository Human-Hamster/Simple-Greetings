using System;

namespace SimpleGreetings.Config
{
    [Serializable]
    public class TextSettings : ICloneable
    {
        public bool textEnabled { get; set; } = false;
        public string innerText { get; set; } = string.Empty;
        public int selectedChannel { get; set; } = 0;
        public float messageDelay { get; set; } = 1.0f;

        public void Merge(TextSettings other)
        {
            this.innerText = other.innerText;
            this.textEnabled = other.textEnabled;
            this.selectedChannel = other.selectedChannel;
            this.messageDelay = other.messageDelay;
        }

        public object Clone()
        {
            var clone = new TextSettings();

            clone.textEnabled = textEnabled; 
            clone.innerText = innerText;
            clone.selectedChannel = selectedChannel;
            clone.messageDelay = messageDelay;

            return clone;
        }
    }

    [Serializable]
    public class MacroSettings
    {
        public bool macroEnabled { get; set; } = false;
        public int macro { get; set; } = 0;
        public string[] executeOrder { get; set; } = { "Macro", "Text" };
        public int macroType { get; set; } = 0; 
        public void Merge(MacroSettings other)
        {
            this.macroEnabled = other.macroEnabled;
            this.macro = other.macro;
            this.executeOrder = other.executeOrder;
            this.macroType = other.macroType;
        }

        public bool MacroFirst()
        {
            return 0 == Array.IndexOf(executeOrder, "Macro");
        }
    }
    
    [Serializable]
    public class InstanceSettings
    {
        public bool Roulettes { get; set; } = false;
        public bool Dungeons { get; set; } = true;
        //public bool AllianceRaid { get; set; } = false;
        //public bool NormalRaids { get; set; } = false;
        public bool Raids { get; set; } = false;
        public bool Trials { get; set; } = false;

        public bool CheckContentType(uint contentType)
        {
            switch(contentType)
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
    }
}

