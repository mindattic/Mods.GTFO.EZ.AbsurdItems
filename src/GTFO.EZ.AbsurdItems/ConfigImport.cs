using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace GTFO.EZ.AbsurdItems;

/// <summary>
/// The features moved here from GTFO.EZ (artifact buffs, Consumables Are Kits) and
/// GTFO.EZ.AbsurdWeapons (Hand Grenade, Stim Pack, Loot Mix) bind the same keys, but in this mod's own
/// .cfg - so a player's values in the old mods' files would silently fall back to the defaults. Each
/// moved key that isn't in this mod's .cfg yet is copied once from the old file (read as text: those
/// mods no longer bind the keys, and their files keep them as orphaned lines). A key already in this
/// mod's .cfg is never touched, so this only ever runs once per key.
/// </summary>
internal static class ConfigImport
{
    private const string EzFile = "MindAttic.GTFO.EZ.cfg";
    private const string AbsurdWeaponsFile = "MindAttic.GTFO.EZ.AbsurdWeapons.cfg";

    // The (section, key) pairs in this mod's .cfg before anything was bound this run.
    private static HashSet<(string, string)> existing = new();

    internal static void Snapshot(ConfigFile config)
    {
        try
        {
            existing = File.Exists(config.ConfigFilePath) ? Keys(Read(config.ConfigFilePath)) : new();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Config import: couldn't read this mod's own config. {e.Message}");
            existing = new();
        }
    }

    // After every Bind.
    internal static void Apply(ConfigFile config)
    {
        try
        {
            var ez = ReadOld(EzFile);
            var aw = ReadOld(AbsurdWeaponsFile);
            if (ez.Count == 0 && aw.Count == 0) return;

            var imported = 0;
            void Copy(Dictionary<(string, string), string> from, string file, string fromSection, string fromKey, string section, string key)
            {
                if (existing.Contains((section, key)) || !from.TryGetValue((fromSection, fromKey), out var value)) return;
                if (!TryGetAny(config, section, key, out var entry) || entry == null) return;
                var before = entry.GetSerializedValue();
                entry.SetSerializedValue(value);
                if (entry.GetSerializedValue() != before)
                {
                    imported++;
                    Plugin.Logger.LogInfo($"Config import: [{section}] {key} = {entry.GetSerializedValue()} (from {file}).");
                }
            }

            // GTFO.EZ: artifact buffs.
            foreach (var key in new[] { "Artifact Buffs Enabled", "Bold: Max Health (%)", "Aggressive: Damage (%)", "Muted: Quieter (%)" })
                Copy(ez, EzFile, "Artifacts", key, "Artifacts", key);

            // GTFO.EZ.AbsurdWeapons: the Hand Grenade, the Stim Pack, their sounds and Loot Mix's own keys.
            foreach (var key in new[] { "Hand Grenade Enabled", "FuseSeconds", "BlastRadius", "BlastDamage", "BlastEdgeFraction",
                         "BlastStagger", "MaxCarried", "ThrowForceMultiplier" })
                Copy(aw, AbsurdWeaponsFile, "Hand Grenade", key, "Hand Grenade", key);
            foreach (var key in new[] { "Stim Pack Enabled", "HealPercent", "MaxHealthPercent", "DrainPercentPerSecond",
                         "DamageBoostPercent", "DamageBoostSeconds" })
                Copy(aw, AbsurdWeaponsFile, "Stim Pack", key, "Stim Pack", key);
            foreach (var key in new[] { "HandGrenadeThrow", "HandGrenadeBounce" })
                Copy(aw, AbsurdWeaponsFile, "Sounds", key, "Sounds", key);
            foreach (var key in new[] { "Loot Mix Enabled", "HandGrenadeWeight", "StimPackWeight", "OutInTheOpenShare" })
                Copy(aw, AbsurdWeaponsFile, "Loot", key, "Loot", key);

            // The kit settings both mods had: whichever decided the loot. With AbsurdWeapons' Loot Mix
            // on (and that mod not switched off), its copies did and GTFO.EZ's were skipped.
            var awLootMix = IsTrue(aw, "Loot", "Loot Mix Enabled", false) && IsTrue(aw, "0. Mod", "Enabled", true);
            var kitKeys = new (string Ez, string Aw)[]
            {
                ("Kit Loot (%)", "KitLootPercent"), ("Health Kit Weight", "HealthKitWeight"), ("Tool Kit Weight", "ToolKitWeight"),
                ("Ammo Kit Weight", "AmmoKitWeight"), ("Kit Charges Min", "KitChargesMin"), ("Kit Charges Max", "KitChargesMax"),
            };
            foreach (var (ezKey, awKey) in kitKeys)
            {
                if (awLootMix) Copy(aw, AbsurdWeaponsFile, "Loot", awKey, "Loot", ezKey);
                else Copy(ez, EzFile, "Loot", ezKey, "Loot", ezKey);
            }
            if (!awLootMix) Copy(ez, EzFile, "Loot", "Consumables Are Kits", "Loot", "Consumables Are Kits");

            if (imported > 0) Plugin.Logger.LogInfo($"Config import: {imported} setting(s) carried over from GTFO.EZ / GTFO.EZ.AbsurdWeapons.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Config import: couldn't carry settings over from GTFO.EZ / GTFO.EZ.AbsurdWeapons. {e.Message}");
        }
    }

    private static bool TryGetAny(ConfigFile config, string section, string key, out ConfigEntryBase? entry)
    {
        foreach (var definition in config.Keys)
        {
            if (definition.Section != section || definition.Key != key) continue;
            entry = config[definition];
            return true;
        }
        entry = null;
        return false;
    }

    private static bool IsTrue(Dictionary<(string, string), string> values, string section, string key, bool missing) =>
        values.TryGetValue((section, key), out var value) ? string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) : missing;

    private static Dictionary<(string, string), string> ReadOld(string file)
    {
        try
        {
            var path = Path.Combine(Paths.ConfigPath, file);
            return File.Exists(path) ? Read(path) : new();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Config import: couldn't read {file}. {e.Message}");
            return new();
        }
    }

    private static HashSet<(string, string)> Keys(Dictionary<(string, string), string> values) => new(values.Keys);

    // A BepInEx .cfg: "[Section]" headers, "Key = Value" lines, "#" comments.
    private static Dictionary<(string, string), string> Read(string path)
    {
        var values = new Dictionary<(string, string), string>();
        var section = string.Empty;
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                section = line.Substring(1, line.Length - 2).Trim();
                continue;
            }
            var equals = line.IndexOf('=');
            if (equals <= 0) continue;
            values[(section, line.Substring(0, equals).Trim())] = line.Substring(equals + 1).Trim();
        }
        return values;
    }
}
