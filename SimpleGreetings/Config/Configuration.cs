using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using SimpleGreetings.GameData;
using SimpleGreetings.Windows;
using System;
using System.Linq;

namespace SimpleGreetings.Config
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        // Text Settings
        protected internal TextSettings textSettings { get; set; } = new TextSettings();
        protected internal MacroSettings macroSettings { get; set; } = new MacroSettings();
        protected internal InstanceSettings instanceSettings { get; set; } = new InstanceSettings();

        // Refactor this later, convert from string to enum etc
        public readonly XivChatType[] channelOptions = { XivChatType.Party, XivChatType.Say };
        public readonly MacroType[] macroOptions = { MacroType.Individual, MacroType.Shared };

        protected internal bool OnlyActivateOnNewPartyMember { get; set; } = true;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
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

