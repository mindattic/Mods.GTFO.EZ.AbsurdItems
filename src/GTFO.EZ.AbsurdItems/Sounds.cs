using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace GTFO.EZ.AbsurdItems;

/// <summary>
/// The grenades' sounds, picked from the game's own Wwise events. Each is a config entry
/// holding the event's name (<c>[Sounds]</c>; empty = silent), so they can be re-picked without a
/// rebuild. Events with "2D" in the name are first-person only, so they play for the local player alone.
/// </summary>
internal static class Sounds
{
    internal static ConfigEntry<string> HandGrenadeThrow = null!;
    internal static ConfigEntry<string> HandGrenadeBounce = null!;

    internal static void Bind(ConfigFile config)
    {
        const string s = "Sounds";
        HandGrenadeThrow = config.Bind(s, "HandGrenadeThrow", "FRAGGRENADETHROW", "Throwing a Frag or Plasma Grenade (instead of the glowstick's toss). (The key keeps the old Hand Grenade's name.)");
        HandGrenadeBounce = config.Bind(s, "HandGrenadeBounce", "FRAGGRENADEBOUNCE", "A Frag Grenade bouncing, or a Plasma Grenade sticking (instead of the glowstick's clatter).");
    }

    // `local`: whether the event's source is the local player (a "2D" event only plays for them).
    internal static void Play(string eventName, Vector3 position, bool local = true)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;
        if (!local && eventName.IndexOf("2D", StringComparison.OrdinalIgnoreCase) >= 0) return;
        try
        {
            CellSound.Post(eventName.Trim(), position);
        }
        catch (Exception e)
        {
            if (failed.Add(eventName)) Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't play sound '{eventName}': {e.Message}");
        }
    }

    private static readonly HashSet<string> failed = new();
    private static readonly Dictionary<string, float> lastPlayed = new();

    // Play, but no more than once per `interval` seconds for `key`.
    internal static void PlayLimited(string key, float interval, string eventName, Vector3 position, bool local = true)
    {
        if (lastPlayed.TryGetValue(key, out var at) && Time.time - at < interval) return;
        lastPlayed[key] = Time.time;
        Play(eventName, position, local);
    }
}
