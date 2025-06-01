using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using SimpleGreetings.Windows;
using System;
using System.Threading.Tasks;
using SimpleGreetings.Config;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace SimpleGreetings
{
    public sealed class Plugin : IDalamudPlugin
    {
        public static string Name => "Simple Greetings";
        private const string CommandName = "/simplegreet";

        internal IDalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }

        public Configuration Config { get; init; }
        public WindowSystem WindowSystem = new("Simple Greetings");

        private IDataManager DataManager { get; init; }

        private MainWindow MainWindow { get; init; }
        private IChatGui ChatGui { get; init; }
        private IClientState ClientState { get; init; }

        private IFramework Framework { get; init; }
        internal IPluginLog PlugLog { get; init; }

        private ICondition Condition { get; init; }

        private IGameGui GameGui { get; init; }

        private int playerCount { get; set; }

        private bool playerCountChanged { get; set; }

        private IPartyList PartyList { get; init; }
        private bool queued = false;

        private int LastPartySize { get; set; } = 0;

        public Plugin(
            IFramework framework,
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IDataManager dataManager,
            IChatGui chatGui,
            IPluginLog logger, 
            IPartyList party,
            IGameGui gameGui,
            ICondition condition,
            IClientState clientState)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            DataManager = dataManager;
            PartyList = party;
            GameGui = gameGui;
            Condition = condition;
            Framework = framework;
            ChatGui = chatGui;
            ClientState = clientState;

            PlugLog = logger;

            Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(PluginInterface);
            ECommonsMain.Init(PluginInterface, this);

            MainWindow = new MainWindow(this);
            WindowSystem.AddWindow(MainWindow);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the configuration window for Simple Greetings."
            });

            // Track number of players
            this.playerCount = 0;
            this.playerCountChanged = false;

            // Event Listeners
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            clientState.TerritoryChanged += OnAreaChanged;
            clientState.CfPop += onCfPop;
            chatGui.ChatMessage += onChatMessage;

            Framework.Update += this.UpdateTick;
        }

        public void onChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (type == XivChatType.ErrorMessage)
            {
#if DEBUG
                this.PlugLog.Debug($"Error Message: {message.TextValue.ToString().Trim()}");
#endif

                if (message.TextValue.ToString().Trim().Contains("Your registration is withdrawn"))
                {
#if DEBUG
                    this.PlugLog.Debug($"Queued greet flag is dequeued");
#endif

                    this.LastPartySize = 0;
                    this.queued = false;
                }
            }
#if DEBUG
                this.PlugLog.Debug($"Player count: {this.playerCount}");
#endif

        }

        private void onCfPop(ContentFinderCondition condition)
        {
            // TODO: Implement a method here that checks for content types 
            // Against the config options that the user provides

#if DEBUG
            this.PlugLog.Debug($"Content Type: {condition.ContentType}");
            this.PlugLog.Debug($"Content Type Value: {condition.ContentType.Value}");
            this.PlugLog.Debug($"Party Length: {this.PartyList.Length}");
#endif

            if (Config.instanceSettings.CheckContentType(condition.ContentType.RowId))
            {
                this.queued = true;
                this.LastPartySize = PartyList.Length;
#if DEBUG
                this.PlugLog.Debug($"Error Message: {Config.instanceSettings.CheckContentType(condition.ContentType.RowId)}");
#endif
            }
        }

        private unsafe void UpdateTick(IFramework framework)
        {
            this.CountPlayers();

            //if (this.playerCountChanged) {
            //    sendText();
            //}
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
                    PlugLog.Debug("Couldn't find Macro Chain plugin. Are you sure it's installed?");
                    return;
                }

                if (Config.macroSettings.macro == -1 || Config.macroSettings.macro > 99 || Config.macroSettings.macro < 1)
                {
                    PlugLog.Debug($"Macro #{Config.macroSettings.macro} is not valid, could not send macro greeting.");
                }
                else
                {
                    var macroHandler = GetMacroChainHandler();
                    macroHandler("/runmacro", Config.macroSettings.ToString());
                }
            }
        }

        private unsafe void CountPlayers()
        {
            var objects = GameObjectManager.Instance()->Objects.EntityIdSorted;
            GameObject* localPlayerGameObject = GameObjectManager.Instance()->Objects.IndexSorted[0];
            IntPtr namePlateWidget = this.GameGui.GetAddonByName("NamePlate");

            if (namePlateWidget == nint.Zero ||
                (!((AtkUnitBase*)namePlateWidget)->IsVisible && !Condition[ConditionFlag.Performing]) ||
                localPlayerGameObject == null || localPlayerGameObject->EntityId == 0xE0000000)
            {
                return;
            }

            bool isBound = (Condition[ConditionFlag.BoundByDuty] &&
                            localPlayerGameObject->EventId.ContentId != EventHandlerContent.TreasureHuntDirector)
                           || Condition[ConditionFlag.BetweenAreas]
                           || Condition[ConditionFlag.WatchingCutscene]
                           || Condition[ConditionFlag.DutyRecorderPlayback];

            Character* localPlayer = (Character*)localPlayerGameObject;

            var count = 0;

            for (var i = 0; i != objects.Length; i++)
            {
                GameObject* gameObject = objects[i];
                Character* characterPtr = (Character*)gameObject;

                if (gameObject == null || gameObject == localPlayerGameObject || !gameObject->IsCharacter() || (ObjectKind)characterPtr->GameObject.ObjectKind != ObjectKind.Player )
                {
                    continue;
                }

                count += 1;
            }

            if (count != 0 && this.playerCount != 0 && this.playerCount != count)
            {
                this.playerCountChanged = true;
            } 
            else 
            {
                this.playerCountChanged = false;
            }

            this.playerCount = count;
        }

        private void sendText()
        {
            if (Config.textSettings.textEnabled)
            {
                if (this.Config.textSettings.innerText.Trim().Length == 0)
                {
                    PlugLog.Debug("There's no greet message to send!");
                }
                else
                {
                    try
                    {
                        Chat.SendMessage(Chat.SanitiseText(this.Config.textSettings.innerText));
                    } catch (Exception e) { 
                        PlugLog.Debug($"There is a problem with the greeting message: {e.Message}");
                    }
                }
            }
        }

        private async void OnAreaChanged(ushort area)
        {
            if (!this.queued)
            {
                return;
            }

            if (Config.OnlyActivateOnNewPartyMember && (this.PartyList.Length > this.LastPartySize))
            {
                this.queued = false;
                this.LastPartySize = 0;
                return;
            }

            this.PlugLog.Debug("Flag detected. Deploying Greetings");
            await Task.Delay((Int32)(this.Config.textSettings.messageDelay * 1000));

            var macroFirst = Config.macroSettings.MacroFirst();

            if (macroFirst)
            {
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

#if DEBUG
            this.PlugLog.Debug($"Terrority changed to {area}");
            this.PlugLog.Debug($"Territory is {ECommons.TerritoryName.GetTerritoryName(area)}");
#endif
        }

        public bool IsMacroChainLoaded()
        {
            return this.CommandManager.Commands.ContainsKey("/runmacro");
        }

        public IReadOnlyCommandInfo.HandlerDelegate GetMacroChainHandler()
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
            ECommonsMain.Dispose();

            // Clean up listeners
            this.PluginInterface.UiBuilder.Draw -= DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
            ClientState.TerritoryChanged -= OnAreaChanged;
            ClientState.CfPop -= onCfPop;
            ChatGui.ChatMessage -= onChatMessage;
        }
    }
}

