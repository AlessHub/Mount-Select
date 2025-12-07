using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;

namespace MountSelect.Windows;

public partial class MainWindow
{
    private void DrawMultiSeatTab()
    {
        ImGui.TextWrapped("Click mounts to add/remove them from your multi-seat favorites. Use /multimount to summon a random one! Darkened icons are disabled.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Multi-Seat Mount Favorites (Click to add/remove):");
        ImGui.Spacing();

        ImGui.SetNextItemWidth(250);
        if (ImGui.InputTextWithHint("##MultiMountGridSearch", "Search mounts...", ref multiMountGridSearchText, 100))
        {
            currentMultiMountPage = 0;
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear##MultiSearchClear") && !string.IsNullOrWhiteSpace(multiMountGridSearchText))
        {
            multiMountGridSearchText = "";
            currentMultiMountPage = 0;
        }
        ImGui.Spacing();

        if (sortedMultiSeatMountsCache == null)
        {
            var mountsDict = mountService.GetMultiSeatMounts();
            sortedMultiSeatMountsCache = mountsDict.OrderBy(m => m.Key).ToList();
        }
        var allMounts = sortedMultiSeatMountsCache;
        
        var mounts = string.IsNullOrWhiteSpace(multiMountGridSearchText)
            ? allMounts
            : allMounts.Where(m => m.Value.Contains(multiMountGridSearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        int totalPages = (int)Math.Ceiling((double)mounts.Count / MountsPerPage);
        int startIndex = currentMultiMountPage * MountsPerPage;
        int endIndex = Math.Min(startIndex + MountsPerPage, mounts.Count);
        
        if (currentMultiMountPage >= totalPages && totalPages > 0)
        {
            currentMultiMountPage = totalPages - 1;
        }

        if (totalPages > 1)
        {
            if (ImGui.Button("◀##MultiPrev"))
            {
                currentMultiMountPage = Math.Max(0, currentMultiMountPage - 1);
            }
            
            ImGui.SameLine();
            for (int page = 0; page < totalPages; page++)
            {
                if (page > 0)
                    ImGui.SameLine();
                
                bool isCurrentPage = page == currentMultiMountPage;
                
                if (isCurrentPage)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.5f, 0.8f, 1.0f));
                }
                
                if (ImGui.Button($"{page + 1}##multipage{page}"))
                {
                    currentMultiMountPage = page;
                }
                
                if (isCurrentPage)
                {
                    ImGui.PopStyleColor();
                }
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("▶##MultiNext"))
            {
                currentMultiMountPage = Math.Min(totalPages - 1, currentMultiMountPage + 1);
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
                bool isEnabled = configuration.MultiSeatMounts.Contains(mountId);

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
                            configuration.MultiSeatMounts.Remove(mountId);
                        }
                        else
                        {
                            configuration.MultiSeatMounts.Add(mountId);
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
                    if (ImGui.Button($"?##multimount{mountId}", new Vector2(MountIconSize, MountIconSize)))
                    {
                        if (isEnabled)
                        {
                            configuration.MultiSeatMounts.Remove(mountId);
                        }
                        else
                        {
                            configuration.MultiSeatMounts.Add(mountId);
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
        ImGui.Text($"Selected Multi-Seat Mounts: {configuration.MultiSeatMounts.Count}");
    }
}
