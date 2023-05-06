using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using SimpleGreetings.Windows;
using Dalamud.Data;
using SimpleGreetings.Handlers;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Game.ClientState.Party;
using Windows.Foundation.Metadata;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System;
using System.Linq;

namespace SimpleGreetings
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Simple Greetings";
        private const string CommandName = "/simplegreet";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("SamplePlugin");

        private DataManager _data { get; init; }
        private readonly TerritoryHandler territoryHandler = null!;

        private MainWindow MainWindow { get; init; }
        private ChatGui chatGui { get; init;}
        private ClientState clientState { get ; init; }

        private PartyList party { get; init; }
        private bool queued = false;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] ChatGui chatGui, 
            [RequiredVersion("1.0")] PartyList party, 
            [RequiredVersion("1.0")] ClientState clientState)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this._data = dataManager;
            this.territoryHandler = new TerritoryHandler(dataManager);
            this.party = party;

            this.chatGui = chatGui;
            this.clientState = clientState;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            //var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            //var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

            MainWindow = new MainWindow(this);
            WindowSystem.AddWindow(MainWindow);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the configuration window for Simple Greetings."
            });

            // Event Listeners
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            clientState.TerritoryChanged += OnAreaChanged;
            clientState.CfPop += onCfPop;
        }

        private void onCfPop(object? sender, ContentFinderCondition condition)
        {
            // TODO: Implement a method here that checks for content types 
            // Against the config options that the user provides

            this.LogXivChatEntryDebug($"Content Type: {condition.ContentType?.ToString()}");

            if (condition.ContentType != null && condition.ContentType.ToString().Contains("Roulette")) {
                var chatEntry = new XivChatEntry();
                chatEntry.Message = this.Configuration.greetText;
                this.queued = true;
            }
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            this.CommandManager.RemoveHandler(CommandName);

            // Clean up listeners
            this.PluginInterface.UiBuilder.Draw -= DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
            this.clientState.TerritoryChanged -= OnAreaChanged;
            this.clientState.CfPop -= onCfPop;
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            MainWindow.IsOpen = true;
        }

        private void sendMacro()
        {
            if (Configuration.macroEnabled)
            {
                if (!IsMacroChainLoaded())
                {
                    LogXivChatEntryDebug("Couldn't find Macro Chain plugin. Are you sure it's installed?");
                    return;
                } 

                if (Configuration.macro == -1 || Configuration.macro > 99 || Configuration.macro < 1)
                {
                    LogXivChatEntryDebug($"Macro #{Configuration.macro} is not valid, could not send macro greeting.");
                } 
                else
                {
                    var macroHandler = GetMacroChainHandler();
                    macroHandler("/runmacro", Configuration.macro.ToString());
                }

            }
        }

        private void sendText()
        {
            if (Configuration.textEnabled)
            {
                if (this.Configuration.greetText.Trim().Length == 0) {
                    LogXivChatEntryDebug("There's no greet message to send!");
                } 
                else
                {
                    this.chatGui.PrintChat(
                        CreateChatMessage(Configuration.greetText, 
                                          Configuration.channelOptions[Configuration.outputChannel])
                        );
                }
            }
        }

        private async void OnAreaChanged(object? sender, ushort area)
        {
            if (this.queued && this.territoryHandler.getTerritoryData(area).InstanceType == "dun")
            {
                this.LogXivChatEntryDebug("Dungeon detected. Deploying Greetings");
                await Task.Delay((Int32)(this.Configuration.messageDelay * 1000));

                if (Configuration.macroFirst) {
                    sendMacro();
                    sendText();
                }
                else 
                { 
                    sendText();
                    sendMacro();
                }

                this.queued = false;

                //this.chatGui.PrintChat(CreateChatMessage(this.Configuration.greetText, this.Configuration._xivEquiv[this.Configuration.outputChannel]));
                //macro_handler("/runmacro", "98");

                //AutoTranslatePayload[] payload =  { new AutoTranslatePayload(2, 1) };
                //this.LogXivChatEntryEcho(payload[0].Text);
                //var message = new SeString(payload);
                //this.chatGui.Print(message);
            }

            //PluginLog.Debug($"Terrority changed to {e}");
            //this.LogXivChatEntryEcho($"Terrority changed to {e}");
            //this.LogXivChatEntryEcho($"Territory is {this._handler.getTerritoryData(e).Name}");
            //this.LogXivChatEntryEcho($"Territory is (raw string) {this._handler.getTerritoryData(e).RawString}");
        }

        public bool IsMacroChainLoaded()
        {
            return this.CommandManager.Commands.ContainsKey("/runmacro");
        }

        public CommandInfo.HandlerDelegate GetMacroChainHandler()
        {
            return this.CommandManager.Commands["/runmacro"].Handler;
        }

        private static XivChatEntry CreateChatMessage(string text, XivChatType type)
        {
            var chatEntry = new XivChatEntry();
            chatEntry.Message = text;
            chatEntry.Type = type;

            return chatEntry;
        }

        public void LogXivChatEntryDebug(string text)
        {
            var chatEntry = new XivChatEntry();
            chatEntry.Message = text; 
            chatEntry.Type = XivChatType.Debug;

            this.chatGui.PrintChat(chatEntry);
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            MainWindow.IsOpen = true;
        }
    }
}

