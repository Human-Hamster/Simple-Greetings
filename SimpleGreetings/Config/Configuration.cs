using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using SimpleGreetings.GameData;
using System;
using System.Linq;

namespace SimpleGreetings.Config
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        // Changed to public properties
        public InstanceSettings InstanceSettings { get; set; }
        public RpSettings RpSettings { get; set; }

        // Refactor this later, convert from string to enum etc
        internal static readonly XivChatType[] channelOptions = [XivChatType.Party, XivChatType.Say];
        internal static readonly MacroType[] macroOptions = [MacroType.Individual, MacroType.Shared];

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public Configuration()
        {
            // Initialize the new properties
            InstanceSettings = new InstanceSettings();
            RpSettings = new RpSettings();
        }

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }

        public static string[] GetChannelOptions()
        {
            return [.. channelOptions.Select(x => x.ToString())];
        }

        public static string[] GetMacroOptions()
        {
            return [.. macroOptions.Select(x => x.ToString())];
        }
    }
}
