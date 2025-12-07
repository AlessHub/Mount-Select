using Dalamud.Game.ClientState.Conditions;

namespace MountSelect.Services;

public partial class MountActionService
{
    private bool CanMount()
    {
        // probably missing something here
        // have yet to figure out how to check for chatting state
        // currently user can mount if they use a keybind while chatting

        return clientState.IsLoggedIn &&
               !condition[ConditionFlag.InCombat] &&
               !condition[ConditionFlag.Casting] &&
               !condition[ConditionFlag.BetweenAreas] &&
               !condition[ConditionFlag.OccupiedInCutSceneEvent] &&
               !condition[ConditionFlag.WatchingCutscene] &&
               !condition[ConditionFlag.OccupiedInQuestEvent] &&
               !condition[ConditionFlag.Mounted]; 
    }

    public bool IsMounted()
    {
        return condition[ConditionFlag.Mounted];
    }
}