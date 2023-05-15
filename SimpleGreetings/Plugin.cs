using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using SimpleGreetings.Windows;
using Dalamud.Data;
using SimpleGreetings.Handlers;
using Lumina.Excel.GeneratedSheets;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using System;
using SimpleGreetings.Config;

namespace SimpleGreetings
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Simple Greetings";
        private const string CommandName = "/simplegreet";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }

        public Configuration Config { get; init; }
        public WindowSystem WindowSystem = new("Simple Greetings");

        private DataManager _data { get; init; }
        private readonly TerritoryHandler territoryHandler = null!;

        private MainWindow MainWindow { get; init; }
        private ChatGui chatGui { get; init; }
        private ClientState clientState { get; init; }

        private PartyList party { get; init; }
        private bool queued = false;

        private int LastPartySize { get; set; } = 0;

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

            this.Config = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialize(this.PluginInterface);

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
            chatGui.ChatMessage += onChatMessage;
        }

        public void onChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if(type == XivChatType.ErrorMessage)
            {
                #if DEBUG
                this.LogXivChatEntryDebug($"Error Message: {message.TextValue.ToString().Trim()}");
                #endif

                if (message.TextValue.ToString().Trim().Contains("Your registration is withdrawn"))
                {
                    #if DEBUG
                    this.LogXivChatEntryDebug($"Queued greet flag is dequeued");
                    #endif

                    this.LastPartySize = 0;
                    this.queued = false;
                }
            }
        }

        private void onCfPop(object? sender, ContentFinderCondition condition)
        {
            // TODO: Implement a method here that checks for content types 
            // Against the config options that the user provides

            #if DEBUG
            this.LogXivChatEntryDebug($"Content Type: {condition.ContentType?.ToString()}");
            this.LogXivChatEntryDebug($"Content Type Row: {condition.ContentType.Row.ToString()}");
            this.LogXivChatEntryDebug($"Content Type Value: {condition.ContentType.Value}");
            this.LogXivChatEntryDebug($"Party Length: {party.Length}");
            #endif

            if(Config.instanceSettings.CheckContentType(condition.ContentType.Row))
            {
                this.queued = true;
                this.LastPartySize = party.Length;
                #if DEBUG
                this.LogXivChatEntryDebug($"Error Message: {Config.instanceSettings.CheckContentType(condition.ContentType.Row)}");
                #endif
            }
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            MainWindow.IsOpen = true;
        }

        private void sendMacro()
        {
            if (Config.macroSettings.macroEnabled)
            {
                if (!IsMacroChainLoaded())
                {
                    LogXivChatEntryDebug("Couldn't find Macro Chain plugin. Are you sure it's installed?");
                    return;
                } 

                if (Config.macroSettings.macro == -1 || Config.macroSettings.macro > 99 || Config.macroSettings.macro < 1)
                {
                    LogXivChatEntryDebug($"Macro #{Config.macroSettings.macro} is not valid, could not send macro greeting.");
                } 
                else
                {
                    var macroHandler = GetMacroChainHandler();
                    macroHandler("/runmacro", Config.macroSettings.ToString());
                }
            }
        }

        private void sendText()
        {
            if (Config.textSettings.textEnabled)
            {
                if (this.Config.textSettings.innerText.Trim().Length == 0) {
                    LogXivChatEntryDebug("There's no greet message to send!");
                } 
                else
                {
                    this.chatGui.PrintChat(
                        CreateChatMessage(Config.textSettings.innerText, 
                                          Config.channelOptions[Config.textSettings.selectedChannel])
                        );
                }
            }
        }

        private async void OnAreaChanged(object? sender, ushort area)
        {
            if (!this.queued)
            {
                return;
            }

            if (Config.OnlyActivateOnNewPartyMember &&  (this.party.Length > this.LastPartySize))
            {
                this.queued = false;
                this.LastPartySize = 0;
                return;
            }

            this.LogXivChatEntryDebug("Flag detected. Deploying Greetings");
            await Task.Delay((Int32)(this.Config.textSettings.messageDelay * 1000));

            var macroFirst = Config.macroSettings.MacroFirst();

            if (macroFirst) {
                sendMacro();
                await Task.Delay((Int32)(1000)); // Wait a second before deploying second to avoid spam detection
                sendText();
            }
            else 
            { 
                sendText();
                await Task.Delay((Int32)(1000));
                sendMacro();
            }

            this.queued = false;

            //this.chatGui.PrintChat(CreateChatMessage(this.Configuration.greetText, this.Configuration._xivEquiv[this.Configuration.outputChannel]));
            //macro_handler("/runmacro", "98");

            //AutoTranslatePayload[] payload =  { new AutoTranslatePayload(2, 1) };
            //this.LogXivChatEntryEcho(payload[0].Text);
            //var message = new SeString(payload);
            //this.chatGui.Print(message);

            #if DEBUG
            this.LogXivChatEntryDebug($"Terrority changed to {area}");
            this.LogXivChatEntryDebug($"Territory is {this.territoryHandler.getTerritoryData(area).Name}");
            this.LogXivChatEntryDebug($"Territory is (raw string) {this.territoryHandler.getTerritoryData(area).RawString}");
            #endif
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
            this.chatGui.ChatMessage -= onChatMessage;
        }
    }
}

