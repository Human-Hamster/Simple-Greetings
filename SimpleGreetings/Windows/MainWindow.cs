using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Windows.Media.Capture;

namespace SimpleGreetings.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin plugin;
    private Configuration config;

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

    private bool persistError { get; set;  } = false;

    public MainWindow(Plugin plugin) : base(
        "Simple Greetings - Pleased to meet you, pleasure to greet you!", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        this.config = plugin.Configuration;

        // Text Options
        textEnabled = config.textEnabled;
        innerText = config.greetText ?? "";
        selectedChannel = config.outputChannel;
        messageDelay = config.messageDelay;

        // Macro Options
        macro = config.macro;
        macroType = config.macroType;
        macroEnabled = config.macroEnabled;

        if (config.macroFirst) 
        {
            executeOrder = new string[] { "Macro", "Text" };
        } else
        {
            executeOrder = new string[] { "Text", "Macro" };
        }

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose()
    {
        config.Save();
    }

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
            ImGui.EndTabItem();
        }
    } 

    public void InstanceSettings()
    {
        if (ImGui.BeginTabItem("Instance Settings"))
        {
            ImGui.Text("WIP: Add Instance filtering, duty/roulette/raid filtering");

            ImGui.EndTabItem();
        }
    } 

    public void SaveSettings()
    {
        config.textEnabled = textEnabled;
        config.greetText = innerText;
        config.messageDelay = messageDelay;
        config.outputChannel = selectedChannel;

        config.macroEnabled = macroEnabled;
        config.macro = macro;
        config.macroType = macroType;
        config.macroFirst = Array.IndexOf(executeOrder, "Macro") == 0;

        config.Save();

        #if DEBUG

        plugin.LogXivChatEntryDebug($"Config saved with: Greet Text {config.greetText}");
        plugin.LogXivChatEntryDebug($"Config saved with: Delay {config.messageDelay}");
        plugin.LogXivChatEntryDebug($"Config saved with: output channel selection {config.outputChannel}");
        plugin.LogXivChatEntryDebug($"Config saved with: output channel {config.GetChannelOptions()[config.outputChannel]}");

        plugin.LogXivChatEntryDebug($"Config saved with: MacroType {config.macroType}");
        plugin.LogXivChatEntryDebug($"Config saved with: macroFirst {config.macroFirst}");

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
