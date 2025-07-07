﻿using DotNext.Collections.Generic;
using Microsoft.Extensions.Logging;
using SoulsFormats;
using StudioCore.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudioCore.TextEditor;

[JsonSourceGenerationOptions(WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Metadata, IncludeFields = true)]
[JsonSerializable(typeof(JsonFMG))]
internal partial class FmgSerializerContext : JsonSerializerContext
{
}

[Obsolete]
public class JsonFMG
{
    public FMG Fmg;
    public FmgIDType FmgID;

    [JsonConstructor]
    public JsonFMG()
    {
    }

    public JsonFMG(FmgIDType fmg_id, FMG fmg)
    {
        FmgID = fmg_id;
        Fmg = fmg;
    }
}

/// <summary>
///     Imports and exports FMGs using external formats.
/// </summary>
public static class FmgExporter
{
    private const string _entrySeparator = "###";

    private static Dictionary<FmgIDType, FMG> GetFmgs(FMGBank bank, string msgBndPath)
    {
        Dictionary<FmgIDType, FMG> fmgs = new();
        IBinder fmgBinder;
        if (bank.Project.Type is GameType.DemonsSouls or GameType.DarkSoulsPTDE
            or GameType.DarkSoulsRemastered)
        {
            fmgBinder = BND3.Read(msgBndPath);
        }
        else
        {
            fmgBinder = BND4.Read(msgBndPath);
        }
        foreach (var file in fmgBinder.Files)
        {
            var fmg = FMG.Read(file.Bytes);
            fmgs.Add((FmgIDType)file.ID, fmg);
        }

        fmgBinder.Dispose();

        return fmgs;
    }

    private static string FormatJson(string json)
    {
        json = json.Replace("{\"ID\"", "\r\n{\"ID\"");
        json = json.Replace("],", "\r\n],");
        return json;
    }

    /// <summary>
    ///     Exports jsons that only contains entries that differ between game and mod directories.
    /// </summary>
    public static void ExportFmgTxt(FMGLanguage lang, bool moddedOnly)
    {
        if (!PlatformUtils.Instance.OpenFolderDialog("Choose Export Folder", out var path))
        {
            return;
        }

        if (lang.Owner.Project.ParentProject == null)
        {
            TaskLogs.AddLog("Error: Project has no parent to compare to. Cannot export modded files without vanilla FMGs to compare to.",
                LogLevel.Warning, TaskLogs.LogPriority.High);
            return;
        }

        if (moddedOnly && lang.Owner.Project.AssetLocator.RootDirectory == lang.Owner.Project.ParentProject.AssetLocator.RootDirectory)
        {
            TaskLogs.AddLog("Error: Game directory is identical to mod directory or project has no parent. Cannot export modded files without vanilla FMGs to compare to.",
                LogLevel.Warning, TaskLogs.LogPriority.High);
            return;
        }

        var itemPath = Locator.AssetLocator.GetItemMsgbnd(lang.LanguageFolder).AssetPath;
        var menuPath = Locator.AssetLocator.GetMenuMsgbnd(lang.LanguageFolder).AssetPath;
        var itemPath_Vanilla = itemPath.Replace(lang.Owner.Project.AssetLocator.RootDirectory, lang.Owner.Project.ParentProject.AssetLocator.RootDirectory);
        var menuPath_Vanilla = menuPath.Replace(lang.Owner.Project.AssetLocator.RootDirectory, lang.Owner.Project.ParentProject.AssetLocator.RootDirectory);

        Dictionary<FmgIDType, FMG> fmgs_vanilla = new();
        fmgs_vanilla.AddAll(GetFmgs(lang.Owner, itemPath_Vanilla));
        fmgs_vanilla.AddAll(GetFmgs(lang.Owner, menuPath_Vanilla));

        Dictionary<FmgIDType, FMG> fmgs_mod = new();
        foreach (var info in lang._FmgInfoBanks.SelectMany((x) => x.Value.FmgInfos))
        {
            fmgs_mod.Add(info.FmgID, info.Fmg);
        }

        Dictionary<FmgIDType, FMG> fmgs_out;

        if (!moddedOnly)
        {
            // Export all entries
            fmgs_out = fmgs_mod;
        }
        else
        {
            // Export modded entries only
            fmgs_out = new();
            foreach (var kvp in fmgs_mod)
            {
                var fmg_mod = kvp.Value;
                var entries_vanilla = fmgs_vanilla[kvp.Key].Entries.ToList();
                FMG entries_out = new(fmg_mod.Version);

                foreach (var entry in fmg_mod.Entries)
                {
                    FMG.Entry entry_vanilla = null;
                    for (var i = 0; i < entries_vanilla.Count; i++)
                    {
                        if (entries_vanilla[i].ID == entry.ID)
                        {
                            entry_vanilla = entries_vanilla[i];
                            entries_vanilla.RemoveAt(i);
                            break;
                        }
                    }

                    if (entry_vanilla != null && entry.Text == entry_vanilla.Text)
                    {
                        continue;
                    }

                    entries_out.Entries.Add(entry);
                }

                if (entries_out.Entries.Count > 0)
                {
                    fmgs_out.Add(kvp.Key, entries_out);
                }
            }
        }

        if (fmgs_out.Count == 0)
        {
            TaskLogs.AddLog("All FMG entries in mod folder are identical to game folder. No files have been exported.",
                LogLevel.Information, TaskLogs.LogPriority.High);
            return;
        }

        foreach (var kvp in fmgs_out)
        {
            var fileName = kvp.Key.ToString();
            Dictionary<string, HashSet<int>> sharedText = new();
            foreach (var entry in kvp.Value.Entries)
            {
                // Combine shared text

                if (entry.Text == null)
                    continue;

                entry.Text = entry.Text.TrimEnd('\n');

                if (!sharedText.TryGetValue(entry.Text, out var ids))
                {
                    sharedText[entry.Text] = ids = new();
                }
                ids.Add(entry.ID);
            }

            List<string> output = [];
            output.Add($"###ID {(int)kvp.Key}");
            foreach (var sharedKvp in sharedText)
            {
                var text = sharedKvp.Key;
                HashSet<int> ids = sharedKvp.Value;

                text = text.Replace("\r", "");
                text = text.TrimEnd('\n');

                output.Add("");
                string idsString = "";
                foreach (var id in ids)
                {
                    idsString += _entrySeparator + id.ToString();
                }
                output.Add(idsString);
                output.AddRange(text.Split("\n"));
            }

            File.WriteAllLines($@"{path}\{fileName}.fmgmerge.txt", output);
        }

        TaskLogs.AddLog("Finished exporting FMG txt files",
            LogLevel.Information, TaskLogs.LogPriority.High);
    }

    [Obsolete]
    public static bool ExportJsonFMGs(FMGBank bank)
    {
        if (!PlatformUtils.Instance.OpenFolderDialog("Choose Export Folder", out var path))
        {
            return false;
        }

        var filecount = 0;
        if (bank.Project.Type == GameType.DarkSoulsIISOTFS)
        {
            Directory.CreateDirectory(path);

            foreach (FMGInfo info in bank.FmgInfoBank)
            {
                JsonFMG fmgPair = new(info.FmgID, info.Fmg);
                var json = JsonSerializer.Serialize(fmgPair, FmgSerializerContext.Default.JsonFMG);
                json = FormatJson(json);

                var fileName = info.Name;
                if (CFG.Current.FMG_ShowOriginalNames)
                {
                    fileName = info.FileName;
                }

                File.WriteAllText($@"{path}\{fileName}.fmg.json", json);

                filecount++;
            }
        }
        else
        {
            var itemPath = $@"{path}\Item Text";
            var menuPath = $@"{path}\Menu Text";
            Directory.CreateDirectory(itemPath);
            Directory.CreateDirectory(menuPath);
            foreach (FMGInfo info in bank.FmgInfoBank)
            {
                if (info.FileCategory == FmgFileCategory.Item)
                {
                    path = itemPath;
                }
                else if (info.FileCategory == FmgFileCategory.Menu)
                {
                    path = menuPath;
                }

                JsonFMG fmgPair = new(info.FmgID, info.Fmg);
                var json = JsonSerializer.Serialize(fmgPair, FmgSerializerContext.Default.JsonFMG);
                json = FormatJson(json);

                var fileName = info.Name;
                if (CFG.Current.FMG_ShowOriginalNames)
                {
                    fileName = info.FileName;
                }

                File.WriteAllText($@"{path}\{fileName}.fmg.json", json);

                filecount++;
            }
        }

        TaskLogs.AddLog($"Finished exporting {filecount} text files",
            LogLevel.Information, TaskLogs.LogPriority.High);
        return true;
    }

    private static bool ImportFmg(FMGLanguage lang, FmgIDType fmgId, FMG fmg, bool merge)
    {
        foreach (FMGInfo info in lang._FmgInfoBanks.SelectMany((x) => x.Value.FmgInfos))
        {
            if (info.FmgID == fmgId)
            {
                if (merge)
                {
                    // Merge mode. Add and replace FMG entries instead of overwriting FMG entirely
                    foreach (var entry in fmg.Entries)
                    {
                        var currentEntry = info.Fmg.Entries.Find(e => e.ID == entry.ID);
                        if (currentEntry == null)
                        {
                            info.Fmg.Entries.Add(entry);
                        }
                        else if (currentEntry.Text != entry.Text)
                        {
                            currentEntry.Text = entry.Text;
                        }
                    }
                }
                else
                {
                    // Overwrite mode. Replace FMG with imported json
                    info.Fmg = fmg;
                }

                return true;
            }
        }

        TaskLogs.AddLog($"FMG import error: No loaded FMGs have an ID of \"{fmgId}\"",
            LogLevel.Error, TaskLogs.LogPriority.Normal);

        return false;
    }

    public static bool ImportFmgJson(FMGLanguage lang, bool merge)
    {
        if (!PlatformUtils.Instance.OpenMultiFileDialog("Choose Files to Import",
                new[] { AssetUtils.FmgJsonFilter }, out IReadOnlyList<string> files))
        {
            return false;
        }

        if (files.Count == 0)
        {
            return false;
        }

        var filecount = 0;
        foreach (var filePath in files)
        {
            try
            {
                var file = File.ReadAllText(filePath);
                JsonFMG json = JsonSerializer.Deserialize(file, FmgSerializerContext.Default.JsonFMG);
                bool success = ImportFmg(lang, json.FmgID, json.Fmg, merge);
                if (success)
                {
                    filecount++;
                }
            }
            catch (JsonException e)
            {
                TaskLogs.AddLog($"FMG import error: Couldn't import \"{filePath}\"",
                    LogLevel.Error, TaskLogs.LogPriority.Normal, e);
            }
        }

        if (filecount == 0)
        {
            return false;
        }

//        lang.HandleDuplicateEntries();
        PlatformUtils.Instance.MessageBox($"Imported {filecount} json files", "Finished", MessageBoxButtons.OK);
        return true;
    }

    public static bool ImportFmgTxt(FMGLanguage lang, bool merge)
    {
        if (!PlatformUtils.Instance.OpenMultiFileDialog("Choose Files to Import",
                new[] { AssetUtils.TxtFilter }, out IReadOnlyList<string> files))
        {
            return false;
        }

        if (files.Count == 0)
        {
            return false;
        }

        var filecount = 0;
        foreach (var filePath in files)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                FMG fmg = new();
                int fmgId = 0;
                var file = File.ReadAllLines(filePath);
                try
                {
                    fmgId = int.Parse(file[0].Replace(_entrySeparator, "").Replace("ID", ""));
                }
                catch
                {
                    TaskLogs.AddLog($"FMG import error for file {fileName}: Cannot parse FMG ID on line 1.",
                        LogLevel.Error, TaskLogs.LogPriority.Normal);
                    return false;
                }

                Queue<int> entryIds = new();
                List<string> text = new();
                for (var i = 1; i < file.Length; i++)
                {
                    var line = file[i];
                    if (i + 1 == file.Length)
                    {
                        text.Add(line);
                        string str = string.Join("\r\n", text);
                        while (entryIds.Count > 0)
                        {
                            fmg.Entries.Add(new(entryIds.Dequeue(), str));
                        }
                    }
                    else if (line.StartsWith(_entrySeparator))
                    {
                        if (text.Count > 0)
                        {
                            string str = string.Join("\r\n", text);

                            while (entryIds.Count > 0)
                            {
                                fmg.Entries.Add(new(entryIds.Dequeue(), str));
                            }

                            try
                            {
                                var ids = line.Split(_entrySeparator);
                                foreach (var id in ids)
                                {
                                    if (string.IsNullOrEmpty(id))
                                        continue;

                                    entryIds.Enqueue(int.Parse(id));
                                }
                            }
                            catch
                            {
                                TaskLogs.AddLog($"FMG import error for file {fileName}: Cannot parse entry ID on line {i + 1}.",
                                    LogLevel.Error, TaskLogs.LogPriority.High);
                                return false;
                            }
                            text = new();
                        }
                    }
                    else
                    {
                        text.Add(line);
                    }
                }

                bool success = ImportFmg(lang, (FmgIDType)fmgId, fmg, merge);
                if (success)
                {
                    filecount++;
                }
            }
            catch (Exception e)
            {
                TaskLogs.AddLog($"FMG import error: Couldn't import \"{filePath}\"",
                    LogLevel.Error, TaskLogs.LogPriority.Normal, e);
            }
        }

        if (filecount == 0)
        {
            return false;
        }

//        lang.HandleDuplicateEntries();
        TaskLogs.AddLog($"FMG import: Finished importing {filecount} txt files",
            LogLevel.Information, TaskLogs.LogPriority.Normal);
        return true;
    }
}

