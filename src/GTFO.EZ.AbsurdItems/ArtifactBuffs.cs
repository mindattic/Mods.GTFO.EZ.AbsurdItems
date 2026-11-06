using System;
using System.Collections.Generic;
using System.Linq;
using Agents;
using BepInEx.Configuration;
using GameData;
using HarmonyLib;
using LevelGeneration;
using MindAttic.GTFO.Shared;
using Player;
using SNetwork;

namespace GTFO.EZ.AbsurdItems;

// Artifact pickups give the player who picked one up a buff for the rest of the level: each Bold
// (Uncommon) +5% max health, each Aggressive (Rare) +5% damage dealt, each Muted (Common) 5% harder
// for enemies to detect. Counts are kept per player on every client - OnSyncStateChange runs
// everywhere with the picker - because enemy detection runs on the host and damage on whoever
// dealt it, so each needs everyone's counts; max health only ever changes on the owner's own game.
// GAI-T38: since every machine sees every pickup, every machine's event log also says so ("Bob picked
// up a Bold artifact: +5% max health, Bold x3") followed by the team's tally - no network event needed.
// Moved here from GTFO.EZ with the same config keys and defaults.
internal static class ArtifactBuffs
{
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<float> BoldMaxHealthPercent = null!;
    internal static ConfigEntry<float> AggressiveDamagePercent = null!;
    internal static ConfigEntry<float> MutedQuieterPercent = null!;

    internal static void Bind(ConfigFile config)
    {
        const string s = "Artifacts";
        Enabled = config.Bind(s, "Artifact Buffs Enabled", true,
            "Picking up an artifact buffs whoever picked it up until the level ends (a checkpoint restart that puts " +
            "the artifact back takes the buff back off). Bold, Aggressive and Muted are the game's Uncommon, Rare " +
            "and Common artifacts.");

        BoldMaxHealthPercent = NonNegative.Bind(config, s, "Bold: Max Health (%)", 5f,
            "Max health added per Bold artifact picked up, in percent of your starting max health (the gain is healed too). Default 5.");

        AggressiveDamagePercent = NonNegative.Bind(config, s, "Aggressive: Damage (%)", 5f,
            "Damage dealt to enemies (guns, melee and Hand Grenades) added per Aggressive artifact picked up. Default 5.");

        MutedQuieterPercent = NonNegative.Bind(config, s, "Muted: Quieter (%)", 5f,
            "How much harder enemies find it to detect you, per Muted artifact picked up (capped at 75%). Default 5.");
    }

    private static readonly Dictionary<ulong, int[]> CountsByPlayer = new();
    private static readonly Dictionary<IntPtr, (ulong Player, ArtifactCategory Category)> Counted = new();

    private static IntPtr _healthOwner;
    private static float _baseMaxHealth;
    private static float _bonusApplied;

    internal static int Count(PlayerAgent? player, ArtifactCategory category)
    {
        var owner = player?.Owner;
        if (owner == null || !CountsByPlayer.TryGetValue(owner.Lookup, out var counts)) return 0;
        return counts[(int)category];
    }

    // A checkpoint restart puts artifacts picked up after it back in the level (a recall with
    // PlacedInLevel), so that pickup's buff is taken back off; a pickup is only counted once.
    internal static void OnArtifactState(ArtifactPickup_Core artifact, ePickupItemStatus status, PlayerAgent? player)
    {
        var key = artifact.Pointer;
        var category = artifact.m_artifactCategory;

        if (status == ePickupItemStatus.PickedUp)
        {
            var owner = player?.Owner;
            if (owner == null || Counted.ContainsKey(key)) return;
            Counted[key] = (owner.Lookup, category);
            Change(owner.Lookup, category, +1);
            Plugin.Logger.LogInfo($"Artifact buffs: {owner.NickName} picked up a {Name(category)} artifact ({Count(player, category)} this level).");
            Announce(owner, category);
        }
        else if (Counted.TryGetValue(key, out var counted))
        {
            Counted.Remove(key);
            Change(counted.Player, counted.Category, -1);
        }
    }

    // "Bob picked up a Bold artifact: +5% max health, Bold x3", then "Team artifacts - Bob: Bold 3 |
    // Amy: Muted 1, Aggressive 2" - in the event log of every machine (each sees the pickup itself).
    private static void Announce(SNet_Player owner, ArtifactCategory category)
    {
        if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;
        var count = CountsByPlayer.TryGetValue(owner.Lookup, out var counts) ? counts[(int)category] : 0;
        var name = string.IsNullOrWhiteSpace(owner.NickName) ? "Someone" : owner.NickName;
        Log($"{name} picked up a {Name(category)} artifact: {Effect(category)}, {Name(category)} x{count}");
        var summary = TeamSummary();
        if (summary.Length > 0) Log($"Team artifacts - {summary}");
    }

    internal static string TeamSummary()
    {
        var parts = new List<string>();
        foreach (var (lookup, counts) in CountsByPlayer)
        {
            var held = new[] { ArtifactCategory.Uncommon, ArtifactCategory.Rare, ArtifactCategory.Common }
                .Where(c => counts[(int)c] > 0)
                .Select(c => $"{Name(c)} {counts[(int)c]}")
                .ToList();
            if (held.Count > 0) parts.Add($"{NameOf(lookup)}: {string.Join(", ", held)}");
        }
        return string.Join(" | ", parts);
    }

    private static string Effect(ArtifactCategory category) => category switch
    {
        ArtifactCategory.Uncommon => $"+{BoldMaxHealthPercent.Value:0.#}% max health",
        ArtifactCategory.Rare => $"+{AggressiveDamagePercent.Value:0.#}% damage",
        ArtifactCategory.Common => $"{MutedQuieterPercent.Value:0.#}% harder to detect",
        _ => "no buff",
    };

    private static string NameOf(ulong lookup)
    {
        try
        {
            if (SNet.TryGetPlayer(lookup, out var player) && player != null && !string.IsNullOrWhiteSpace(player.NickName))
                return player.NickName;
        }
        catch (Exception) { }
        return "Someone";
    }

    private static void Log(string text)
    {
        if (!MessageHelper.Show(text, MessageType.EventLog)) Plugin.Logger.LogInfo($"Artifact buffs: {text}");
    }

    private static void Change(ulong player, ArtifactCategory category, int by)
    {
        if (!CountsByPlayer.TryGetValue(player, out var counts)) CountsByPlayer[player] = counts = new int[(int)ArtifactCategory._COUNT];
        counts[(int)category] = Math.Max(0, counts[(int)category] + by);
        if (category == ArtifactCategory.Uncommon) ApplyMaxHealth();
    }

    // The local player's max health gets BoldMaxHealthPercent of the starting max (the player
    // datablock's health, with GTFO.EZ's multiplier already in it when that's installed) per Bold
    // pickup. Only the bonus this adds is changed, never the whole max - so another mod raising or
    // lowering max health at the same time (a Stim Pack overheal) keeps its part. The gain is healed too.
    internal static void ApplyMaxHealth()
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var agent) || agent == null) return;
        var damage = agent.Damage?.TryCast<Dam_PlayerDamageLocal>();
        if (damage == null) return;
        if (damage.Pointer != _healthOwner)
        {
            _healthOwner = damage.Pointer;
            _bonusApplied = 0f;
            var player = GameDataBlockBase<PlayerDataBlock>.GetBlock(1);
            _baseMaxHealth = player != null && player.health > 0f ? player.health : damage.HealthMax;
        }

        var bonus = _baseMaxHealth * BoldMaxHealthPercent.Value / 100f * Count(agent, ArtifactCategory.Uncommon);
        var gain = bonus - _bonusApplied;
        if (Math.Abs(gain) < 0.01f) return;
        _bonusApplied = bonus;
        var oldMax = damage.HealthMax;
        var max = Math.Max(0.01f, oldMax + gain);
        damage.HealthMax = max;
        // Health above max is a Stim Pack overheal: leave it to drain on its own rather than cut it
        // back to the new max. Otherwise the gain is healed, up to the new max.
        var health = Math.Max(0.01f, damage.Health + Math.Max(0f, gain));
        if (damage.Health <= oldMax) health = Math.Min(max, health);
        damage.SendSetHealth(health);
    }

    internal static void Reset()
    {
        CountsByPlayer.Clear();
        Counted.Clear();
        _healthOwner = IntPtr.Zero; // the next ApplyMaxHealth reads this level's starting max fresh
    }

    internal static float DamageMultiplier(Agent? source)
    {
        var player = source?.TryCast<PlayerAgent>();
        return 1f + AggressiveDamagePercent.Value / 100f * Count(player, ArtifactCategory.Rare);
    }

    internal static float DetectionMultiplier(PlayerAgent player) =>
        Math.Max(0.25f, 1f - MutedQuieterPercent.Value / 100f * Count(player, ArtifactCategory.Common));

    private static string Name(ArtifactCategory category) => category switch
    {
        ArtifactCategory.Common => "Muted",
        ArtifactCategory.Uncommon => "Bold",
        ArtifactCategory.Rare => "Aggressive",
        _ => category.ToString(),
    };
}

[HarmonyPatch]
internal static class ArtifactBuffs_Patches
{
    [HarmonyPatch(typeof(ArtifactPickup_Core), nameof(ArtifactPickup_Core.OnSyncStateChange))]
    [HarmonyPostfix]
    private static void AfterArtifactState(ArtifactPickup_Core __instance, ePickupItemStatus status, PlayerAgent player)
    {
        if (!ArtifactBuffs.Enabled.Value) return;
        try
        {
            ArtifactBuffs.OnArtifactState(__instance, status, player);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Artifact buffs: couldn't count a pickup. {e}");
        }
    }

    [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.Setup))]
    [HarmonyPostfix]
    private static void AfterLevelSetup(Dam_PlayerDamageLocal __instance)
    {
        if (LocalPlayerDamage.IsMine(__instance)) ArtifactBuffs.Reset();
    }

    [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
    [HarmonyPrefix]
    private static void BeforeBulletDamage(ref float dam, Agent sourceAgent)
    {
        if (ArtifactBuffs.Enabled.Value) dam *= SafeDamageMultiplier(sourceAgent);
    }

    [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
    [HarmonyPrefix]
    private static void BeforeMeleeDamage(ref float dam, Agent sourceAgent)
    {
        if (ArtifactBuffs.Enabled.Value) dam *= SafeDamageMultiplier(sourceAgent);
    }

    [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.GetDetectionMod))]
    [HarmonyPostfix]
    private static void AfterDetectionMod(PlayerAgent __instance, ref float __result)
    {
        if (!ArtifactBuffs.Enabled.Value) return;
        try
        {
            __result *= ArtifactBuffs.DetectionMultiplier(__instance);
        }
        catch (Exception)
        {
            // leave detection as the game computed it
        }
    }

    private static float SafeDamageMultiplier(Agent sourceAgent)
    {
        try
        {
            return ArtifactBuffs.DamageMultiplier(sourceAgent);
        }
        catch (Exception)
        {
            return 1f;
        }
    }
}
