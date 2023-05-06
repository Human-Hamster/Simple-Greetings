using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using SimpleGreetings.GameData;
using System;
using System.Linq;

namespace SimpleGreetings
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool textEnabled { get; set; } = false;
        public string greetText { get; set; } = string.Empty;
        public float messageDelay { get; set; } = 1.0f;
        public int outputChannel { get; set; } = 0;

        public bool macroEnabled { get; set; } = false;
        public bool macroFirst { get; set; } = false;
        public int macro { get; set; } = -1;
        public int macroType { get; set; } = 0;

        // Refactor this later, convert from string to enum etc
        public readonly XivChatType[] channelOptions = {XivChatType.Party, XivChatType.Say};
        public readonly MacroType[] macroOptions = {MacroType.Individual, MacroType.Shared};


        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;


        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }

        public string[] GetChannelOptions()
        {
            return channelOptions.Select(x => x.ToString()).ToArray();
        }

        public string[] GetMacroOptions()
        {
            return macroOptions.Select(x => x.ToString()).ToArray();
        }
    }
}

