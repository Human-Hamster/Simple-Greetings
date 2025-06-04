using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using SimpleGreetings.Config;

namespace SimpleGreetings.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin plugin;
    private InstanceSettings instanceSettings;
    private RpSettings rpSettings;

    private bool persistError { get; set; } = false;

    public MainWindow(Plugin plugin) : base(
        "Simple Greetings - Pleased to meet you, pleasure to greet you!", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        this.instanceSettings = plugin.Config.InstanceSettings; // Using public property
        this.rpSettings = plugin.Config.RpSettings; // Using public property

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

    private void TextSettings(ref TextSettings settings)
    {
        // Added ##TextSection to ensure unique ID within its scope
        ImGui.Checkbox("Enable##TextSection", ref settings.textEnabled);

        if (!settings.textEnabled)
        {
            ImGui.BeginDisabled();
        }

        ImGui.InputText("Greeting Text", ref settings.innerText, 255);
        ImGui.SameLine(); HelpMarker("Text to send for greeting!\nFor auto-translate and macros, use the Macro section!");
        ImGui.Separator();

        ImGui.SetNextItemWidth(120);
        ImGui.Combo("Output Channel", ref settings.selectedChannel, Configuration.GetChannelOptions(), 2);

        if (!settings.textEnabled)
        {
            ImGui.EndDisabled();
        }
    }

    private void MacroSettings(ref MacroSettings settings)
    {
        // Added ##MacroSection to ensure unique ID within its scope
        ImGui.Checkbox("Enable##MacroSection", ref settings.macroEnabled);

        if (settings.macroEnabled || persistError)
        {
            if (!this.plugin.IsMacroChainLoaded())
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255f, 0f, 0f, 255f));
                ImGui.SameLine(); ImGui.Text("Error: Require Macro Chain plugin to work");
                ImGui.PopStyleColor();
                settings.macroEnabled = false;
                persistError = true;
            }
            else
            {
                persistError = false;
            }
        }

        if (!settings.macroEnabled)
        {
            ImGui.BeginDisabled();
        }

        ImGui.Text("Macro #"); ImGui.SameLine(); HelpMarker("Macro Number to Execute");
        ImGui.SameLine(); ImGui.SetNextItemWidth(120); ImGui.InputInt("", ref settings.macro);
        ImGui.SetNextItemWidth(150); ImGui.Combo("Macro Type", ref settings.macroType, Configuration.GetMacroOptions(), Configuration.macroOptions.Length);

        if (!settings.macroEnabled)
        {
            ImGui.EndDisabled();
        }
    }

    public static void GoodbyeSettings()
    {
        ImGui.Text("Work in Progress! Message/macro to send upon clearing");
    }

    public void DrawInstanceSettingsContent()
    {
        // Added ##InstanceTab suffix for unique ID
        ImGui.Checkbox("Enable##InstanceTab", ref instanceSettings.enabled);

        ImGui.Separator();
        if (!instanceSettings.enabled)
        {
            ImGui.BeginDisabled();
        }

        // Added ##Instance suffix to other checkboxes in this section for unique IDs
        ImGui.Checkbox("Roulettes##Instance", ref instanceSettings.Roulettes);
        ImGui.Checkbox("Dungeons##Instance", ref instanceSettings.Dungeons);
        ImGui.Checkbox("Normal/Endgame/Alliance Raids##Instance", ref instanceSettings.Raids);
        ImGui.Checkbox("Trials##Instance", ref instanceSettings.Trials);

        ImGui.Separator();
        ImGui.Text("Text Greeting Settings:");
        // Push unique ID for this section before calling TextSettings
        ImGui.PushID("InstanceTextSettings");
        TextSettings(ref instanceSettings.textSettings);
        ImGui.PopID(); // Pop the ID after TextSettings

        ImGui.Separator();
        ImGui.Text("Macro Greeting Settings:");
        // Push unique ID for this section before calling MacroSettings
        ImGui.PushID("InstanceMacroSettings");
        MacroSettings(ref instanceSettings.macroSettings);
        ImGui.PopID(); // Pop the ID after MacroSettings

        ImGui.Separator();
        ImGui.SetNextItemWidth(120);
        ImGui.SliderFloat("Message Delay", ref instanceSettings.messageDelay, 0.0f, 5.0f);
        ImGui.SameLine(); HelpMarker("Delay the greet text when you join a new instance. CTRL+Click for manual input.");

        if (ImGui.TreeNode("Advanced Settings"))
        {
            if (ImGui.TreeNode("Order of Greetings (Drag)"))
            {
                for (int n = 0; n < instanceSettings.executeOrder.Length; n++)
                {
                    string item = instanceSettings.executeOrder[n];
                    ImGui.Selectable($"{n + 1}. {item}");

                    if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                    {
                        int n_next = n + (ImGui.GetMouseDragDelta(0).Y < 0f ? -1 : 1);
                        if (n_next >= 0 && n_next < instanceSettings.executeOrder.Length)
                        {
                            string temp = instanceSettings.executeOrder[n_next];
                            instanceSettings.executeOrder[n_next] = item;
                            instanceSettings.executeOrder[n] = temp;
                            ImGui.ResetMouseDragDelta();
                        }
                    }
                }
                ImGui.TreePop();
            }
            ImGui.Checkbox("Only Activate If New Party Member##Instance", ref instanceSettings.OnlyActivateOnNewPartyMember); ImGui.SameLine(); HelpMarker("Only activate the greetings if at least one new member has joined your party.\nIf your party is full prior to joining the instance, the greetings won't activate.");
            ImGui.TreePop();
        }

        if (!instanceSettings.enabled)
        {
            ImGui.EndDisabled();
        }
    }

    public void DrawRpSettingsContent()
    {
        // Added ##RPTab suffix for unique ID
        ImGui.Checkbox("Enable##RPTab", ref rpSettings.enabled);
        if (!rpSettings.enabled)
        {
            ImGui.BeginDisabled();
        }

        ImGui.Separator();
        ImGui.Text("Text Greeting Settings:");
        // Push unique ID for this section before calling TextSettings
        ImGui.PushID("RPTextSettings");
        TextSettings(ref rpSettings.textSettings);
        ImGui.PopID(); // Pop the ID after TextSettings

        ImGui.Separator();
        ImGui.Text("Macro Greeting Settings:");
        // Push unique ID for this section before calling MacroSettings
        ImGui.PushID("RPMacroSettings");
        MacroSettings(ref rpSettings.macroSettings);
        ImGui.PopID(); // Pop the ID after MacroSettings

        ImGui.Separator();
        ImGui.SetNextItemWidth(120);
        ImGui.SliderFloat("Message Delay", ref rpSettings.messageDelay, 0.0f, 5.0f);
        ImGui.SameLine(); HelpMarker("Delay the greet text when you join a new instance. CTRL+Click for manual input.");

        if (ImGui.TreeNode("Advanced Settings"))
        {
            if (ImGui.TreeNode("Order of Greetings (Drag)"))
            {
                for (int n = 0; n < rpSettings.executeOrder.Length; n++)
                {
                    string item = rpSettings.executeOrder[n];
                    ImGui.Selectable($"{n + 1}. {item}");

                    if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                    {
                        int n_next = n + (ImGui.GetMouseDragDelta(0).Y < 0f ? -1 : 1);
                        if (n_next >= 0 && n_next < rpSettings.executeOrder.Length)
                        {
                            string temp = rpSettings.executeOrder[n_next];
                            rpSettings.executeOrder[n_next] = item;
                            rpSettings.executeOrder[n] = temp;
                            ImGui.ResetMouseDragDelta();
                        }
                    }
                }
                ImGui.TreePop();
            }
            // No other checkboxes in this section, but if there were, they'd need a suffix too.
            ImGui.TreePop();
        }

        if (!rpSettings.enabled)
        {
            ImGui.EndDisabled();
        }
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("Greets"))
        {

            if (ImGui.BeginTabItem("Home (RP) Greetings"))
            {
                DrawRpSettingsContent();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Instance Greetings"))
            {
                DrawInstanceSettingsContent();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Goodbye Settings"))
            {
                GoodbyeSettings();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();

        if (ImGui.Button("Save Configuration"))
        {
            plugin.Config.Save();
        }
    }
}
