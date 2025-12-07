using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using MountSelect.Configuration;
using MountSelect.Services;

namespace MountSelect.Windows;

public partial class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly PluginConfiguration configuration;
    private readonly MountService mountService;

    public ConfigWindow(Plugin plugin, MountService mountService) : base("Mount Select Configuration###MountSelectConfigWindow")
    {
        this.Size = new Vector2(500, 400);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        this.configuration = plugin.Configuration;
        this.mountService = mountService;
    }

    public void Dispose()
    {
        // cleanup if needed
    }

    public override void Draw()
    {
        ImGui.Text("Keybind Configuration");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped("Configure default mounts in the Job Mounts tab (select 'Default' from the job dropdown).");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // keybinds
        DrawKeybindsSection();

        ImGui.Spacing();
        
    
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), "💡 Alternative: Use Macros Instead of Keybinds");
        ImGui.TextWrapped("If you prefer not to use keybinds, you can create in-game macros and bind them to hotbar keys:");
        ImGui.Spacing();


        ImGui.BulletText("/qmount - Summons a random mount from your current job's rotation.");
        ImGui.BulletText("/multimount - Summons a random multi-seat mount from your favorites.");
        ImGui.BulletText("/mountselect - Open mount selection window");
        ImGui.BulletText("/mountconfig - Open this configuration window");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.Button("Open Macro Window"))
        {
            unsafe
            {
                AgentModule.Instance()->GetAgentByInternalId(AgentId.Macro)->Show();
            }
        }
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), "(Opens in-game macro editor)");
    }
}