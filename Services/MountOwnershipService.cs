using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace MountSelect.Services;

public class MountOwnershipService
{
    private readonly IPluginLog pluginLog;
    private readonly Dictionary<uint, bool> mountOwnershipCache = new();

    public MountOwnershipService(IPluginLog pluginLog)
    {
        this.pluginLog = pluginLog;
    }

    public unsafe bool IsMountOwned(uint mountId)
    {
        if (mountOwnershipCache.TryGetValue(mountId, out bool cached))
            return cached;

        try
        {
            var playerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance();
            if (playerState == null)
            {
                pluginLog.Warning($"PlayerState is null when checking mount {mountId}");
                return false;
            }

            bool isOwned = playerState->IsMountUnlocked(mountId);
            
            mountOwnershipCache[mountId] = isOwned;
            return isOwned;
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, $"Error checking ownership for mount {mountId}");
            return false;
        }
    }

    public Dictionary<uint, string> FilterOwnedMounts(Dictionary<uint, string> allMounts)
    {
        return allMounts
            .Where(kvp => IsMountOwned(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
