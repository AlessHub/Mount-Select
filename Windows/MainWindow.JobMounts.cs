using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;

namespace MountSelect.Windows;

public partial class MainWindow
{
    private int selectedJobIndex = 0;
    private string jobSearchText = "";
    private bool jobComboJustOpened = false;
    private uint? lastTrackedJobId = null;
    
    private int currentMountPage = 0;
    private string mountGridSearchText = "";
    private const int MountsPerRow = 5;
    private const int MountRows = 6;
    private const int MountsPerPage = MountsPerRow * MountRows;
    private const float MountIconSize = 48f;
    
    // multi seat
    private int currentMultiMountPage = 0;
    private string multiMountGridSearchText = "";

    private void DrawJobMountsTab()
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), "💡How to use");
        ImGui.TextWrapped("Select a job, then click mounts to add/remove them from that job's rotation. Darkened icons are disabled.");
        ImGui.Spacing();
        ImGui.TextWrapped("You can use Default to act like a mount roulette for all jobs that don't have any mounts assigned.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var showOnlyJobs = configuration.ShowOnlyJobs;
        if (ImGui.Checkbox("Show only jobs (hide base classes)", ref showOnlyJobs))
        {
            configuration.ShowOnlyJobs = showOnlyJobs;
            configuration.Save();
            selectedJobIndex = -1;
        }
        
        ImGui.SameLine();
        var autoSelectCurrentJob = configuration.AutoSelectCurrentJob;
        if (ImGui.Checkbox("Auto-select current job", ref autoSelectCurrentJob))
        {
            configuration.AutoSelectCurrentJob = autoSelectCurrentJob;
            configuration.Save();
        }

        ImGui.Spacing();

        var jobsRaw = mountService.GetJobsFiltered(configuration.ShowOnlyJobs).OrderBy(j => j.Value).ToList();
        var jobs = new List<KeyValuePair<uint, string>> { new KeyValuePair<uint, string>(0, "Default") };
        jobs.AddRange(jobsRaw);
        
        if (configuration.AutoSelectCurrentJob)
        {
            var currentJobId = mountService.GetCurrentJobId();
            if (currentJobId.HasValue)
            {
                if (lastTrackedJobId != currentJobId.Value)
                {
                    lastTrackedJobId = currentJobId.Value;
                    
                    var jobIndex = jobs.FindIndex(j => j.Key == currentJobId.Value);
                    if (jobIndex > 0)
                    {
                        selectedJobIndex = jobIndex;
                    }
                }
            }
        }
        else
        {
            lastTrackedJobId = null;
        }
        
        var filteredJobs = string.IsNullOrWhiteSpace(jobSearchText)
            ? jobs
            : jobs.Where(j => j.Value.Contains(jobSearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        ImGui.Text("Select Job:");
        
        string currentJobPreview = selectedJobIndex >= 0 && selectedJobIndex < jobs.Count
            ? jobs[selectedJobIndex].Value
            : "Default";

        if (selectedJobIndex > 0 && selectedJobIndex < jobs.Count)
        {
            var selectedJobId = jobs[selectedJobIndex].Key;
            var iconId = mountService.GetJobIconId(selectedJobId);

            if (iconId > 0)
            {
                var icon = Plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrDefault();
                if (icon != null)
                {
                    ImGui.Image(icon.Handle, new Vector2(20, 20));
                    ImGui.SameLine();
                }
            }
        }

        ImGui.SetNextItemWidth(-1);

        if (ImGui.BeginCombo("##JobSelect", currentJobPreview, ImGuiComboFlags.HeightLarge))
        {
            ImGui.SetNextItemWidth(-1);
            if (!jobComboJustOpened)
            {
                ImGui.SetKeyboardFocusHere();
                jobComboJustOpened = true;
            }
            ImGui.InputTextWithHint("##JobSearch", "Search jobs...", ref jobSearchText, 100);
            ImGui.Separator();

            if (ImGui.BeginChild("##JobList", new Vector2(0, 150), false))
            {
                for (int i = 0; i < filteredJobs.Count; i++)
                {
                    var actualIndex = jobs.IndexOf(filteredJobs[i]);
                    bool isSelected = selectedJobIndex == actualIndex;

                    var jobId = filteredJobs[i].Key;

                    // Only show icon for non-default jobs
                    if (jobId > 0)
                    {
                        var iconId = mountService.GetJobIconId(jobId);
                        if (iconId > 0)
                        {
                            var icon = Plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrDefault();
                            if (icon != null)
                            {
                                ImGui.Image(icon.Handle, new Vector2(20, 20));
                                ImGui.SameLine();
                            }
                        }
                    }

                    if (ImGui.Selectable($"{filteredJobs[i].Value}##{i}", isSelected))
                    {
                        selectedJobIndex = actualIndex;
                        jobSearchText = "";
                        currentMountPage = 0;
                        ImGui.CloseCurrentPopup();
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndChild();

            ImGui.EndCombo();
        }
        else
        {
            jobComboJustOpened = false;
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (selectedJobIndex >= 0 && selectedJobIndex < jobs.Count)
        {
            var selectedJobId = jobs[selectedJobIndex].Key;
            var selectedJobName = jobs[selectedJobIndex].Value;

            if (!configuration.JobMountLists.ContainsKey(selectedJobId))
            {
                configuration.JobMountLists[selectedJobId] = new List<uint>();
            }

            var jobMountList = configuration.JobMountLists[selectedJobId];

            ImGui.Text($"Mounts for {selectedJobName} (Click to add/remove):");
            ImGui.SameLine();
            if (ImGui.Button("Reset Mounts"))
            {
                jobMountList.Clear();
                configuration.Save();
            }
            ImGui.Spacing();

            ImGui.SetNextItemWidth(250);
            if (ImGui.InputTextWithHint("##MountGridSearch", "Search mounts...", ref mountGridSearchText, 100))
            {
                currentMountPage = 0;
            }
            ImGui.SameLine();
            if (ImGui.Button("Clear##SearchClear") && !string.IsNullOrWhiteSpace(mountGridSearchText))
            {
                mountGridSearchText = "";
                currentMountPage = 0;
            }
            ImGui.Spacing();

            if (sortedOwnedMountsCache == null)
            {
                var mountsDict = mountService.GetOwnedMounts();
                sortedOwnedMountsCache = mountsDict.OrderBy(m => m.Key).ToList();
            }
            var allMounts = sortedOwnedMountsCache;
            
            var mounts = string.IsNullOrWhiteSpace(mountGridSearchText)
                ? allMounts
                : allMounts.Where(m => m.Value.Contains(mountGridSearchText, StringComparison.OrdinalIgnoreCase)).ToList();

            int totalPages = (int)Math.Ceiling((double)mounts.Count / MountsPerPage);
            int startIndex = currentMountPage * MountsPerPage;
            int endIndex = Math.Min(startIndex + MountsPerPage, mounts.Count);
            
            if (currentMountPage >= totalPages && totalPages > 0)
            {
                currentMountPage = totalPages - 1;
            }

            if (totalPages > 1)
            {
                if (ImGui.Button("◀"))
                {
                    currentMountPage = Math.Max(0, currentMountPage - 1);
                }
                
                ImGui.SameLine();
                
                for (int page = 0; page < totalPages; page++)
                {
                    if (page > 0)
                        ImGui.SameLine();
                    
                    bool isCurrentPage = page == currentMountPage;
                    
                    if (isCurrentPage)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.5f, 0.8f, 1.0f));
                    }
                    
                    if (ImGui.Button($"{page + 1}##page{page}"))
                    {
                        currentMountPage = page;
                    }
                    
                    if (isCurrentPage)
                    {
                        ImGui.PopStyleColor();
                    }
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("▶"))
                {
                    currentMountPage = Math.Min(totalPages - 1, currentMountPage + 1);
                }
                
                ImGui.Spacing();
            }

            for (int row = 0; row < MountRows; row++)
            {
                for (int col = 0; col < MountsPerRow; col++)
                {
                    int mountIndex = startIndex + (row * MountsPerRow) + col;
                    if (mountIndex >= endIndex)
                    {
                        continue;
                    }

                    var mount = mounts[mountIndex];
                    var mountId = mount.Key;
                    var mountName = mount.Value;
                    bool isEnabled = jobMountList.Contains(mountId);

                    var iconId = mountService.GetMountIconId(mountId);
                    var texture = iconId > 0 
                        ? Plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrDefault()
                        : null;

                    if (col > 0) ImGui.SameLine();

                    ImGui.BeginGroup();

                    if (texture is not null)
                    {
                        if (ImGui.ImageButton(texture.Handle, new Vector2(MountIconSize, MountIconSize)))
                        {
                            if (isEnabled)
                            {
                                jobMountList.Remove(mountId);
                            }
                            else
                            {
                                jobMountList.Add(mountId);
                            }
                            configuration.Save();
                        }

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(mountName);
                        }
                    }
                    else
                    {
                        if (ImGui.Button($"?##mount{mountId}", new Vector2(MountIconSize, MountIconSize)))
                        {
                            if (isEnabled)
                            {
                                jobMountList.Remove(mountId);
                            }
                            else
                            {
                                jobMountList.Add(mountId);
                            }
                            configuration.Save();
                        }

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(mountName);
                        }
                    }

                    if (!isEnabled)
                    {
                        var drawList = ImGui.GetWindowDrawList();
                        var iconMin = ImGui.GetItemRectMin();
                        var iconMax = ImGui.GetItemRectMax();
                        
                        drawList.AddRectFilled(
                            iconMin,
                            iconMax,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.6f))
                        );
                    }

                    ImGui.EndGroup();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text($"Selected Mounts: {jobMountList.Count}");
            if (jobMountList.Count > 0)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), "(Click to remove)");
            }
            
            if (jobMountList.Count > 0)
            {
                ImGui.Spacing();
                const float smallIconSize = 32f;
                const int iconsPerRow = 10;
                
                var mountsToRemove = new List<uint>();
                
                for (int i = 0; i < jobMountList.Count; i++)
                {
                    var mountId = jobMountList[i];
                    var mountName = mountService.GetMountName(mountId);
                    
                    var iconId = mountService.GetMountIconId(mountId);
                    var texture = iconId > 0 
                        ? Plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrDefault()
                        : null;

                    if (i > 0 && i % iconsPerRow != 0)
                        ImGui.SameLine();
                    
                    // push unique ID to avoid conflicts, otherwise some buttons might not respond
                    
                    ImGui.PushID($"smallmount_{mountId}_{i}");
                    
                    if (texture != null)
                    {
                        if (ImGui.ImageButton(texture.Handle, new Vector2(smallIconSize, smallIconSize)))
                        {
                            mountsToRemove.Add(mountId);
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip($"{mountName}\nClick to remove");
                        }
                    }
                    else
                    {
                        if (ImGui.Button("?", new Vector2(smallIconSize, smallIconSize)))
                        {
                            mountsToRemove.Add(mountId);
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip($"{mountName}\nClick to remove");
                        }
                    }
                    
                    ImGui.PopID();
                }
                
                foreach (var mountId in mountsToRemove)
                {
                    jobMountList.Remove(mountId);
                }
                
                if (mountsToRemove.Count > 0)
                {
                    configuration.Save();
                }
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Select a job above to manage its mount rotation.");
        }
    }
}
