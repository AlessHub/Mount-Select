using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using MountSelect.Configuration;
using System;

namespace MountSelect.Services;

public partial class MountActionService
{
    private readonly IPluginLog pluginLog;
    private readonly IChatGui chatGui;
    private readonly ICondition condition;
    private readonly IClientState clientState;
    private readonly MountService mountService;
    private readonly ICommandManager commandManager;

    public MountActionService(
        IPluginLog pluginLog,
        IChatGui chatGui,
        ICondition condition,
        IClientState clientState,
        MountService mountService,
        ICommandManager commandManager)
    {
        this.pluginLog = pluginLog;
        this.chatGui = chatGui;
        this.condition = condition;
        this.clientState = clientState;
        this.mountService = mountService;
        this.commandManager = commandManager;
    }

    public unsafe bool SummonMount(uint mountId, PluginConfiguration config)
    {
        try
        {
            if (!CanMount())
            {
                if (config.ShowInChat)
                {
                    chatGui.PrintError("Cannot mount right now!");
                }
                return false;
            }

            if (!mountService.HasMount(mountId))
            {
                if (config.ShowInChat)
                {
                    chatGui.PrintError($"Mount with ID {mountId} not found!");
                }
                return false;
            }

            var actionManager = ActionManager.Instance();
            if (actionManager == null)
            {
                pluginLog.Error("Failed to get ActionManager instance");
                return false;
            }

            var success = actionManager->UseAction(ActionType.Mount, mountId);

            if (!success)
            {
                if (config.ShowInChat)
                {
                    chatGui.PrintError($"Failed to summon mount!");
                }
                pluginLog.Warning($"Failed to summon mount with ID {mountId}");
            }

            return success;
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, "Error summoning mount");
            return false;
        }
    }
}