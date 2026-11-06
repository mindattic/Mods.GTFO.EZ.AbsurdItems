using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GameData;
using HarmonyLib;
using MindAttic.GTFO.Shared;
using UnityEngine;

namespace GTFO.EZ.AbsurdItems;

// Consumables found in lockers, boxes or lying around are a few small health, tool and ammo kits -
// the shared KitLoot.cs, from GTFO.EZ.Shared - on top of the zone's normal resource packs.
// With Loot Mix on (it was GTFO.EZ.AbsurdWeapons'), the same run adds Frag and Plasma Grenades and Stim Packs to
// the kits' lottery at their weights' odds, and leaves OutInTheOpenShare of the loot lying around
// the zone instead of in boxes. One run, one set of kit settings: Consumables Are Kits' (moved here
// from GTFO.EZ, same keys and defaults); GTFO.EZ.AbsurdWeapons' duplicates of them (KitLootPercent,
// HealthKitWeight, ...) are gone. GTFO.EZ, when installed, leaves its own consumable spawn odds
// alone while this is on, and its datablock tweaks (the consumable spawn multiplier, the zone
// resource budget the kit count follows) run first.
[HarmonyPatch]
internal static class ConsumablesAreKits
{
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<float> KitLootPercent = null!;
    internal static ConfigEntry<float> HealthKitWeight = null!;
    internal static ConfigEntry<float> ToolKitWeight = null!;
    internal static ConfigEntry<float> AmmoKitWeight = null!;
    internal static ConfigEntry<int> KitChargesMin = null!;
    internal static ConfigEntry<int> KitChargesMax = null!;

    // Loot Mix - the grenades and the Stim Pack in the same lottery (keys as GTFO.EZ.AbsurdWeapons had them;
    // GAI-T37 split the old HandGrenadeWeight between the Frag and Plasma Grenades).
    internal static ConfigEntry<bool> LootMixEnabled = null!;
    internal static ConfigEntry<float> FragGrenadeWeight = null!;
    internal static ConfigEntry<float> PlasmaGrenadeWeight = null!;
    internal static ConfigEntry<float> StimPackWeight = null!;
    internal static ConfigEntry<float> OutInTheOpenShare = null!;

    internal static void Bind(ConfigFile config)
    {
        const string s = "Loot";
        Enabled = config.Bind(s, "Consumables Are Kits", true,
            "Consumables found in lockers, boxes or lying around are small health, tool and ammo kits, and only a few of " +
            "them (see Kit Loot) - on top of the zone's normal resource packs, which are left as they are. Loot Mix adds Frag " +
            "and Plasma Grenades and Stim Packs to them. Off: vanilla consumables (retuned by GTFO.EZ's per-item weights, if that's " +
            "installed), and no Loot Mix. Takes effect on restart.");

        KitLootPercent = NonNegative.Bind(config, s, "Kit Loot (%)", 20f,
            "With Consumables Are Kits on: how much the kits found as loot add, in percent of a zone's normal health, ammo " +
            "and tool pack budget (averaged over the difficulty tiers). Default 20 - about a fifth more kits than the resource " +
            "packs alone. Loot Mix's grenades and Stim Packs come on top. Takes effect on restart.");

        HealthKitWeight = NonNegative.Bind(config, s, "Health Kit Weight", 1f,
            "With Consumables Are Kits on: multiplies the odds a kit is a health kit. The odds already follow the zone's own health pack budget; 1 keeps that split. Takes effect on restart.");

        ToolKitWeight = NonNegative.Bind(config, s, "Tool Kit Weight", 1f,
            "With Consumables Are Kits on: multiplies the odds a kit is a tool kit. The odds already follow the zone's own tool pack budget; 1 keeps that split. Takes effect on restart.");

        AmmoKitWeight = NonNegative.Bind(config, s, "Ammo Kit Weight", 1f,
            "With Consumables Are Kits on: multiplies the odds a kit is an ammo kit. The odds already follow the zone's own ammo pack budget; 1 keeps that split. Takes effect on restart.");

        KitChargesMin = config.Bind(s, "Kit Charges Min", 1,
            "Fewest charges a kit found as loot holds. One charge is one use, 20% of a full pack. Takes effect on restart.");

        KitChargesMax = config.Bind(s, "Kit Charges Max", 2,
            "Most charges a kit found as loot holds. Takes effect on restart.");

        LootMixEnabled = config.Bind(s, "Loot Mix Enabled", true,
            "With Consumables Are Kits on: Frag and Plasma Grenades and Stim Packs join the kits in the loot, and OutInTheOpenShare of " +
            "it lies out in the open. Off: kits only, where the game puts them. Takes effect on restart.");

        FragGrenadeWeight = NonNegative.Bind(config, s, "FragGrenadeWeight", 2f,
            "With Loot Mix on: odds a loot consumable is a Frag Grenade, against the kits' weights (1 each). Takes effect on restart.");

        PlasmaGrenadeWeight = NonNegative.Bind(config, s, "PlasmaGrenadeWeight", 2f,
            "With Loot Mix on: odds a loot consumable is a Plasma Grenade, against the kits' weights (1 each). Takes effect on restart.");

        StimPackWeight = NonNegative.Bind(config, s, "StimPackWeight", 1f,
            "With Loot Mix on: relative odds it's a Stim Pack. Takes effect on restart.");

        OutInTheOpenShare = config.Bind(s, "OutInTheOpenShare", 0.6f,
            new ConfigDescription("With Loot Mix on: share (0-1) of loot consumables left lying out in the open instead of in boxes and lockers. Takes effect on restart.",
                new AcceptableValueRange<float>(0f, 1f)));
    }

    // After GTFO.EZ's tweaks: its consumable spawn multiplier sets the tables' sizes and its
    // expedition balance the resource budget the kit count follows. Without it, this runs alone.
    // Low priority: after the grenades and the Stim Pack have registered their items.
    [HarmonyPatch(typeof(GameDataInit), nameof(GameDataInit.Initialize))]
    [HarmonyPostfix]
    [HarmonyAfter(Plugin.GtfoEzGuid)]
    [HarmonyPriority(Priority.Low)]
    private static void Apply()
    {
        try
        {
            if (!Enabled.Value) return;

            var settings = new KitLootSettings
            {
                LogName = "Consumables Are Kits",
                LootPercent = KitLootPercent.Value,
                MinCharges = KitChargesMin.Value,
                MaxCharges = KitChargesMax.Value,
                HealthWeight = HealthKitWeight.Value,
                ToolWeight = ToolKitWeight.Value,
                AmmoWeight = AmmoKitWeight.Value,
            };

            if (!LootMixEnabled.Value)
            {
                KitLoot.Fill(settings);
                return;
            }

            if (Grenades.Frag.Enabled.Value) AddExtra(settings.Extras, Grenades.Frag.ItemId, FragGrenadeWeight, Grenades.Frag.Name);
            if (Grenades.Plasma.Enabled.Value) AddExtra(settings.Extras, Grenades.Plasma.ItemId, PlasmaGrenadeWeight, Grenades.Plasma.Name);
            if (StimPack.Enabled.Value) AddExtra(settings.Extras, StimPack.ItemId, StimPackWeight, "Stim Pack");

            var inBoxes = 1f - Mathf.Clamp01(OutInTheOpenShare.Value);
            var tables = KitLoot.Fill(settings, block => block.ChanceToSpawnInResourceContainer = inBoxes);
            Plugin.Logger.LogInfo($"Loot Mix: {tables} table(s), {1f - inBoxes:P0} out in the open.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems couldn't set the kit loot: {e}");
        }
    }

    private static void AddExtra(List<(uint Id, float Weight)> into, uint id, ConfigEntry<float> weight, string what)
    {
        if (!GameDataBlockBase<ItemDataBlock>.HasBlock(id)) Plugin.Logger.LogWarning($"Loot Mix: no {what} item found - left out of the loot.");
        else if (weight.Value > 0f) into.Add((id, weight.Value));
    }
}
