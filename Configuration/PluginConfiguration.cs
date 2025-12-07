using Dalamud.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MountSelect.Configuration;

[Serializable]
public class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ShowInChat { get; set; } = true;
    public bool ShowMainWindow { get; set; } = true;
    public bool ShowMountButton { get; set; } = true;
    public bool AutoSelectFavoriteMount { get; set; } = false;
    public uint FavoriteMountId { get; set; } = 0;


    public Dictionary<uint, uint> ClassMountMappings { get; set; } = new();
    public Dictionary<uint, List<uint>> JobMountLists { get; set; } = new();
    public uint DefaultMountId { get; set; } = 0;
    public bool ShowOnlyJobs { get; set; } = false;
    public bool AutoSelectCurrentJob { get; set; } = true;
    public List<uint> MultiSeatMounts { get; set; } = new();

    public bool EnableMountKeybind { get; set; } = false;
    public int MountKeybindKey { get; set; } = 0;
    public bool MountKeybindCtrl { get; set; } = false;
    public bool MountKeybindAlt { get; set; } = false;
    public bool MountKeybindShift { get; set; } = false;

    public bool EnableMultiMountKeybind { get; set; } = false;
    public int MultiMountKeybindKey { get; set; } = 0;
    public bool MultiMountKeybindCtrl { get; set; } = false;
    public bool MultiMountKeybindAlt { get; set; } = false;
    public bool MultiMountKeybindShift { get; set; } = false;
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}