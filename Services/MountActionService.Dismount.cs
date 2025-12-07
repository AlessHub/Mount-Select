using System;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace MountSelect.Services;

public partial class MountActionService
{
    public unsafe bool Dismount()
    {
        try
        {
            if (!IsMounted())
                return false;

            var actionManager = ActionManager.Instance();
            if (actionManager == null)
            {
                pluginLog.Error("Failed to get ActionManager instance for dismount");
                return false;
            }
            
            var success = actionManager->UseAction(ActionType.Mount, 0);
            
            if (!success)
            {
                pluginLog.Warning("Failed to dismount");
            }

            return success;
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, "Error dismounting");
            return false;
        }
    }
}
