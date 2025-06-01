using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using SimpleGreetings.Config;

namespace SimpleGreetings.Windows;


public class MainWindow : Window, IDisposable
{
    private Plugin plugin;
    private Configuration config;

    private TextSettings textSettings;
    private MacroSettings macroSettings;
    private InstanceSettings instanceSettings;

    // Text Settings
    private bool textEnabled;
    private string innerText;
    private int selectedChannel;
    private float messageDelay;

    // Macro Settings
    private bool macroEnabled;
    private int macro;
    private string[] executeOrder;
    private int macroType;

    // Instance Settings
    private bool roulettes;
    private bool dungeons;
    private bool allianceRaid;
    private bool normalRaids;
    private bool raids;
    private bool trials;

    private bool NewPartyOnlyEnable;

    private bool persistError { get; set;  } = false;

    public MainWindow(Plugin plugin) : base(
        "Simple Greetings - Pleased to meet you, pleasure to greet you!", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        this.config = plugin.Config;

        this.textSettings = config.textSettings;
        this.macroSettings = config.macroSettings;
        this.instanceSettings = config.instanceSettings;

        // Text Options
        textEnabled = textSettings.textEnabled;
        innerText = textSettings.innerText ?? "";
        selectedChannel = textSettings.selectedChannel;
        messageDelay = textSettings.messageDelay;

        // Macro Options
        macro = macroSettings.macro;
        macroType = macroSettings.macroType;
        macroEnabled = macroSettings.macroEnabled;
        executeOrder = macroSettings.executeOrder;

        // Roulette Options
        roulettes = instanceSettings.Roulettes;
        dungeons = instanceSettings.Dungeons;
        //allianceRaid = instanceSettings.AllianceRaid;
        //normalRaids = instanceSettings.NormalRaids;
        raids = instanceSettings.Raids;
        trials = instanceSettings.Trials;

        NewPartyOnlyEnable = config.OnlyActivateOnNewPartyMember;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }

    private static void HelpMarker(string desc)
    {
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered()) 
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    private void TextSettings()
    {
        if (ImGui.BeginTabItem("Text Settings")) 
        {
            ImGui.Checkbox("Enable", ref textEnabled);

            if (!textEnabled)
            {
                ImGui.BeginDisabled();
            }

            // add content for Tab 1
            ImGui.InputText("Greeting Text", ref innerText, 255);
            ImGui.SameLine(); HelpMarker("Text to send for greeting!\nFor auto-translate and macros, use the next tab!");
            ImGui.Separator();
            ImGui.SetNextItemWidth(120);
            ImGui.SliderFloat("Message Delay", ref messageDelay, 0.0f, 5.0f);
            ImGui.SameLine(); HelpMarker("Delay the greet text when join the instance. CTRL+Click for manual input.");

            ImGui.SetNextItemWidth(120);
            ImGui.Combo("Output Channel", ref selectedChannel, config.GetChannelOptions(), 2);

            ImGui.EndTabItem();

            if (!textEnabled)
            {
                ImGui.EndDisabled();
            }
        }
    }

    private void MacroSettings()
    {
        if (ImGui.BeginTabItem("Macro Settings"))
        {
            ImGui.Checkbox("Enable", ref macroEnabled);

            if (macroEnabled || persistError)
            {
                if (!this.plugin.IsMacroChainLoaded())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255f, 0f, 0f, 255f));
                    ImGui.SameLine(); ImGui.Text("Error: Require Macro Chain plugin to work");
                    ImGui.PopStyleColor();
                    macroEnabled = false;
                    persistError = true;
                }
                else
                {
                    persistError = false;
                }
            }

            if (!macroEnabled)
            {
                ImGui.BeginDisabled();
            }

            ImGui.Text("Macro #"); ImGui.SameLine(); HelpMarker("Macro Number to Execute");
            ImGui.SameLine(); ImGui.SetNextItemWidth(120); ImGui.InputInt("", ref macro);
            ImGui.SetNextItemWidth(150); ImGui.Combo("Macro Type", ref macroType, config.GetMacroOptions(), config.macroOptions.Length);

            if (!macroEnabled)
            {
                ImGui.EndDisabled();
            }
            ImGui.EndTabItem();
        }
    }

    public void GoodbyeSettings() {
        if (ImGui.BeginTabItem("Goodbye Settings")) {
            ImGui.Text("Work in Progress! Message/macro to send upon clearing");
            ImGui.EndTabItem();
        }
    }

    public void AdvancedSettings()
    {
        if (ImGui.BeginTabItem("Advanced Settings"))
        {
            if (ImGui.TreeNode("Order of Greetings (Drag)"))
            {
                for (int n = 0; n < executeOrder.Length; n++)
                {
                    string item = executeOrder[n];
                    ImGui.Selectable($"{n+1}. {item}");

                    if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                    {
                        int n_next = n + (ImGui.GetMouseDragDelta(0).Y < 0f ? -1 : 1);
                        if (n_next >= 0 && n_next < executeOrder.Length)
                        {
                            string temp = executeOrder[n_next];
                            executeOrder[n_next] = item;
                            executeOrder[n] = temp;
                            ImGui.ResetMouseDragDelta();
                        }
                    }
                }
                ImGui.TreePop();
            }
            ImGui.Checkbox("Only Activate If New Party Member", ref NewPartyOnlyEnable); ImGui.SameLine();  HelpMarker("Only activate the greetings if at least one new member has joined your party.\nIf your party is full prior to joining the instance, the greetings won't activate.");
            ImGui.EndTabItem();
        }
    } 

    public void InstanceSettings()
    {
        if (ImGui.BeginTabItem("Instance Settings"))
        {
            //ImGui.Text("WIP: Add Instance filtering, duty/roulette/raid filtering");

            ImGui.Checkbox("Roulettes", ref roulettes);
            ImGui.Checkbox("Dungeons", ref dungeons);
            //ImGui.Checkbox("Alliance Raid", ref allianceRaid);
            //ImGui.Checkbox("Normal Raids", ref normalRaids);
            ImGui.Checkbox("Normal/Endgame/Alliance Raids", ref raids);
            ImGui.Checkbox("Trials", ref trials);
            ImGui.EndTabItem();
        }
    } 

    public void SaveSettings()
    {
        // Save Text Settings
        this.textSettings.textEnabled = textEnabled;
        this.textSettings.innerText = innerText;
        this.textSettings.messageDelay = messageDelay;
        this.textSettings.selectedChannel = selectedChannel;

        // Save Macro Settings
        this.macroSettings.macroEnabled = macroEnabled;
        this.macroSettings.macro = macro;
        this.macroSettings.macroType = macroType;
        this.macroSettings.executeOrder = executeOrder;

        // Save Instance Settings
        //this.instanceSettings.NormalRaids = normalRaids;
        this.instanceSettings.Raids = raids;
        //this.instanceSettings.AllianceRaid = allianceRaid;  
        this.instanceSettings.Dungeons = dungeons;
        this.instanceSettings.Roulettes = roulettes;

        config.Save();

        #if DEBUG
        plugin.LogXivChatEntryDebug($"Config saved with: Greet Text {config.textSettings.innerText}");
        plugin.LogXivChatEntryDebug($"Config saved with: Delay {config.textSettings.messageDelay}");
        plugin.LogXivChatEntryDebug($"Config saved with: output channel {config.GetChannelOptions()[config.textSettings.selectedChannel]}");

        plugin.LogXivChatEntryDebug($"Config saved with: MacroType {config.macroSettings.macroType}");
        plugin.LogXivChatEntryDebug($"Config saved with: macroFirst {config.macroSettings.MacroFirst()}");

        plugin.LogXivChatEntryDebug($"Instance Config saved with: Dungeon {config.instanceSettings.Dungeons}");
        plugin.LogXivChatEntryDebug($"Instance Config saved with: Roulettes {config.instanceSettings.Roulettes}");
        #endif
    }

    public override void Draw()
    {
        ImGui.BeginTabBar("Greets");

        TextSettings();
        MacroSettings();
        GoodbyeSettings();
        InstanceSettings();
        AdvancedSettings();

        ImGui.Separator();
        ImGui.EndTabBar();

        if (ImGui.Button("Save"))
        {
            SaveSettings();
        }
    }
}
