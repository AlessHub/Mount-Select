using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using MountSelect.Configuration;
using MountSelect.Services;

namespace MountSelect.Windows;

public partial class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly PluginConfiguration configuration;
    private readonly MountService mountService;
    private readonly MountActionService mountActionService;
    
    private static List<KeyValuePair<uint, string>>? sortedOwnedMountsCache;
    private static List<KeyValuePair<uint, string>>? sortedMultiSeatMountsCache;

    public MainWindow(Plugin plugin, MountService mountService, MountActionService mountActionService) 
        : base("Mount Select###MountSelectMainWindow")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        this.configuration = plugin.Configuration;
        this.mountService = mountService;
        this.mountActionService = mountActionService;
    }

    public void Dispose()
    {
        // cleanup if needed
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("MountSelectTabs"))
        {
            if (ImGui.BeginTabItem("Job Mounts"))
            {
                DrawJobMountsTab();
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("Multi-Seat Favorites"))
            {
                DrawMultiSeatTab();
                ImGui.EndTabItem();
            }
            
            ImGui.EndTabBar();
        }
    }
}
