using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace GTFO.EZ.AbsurdItems;

/// <summary>
/// GAI-T40: the host's values for this mod's level-affecting settings ([Loot] - the kit loot and
/// Loot Mix - which grenades and Stim Packs may spawn, and how many grenades a pickup holds), while
/// you're in someone else's lobby. Every machine builds the level from its own data, so these must be
/// the host's: everything that shapes the level reads them through <see cref="Effective(ConfigEntry{float})"/>,
/// which is the host's value while one is held, else your own .cfg's. Nothing here writes your .cfg.
///
/// GTFO.EZ fills this (by reflection, on this public type) from the host values its level-settings
/// sync already receives (G.EZ-T125/T128), before it re-applies the level tweaks and runs the kit
/// loot again. Without GTFO.EZ nothing ever sets it, and your own values are used.
/// </summary>
public static class HostLevelSettings
{
    private static readonly Dictionary<string, float> Values = new(StringComparer.Ordinal);

    /// <summary>Whether host values are held (in someone else's lobby, after the host sent them).</summary>
    public static bool Active { get; private set; }

    /// <summary>
    /// The host's values, keyed "Section/Key" as in this mod's .cfg (a bool as 1/0). A setting the
    /// host didn't send keeps your own value. Null or empty clears.
    /// </summary>
    public static void Set(IDictionary<string, float>? values)
    {
        if (values == null || values.Count == 0)
        {
            Clear();
            return;
        }
        Values.Clear();
        foreach (var pair in values) Values[pair.Key] = pair.Value;
        Active = true;
        Plugin.Logger.LogInfo($"Level settings: using the host's values for {Values.Count} GTFO.EZ.AbsurdItems level setting(s).");
        Grenades.RefreshPickupRanges();
    }

    /// <summary>Back to your own values (left the lobby, or hosting yourself).</summary>
    public static void Clear()
    {
        if (!Active) return;
        Values.Clear();
        Active = false;
        Plugin.Logger.LogInfo("Level settings: back to your own GTFO.EZ.AbsurdItems level settings.");
        Grenades.RefreshPickupRanges();
    }

    private static bool TryHost(ConfigEntryBase entry, out float value)
    {
        value = 0f;
        return Active && Values.TryGetValue($"{entry.Definition.Section}/{entry.Definition.Key}", out value) && !float.IsNaN(value);
    }

    internal static float Effective(ConfigEntry<float> entry) => TryHost(entry, out var host) ? host : entry.Value;

    internal static int Effective(ConfigEntry<int> entry) => TryHost(entry, out var host) ? Mathf.RoundToInt(host) : entry.Value;

    internal static bool Effective(ConfigEntry<bool> entry) => TryHost(entry, out var host) ? host != 0f : entry.Value;
}
