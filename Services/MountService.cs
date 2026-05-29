using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace MountSelect.Services;

public class MountService
{
    private readonly IDataManager dataManager;
    private readonly IClientState clientState;
    private readonly IPluginLog pluginLog;
    private readonly MountOwnershipService ownershipService;
    private readonly IObjectTable objectTable;

    private Dictionary<uint, string>? mountCache;
    private Dictionary<uint, string>? jobCache;
    private Dictionary<uint, uint>? mountToActionMap;
    private Dictionary<uint, string>? multiSeatMountCache;
    private Dictionary<uint, string>? ownedMountsCache;

    public MountService(IDataManager dataManager, IClientState clientState, IPluginLog pluginLog, MountOwnershipService ownershipService, IObjectTable objectTable)
    {
        this.dataManager = dataManager;
        this.clientState = clientState;
        this.pluginLog = pluginLog;
        this.ownershipService = ownershipService;
        this.objectTable = objectTable;
    }

    public Dictionary<uint, string> GetAllMounts()
    {
        if (mountCache == null)
        {
            mountCache = new Dictionary<uint, string>();

            try
            {
                // we use lumina instead of just a json now, but we have a fallback in case lumina fails
                
                var mountSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Mount>();
                if (mountSheet != null)
                {
                    var candidateNameProps = new[] { "Name", "Singular", "SingularRaw", "SingularText", "Name_en", "DisplayName" };

                    foreach (var row in mountSheet)
                    {
                        try
                        {
                            var id = row.RowId;
                            string? name = null;
                            var t = row.GetType();
                            foreach (var p in candidateNameProps)
                            {
                                var prop = t.GetProperty(p);
                                if (prop == null) continue;
                                try
                                {
                                    var v = prop.GetValue(row);
                                    if (v == null) continue;
                                    name = v.ToString();
                                    if (!string.IsNullOrEmpty(name)) break;
                                }
                                catch { }
                            }

                            if (!string.IsNullOrEmpty(name))
                            {
                                mountCache[id] = CapitalizeMountName(name!);
                            }
                        }
                        catch { /* ignore individual row parsing issues */ }
                    }
                }
                else
                {
                    pluginLog.Warning("Lumina Mount sheet was null, falling back to embedded JSON");
                }
            }
            catch (Exception ex)
            {
                pluginLog.Error(ex, "Failed to read Lumina Mount sheet");
            }

            // fallback
            if (mountCache.Count == 0)
            {
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "MountSelect.Data.mounts.json";

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        var data = JObject.Parse(json);
                        var mounts = data["mounts"];

                        if (mounts != null)
                        {
                            foreach (var mount in mounts)
                            {
                                var property = (JProperty)mount;
                                if (uint.TryParse(property.Name, out uint id))
                                {
                                    mountCache[id] = property.Value.ToString();
                                }
                            }
                        }
                    }

                    pluginLog.Info($"Loaded {mountCache.Count} mounts from embedded JSON data (fallback)");
                }
                catch (Exception ex)
                {
                    pluginLog.Error(ex, "Failed to load mount data from JSON. Using minimal fallback data.");

                    mountCache = new Dictionary<uint, string>
                    {
                        { 1, "Company Chocobo" },
                        { 9, "Ahriman" },
                        { 22, "Nightmare" }
                    };
                }
            }
        }

        return mountCache;
    }

    public Dictionary<uint, string> GetAllJobs()
    {
        if (jobCache == null)
        {
            jobCache = new Dictionary<uint, string>();

            try
            {
                var classJobSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>();

                if (classJobSheet != null)
                {
                    foreach (var job in classJobSheet)
                    {
                        var jobName = job.Name.ToString();
                        if (!string.IsNullOrEmpty(jobName))
                        {
                            jobCache[job.RowId] = CapitalizeJobName(jobName);
                        }
                    }
                }
                else
                {
                    pluginLog.Warning("ClassJob sheet was null, using fallback");
                    LoadFallbackJobs();
                }
            }
            catch (Exception ex)
            {
                pluginLog.Error(ex, "Failed to load job data from game files. Using fallback.");
                LoadFallbackJobs();
            }
        }

        return jobCache;
    }

    private void LoadFallbackJobs()
    {
        jobCache = new Dictionary<uint, string>
        {
            { 19, "Paladin" }, { 20, "Monk" }, { 21, "Warrior" }, { 22, "Dragoon" },
            { 23, "Bard" }, { 24, "White Mage" }, { 25, "Black Mage" }, { 27, "Summoner" },
            { 28, "Scholar" }, { 30, "Ninja" }, { 31, "Machinist" }, { 32, "Dark Knight" },
            { 33, "Astrologian" }, { 34, "Samurai" }, { 35, "Red Mage" }, { 36, "Blue Mage" },
            { 37, "Gunbreaker" }, { 38, "Dancer" }, { 39, "Reaper" }, { 40, "Sage" },
            { 41, "Viper" }, { 42, "Pictomancer" }
        };
        pluginLog.Info($"Loaded {jobCache.Count} jobs (fallback list)");
    }

    public uint GetJobIconId(uint jobId)
    {
        try
        {
            var classJobSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>();
            if (classJobSheet != null && classJobSheet.TryGetRow(jobId, out var job))
            {
                var iconField = job.GetType().GetProperty("Icon");
                if (iconField != null)
                {
                    var value = iconField.GetValue(job);
                    if (value is byte iconByte)
                        return iconByte;
                    if (value is uint iconUint)
                        return iconUint;
                }
                return 062000u + jobId;
            }
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, $"Failed to get icon for job {jobId}");
        }

        return 0;
    }

    public uint? GetCurrentJobId()
    {
        return objectTable.LocalPlayer?.ClassJob.RowId;
    }

    public string GetJobName(uint jobId)
    {
        GetAllJobs();
        return jobCache != null && jobCache.TryGetValue(jobId, out var jobName)
            ? jobName
            : "Unknown";
    }

    public string GetMountName(uint mountId)
    {
        GetAllMounts();
        return mountCache != null && mountCache.TryGetValue(mountId, out var mountName)
            ? mountName
            : "Unknown Mount";
    }

    public uint GetMountIconId(uint mountId)
    {
        try
        {
            var mountSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Mount>();
            if (mountSheet != null && mountSheet.TryGetRow(mountId, out var mount))
            {
                var iconProp = mount.GetType().GetProperty("Icon");
                if (iconProp != null)
                {
                    var iconValue = iconProp.GetValue(mount);
                    
                    if (iconValue is ushort iconUShort && iconUShort > 0)
                        return iconUShort;
                    if (iconValue is uint iconUint && iconUint > 0)
                        return iconUint;
                    if (iconValue is int iconInt && iconInt > 0)
                        return (uint)iconInt;
                    if (iconValue is short iconShort && iconShort > 0)
                        return (uint)iconShort;
                }
            }
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, $"Failed to get icon for mount {mountId}");
        }

        return 0;
    }

    public bool HasMount(uint mountId)
    {
        GetAllMounts();
        return mountCache != null && mountCache.ContainsKey(mountId);
    }

    public Dictionary<uint, string> GetJobNames()
    {
        return GetAllJobs();
    }

    public Dictionary<uint, string> GetMountNames()
    {
        return GetAllMounts();
    }

    private void EnsureMountActionMap()
    {
        if (mountToActionMap != null) return;
        mountToActionMap = new Dictionary<uint, uint>();

        try
        {
            var mountSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Mount>();
            pluginLog.Info($"Mount sheet is null: {mountSheet == null}");

            if (mountSheet != null)
            {
                try
                {
                    pluginLog.Info("Attempting to enumerate Mount sheet rows...");
                    int rowCount = 0;
                    foreach (var row in mountSheet)
                    {
                        rowCount++;
                        pluginLog.Info($"Found row with RowId: {row.RowId}");
                        if (row.RowId != 0)
                        {
                            pluginLog.Info("Getting properties from row...");
                            var allProps = row.GetType().GetProperties()
                                .Select(p => $"{p.Name}({p.PropertyType.Name})")
                                .OrderBy(n => n)
                                .ToList();
                            pluginLog.Info($"Mount sheet has {allProps.Count} properties");
                            pluginLog.Info($"Mount sheet properties: {string.Join(", ", allProps)}");
                            break;
                        }
                    }
                    pluginLog.Info($"Enumerated {rowCount} rows from Mount sheet");
                }
                catch (Exception ex)
                {
                    pluginLog.Error(ex, "Failed to enumerate Mount sheet properties");
                }

                var mountActionProp = typeof(Lumina.Excel.Sheets.Mount).GetProperty("MountAction");

                foreach (var row in mountSheet)
                {
                    uint mountId = row.RowId;

                    if (mountActionProp != null)
                    {
                        try
                        {
                            var val = mountActionProp.GetValue(row);
                            if (val != null)
                            {
                                var rowIdProp = val.GetType().GetProperty("RowId");
                                if (rowIdProp != null)
                                {
                                    var actionId = rowIdProp.GetValue(val);
                                    if (actionId is uint u && u != 0)
                                    {
                                        mountToActionMap[mountId] = u;
                                    }
                                }
                            }
                        }
                        catch { /* ignore property read errors */ }
                    }
                }
            }

            pluginLog.Info($"Built mount->action mapping: {mountToActionMap.Count} entries");
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, "Failed to build mount->action mapping");
        }
    }

    public Dictionary<uint, string> GetOwnedMounts()
    {
        if (ownedMountsCache != null)
            return ownedMountsCache;

        var allMounts = GetAllMounts();
        ownedMountsCache = ownershipService.FilterOwnedMounts(allMounts);
        return ownedMountsCache;
    }

    private string CapitalizeJobName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(name.ToLower());
    }

    private string CapitalizeMountName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(name.ToLower());
    }

    public bool IsBaseClass(uint jobId)
    {

        var baseClassIds = new HashSet<uint>
        {
            1,  // Gladiator -> Paladin
            2,  // Pugilist -> Monk
            3,  // Marauder -> Warrior
            4,  // Lancer -> Dragoon
            5,  // Archer -> Bard
            6,  // Conjurer -> White Mage
            7,  // Thaumaturge -> Black Mage
            26, // Arcanist -> Summoner/Scholar
            29, // Rogue -> Ninja
        };

        return baseClassIds.Contains(jobId);
    }

    public Dictionary<uint, string> GetJobsFiltered(bool showOnlyJobs)
    {
        var allJobs = GetAllJobs();

        if (!showOnlyJobs)
            return allJobs;

        return allJobs.Where(j => !IsBaseClass(j.Key)).ToDictionary(j => j.Key, j => j.Value);
    }

    public Dictionary<uint, string> GetMultiSeatMounts()
    {
        if (multiSeatMountCache != null)
            return multiSeatMountCache;

        multiSeatMountCache = new Dictionary<uint, string>();

        try
        {
            var mountSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Mount>();
            var ownedMounts = GetOwnedMounts();

            if (mountSheet != null)
            {
                foreach (var mountId in ownedMounts.Keys)
                {
                    if (mountSheet.TryGetRow(mountId, out var mountData))
                    {
                        var extraSeatsField = mountData.GetType().GetProperty("ExtraSeats");
                        if (extraSeatsField != null)
                        {
                            var extraSeats = extraSeatsField.GetValue(mountData);
                            if (extraSeats is byte seatsCount && seatsCount > 0)
                            {
                                multiSeatMountCache[mountId] = ownedMounts[mountId];
                            }
                            else if (extraSeats is int seatsCountInt && seatsCountInt > 0)
                            {
                                multiSeatMountCache[mountId] = ownedMounts[mountId];
                            }
                        }
                    }
                }
            }
            else
            {
                pluginLog.Warning("Mount sheet was null, returning empty multi-seat list");
            }
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, "Failed to load multi-seat mount data from game files");
        }

        if (multiSeatMountCache.Count == 0)
        {
            var ownedMounts = GetOwnedMounts();
            var knownMultiSeatIds = new uint[] { 25, 27, 66, 151, 245, 247, 275, 293 };

            foreach (var id in knownMultiSeatIds)
            {
                if (ownedMounts.TryGetValue(id, out var name))
                {
                    multiSeatMountCache[id] = name;
                }
            }
        }

        return multiSeatMountCache;
    }
}