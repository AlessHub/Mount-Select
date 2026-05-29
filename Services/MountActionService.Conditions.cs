using Dalamud.Game.ClientState.Conditions;

namespace MountSelect.Services;

public partial class MountActionService
{
    private bool CanMount()
    {
        // probably missing something here
        // have yet to figure out how to check for chatting state
        // currently user can mount if they use a keybind while chatting

        // In PvP (BoundByDuty), allow mounting even when InCombat flag is active
        // This matches the game's native behavior for PvP mounting
        var isPvP = condition[ConditionFlag.BoundByDuty];
        var canMountInCombat = isPvP && condition[ConditionFlag.InCombat];

        return clientState.IsLoggedIn &&
               (canMountInCombat || !condition[ConditionFlag.InCombat]) &&
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