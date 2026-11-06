using System;

namespace GTFO.EZ.AbsurdItems;

/// <summary>
/// The numbers behind the items' behaviour, kept free of game and Unity types so they're unit
/// tested directly (tests/GTFO.EZ.AbsurdItems.Tests). The item code calls these.
/// </summary>
internal static class Formulas
{
    // A modded item's display name, marked so it can't be mistaken for a vanilla one (the same mark
    // GTFO.EZ.AbsurdWeapons puts on its weapons).
    internal static string DisplayName(string name) => name + " (!)";

    // Area damage at `distance` from the center: full at the center, falling linearly to
    // `edgeFraction` of it at `radius`.
    internal static float AreaFalloff(float distance, float radius, float edgeFraction)
    {
        if (radius <= 0f) return 1f;
        var t = Math.Clamp(distance / radius, 0f, 1f);
        return 1f + (edgeFraction - 1f) * t;
    }

    // A Hand Grenade's blink interval `age` seconds into its fuse: from 0.5 s down to 0.06 s,
    // speeding up toward the end.
    internal static float GrenadeFlashInterval(float age, float fuseSeconds)
    {
        var progress = fuseSeconds > 0f ? Math.Clamp(age / fuseSeconds, 0f, 1f) : 1f;
        return 0.5f + (0.06f - 0.5f) * progress * progress;
    }

    // GAI-T36: how many Hand Grenades one pickup holds - `min` to `max` inclusive (a min below 1
    // counts as 1, a max below the min as the min), picked from the pickup's own level-generation
    // seed. Every machine generates the level from the same seed, so each one rolls the same count
    // for the same pickup and whoever picks it up gets that count, host or client.
    internal static int GrenadePickupCount(int seed, int min, int max)
    {
        min = Math.Max(1, min);
        max = Math.Max(min, max);
        return new Random(seed).Next(min, max + 1);
    }
}
