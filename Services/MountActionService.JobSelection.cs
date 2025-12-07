using MountSelect.Configuration;
using System;

namespace MountSelect.Services;

public partial class MountActionService
{
    public bool SummonMountForCurrentJob(PluginConfiguration config)
    {
        var currentJobId = mountService.GetCurrentJobId();
        if (currentJobId == null)
        {
            if (config.ShowInChat)
            {
                chatGui.PrintError("Could not determine current job!");
            }
            return false;
        }

        uint mountId = 0;

        if (config.JobMountLists.TryGetValue(currentJobId.Value, out var mountList) && mountList.Count > 0)
        {
            var random = new Random();
            mountId = mountList[random.Next(mountList.Count)];
            return SummonMount(mountId, config);
        }
        else if (config.JobMountLists.TryGetValue(0, out var defaultMountList) && defaultMountList.Count > 0)
        {
            var random = new Random();
            mountId = defaultMountList[random.Next(defaultMountList.Count)];
            return SummonMount(mountId, config);
        }
        else
        {
            return false;
        }
    }
}
