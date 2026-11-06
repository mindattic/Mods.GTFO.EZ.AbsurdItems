using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace GTFO.EZ.AbsurdItems;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInProcess("GTFO.exe")]
[BepInDependency("dev.gtfomodding.gtfo-api")] // the grenades' throws, sticks and blasts reach teammates through it (GAI-T37)
[BepInDependency(GtfoEzGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    public const string GUID = "MindAttic.GTFO.EZ.AbsurdItems";
    public const string NAME = "GTFO.EZ.AbsurdItems";
    public const string VERSION = "20.0.0";

    // Optional: GTFO.EZ's datablock tweaks run before this mod's items and kit loot (the kits follow
    // the resource budget it sets).
    internal const string GtfoEzGuid = "MindAttic.GTFO.EZ";

    internal static ManualLogSource Logger = null!;

    public override void Load()
    {
        Logger = Log;
        ConfigImport.Snapshot(Config); // before the first Bind writes the file

        // The master switch is the cfg's first entry ("0. Mod" sorts ahead of every other section), so
        // the mod can be turned off from its .cfg where a mod manager profile doesn't list it. When it's
        // off nothing else is bound or patched: the other settings stay in the file untouched.
        var enabled = Config.Bind("0. Mod", "Enabled", true,
            "Turn the whole mod on or off. When false, the mod loads nothing: no patches, no items, no hotkeys, no loot changes.");
        if (!enabled.Value)
        {
            Logger.LogInfo($"{NAME} is switched off in its config ([0. Mod] Enabled) - not loading.");
            return;
        }

        ArtifactBuffs.Bind(Config);
        ArtifactGlow.Bind(Config);
        Breadcrumbs.Bind(Config);
        ConsumablesAreKits.Bind(Config);
        Grenades.Bind(Config);
        StimPack.Bind(Config);
        Sounds.Bind(Config);
        ConfigImport.Apply(Config); // after every Bind: values from GTFO.EZ's / GTFO.EZ.AbsurdWeapons' cfgs, once
        ApplyDefaultMigrations(); // after the import, so an imported old default moves on too
        ClassInjector.RegisterTypeInIl2Cpp<BurstFlash>();
        ClassInjector.RegisterTypeInIl2Cpp<GlowstickGrenadeFuse>();
        ClassInjector.RegisterTypeInIl2Cpp<HaloGrenade>();
        ClassInjector.RegisterTypeInIl2Cpp<ArtifactGlow>();

        new Harmony(GUID).PatchAll();
        HaloGrenade.Init();
        AddComponent<GrenadeSupply>();
        AddComponent<ArtifactGlow>();

        Logger.LogInfo($"{NAME} v{VERSION} loaded.");
    }

    // BepInEx keeps whatever value is already in an upgrader's .cfg, so a changed default never
    // reaches existing installs. Each entry moves a setting still on its old default to the new one,
    // once: only for installs whose [Internal] migration version is below the entry's (the same
    // scheme as GTFO.EZ's, under its own key so the two .cfg files never share one), so a player who
    // sets it back afterwards keeps their value.
    private static readonly (int Version, Func<ConfigEntry<int>> Entry, int OldDefault)[] IntDefaultMigrations =
    {
        // GAI-T36: carry up to 20 (the Hand Grenade's, carried over to both grenades by ConfigImport, GAI-T37)
        (1, () => Grenades.Frag.MaxCarried, 6),
        (1, () => Grenades.Plasma.MaxCarried, 6),
    };

    private void ApplyDefaultMigrations()
    {
        try
        {
            var version = Config.Bind("Internal", "AbsurdItems Default Migration Version", 0,
                "Internal bookkeeping - do not edit. Tracks which one-time default migrations have already run.");
            var reached = version.Value;
            var latest = reached;
            foreach (var (at, entry, oldDefault) in IntDefaultMigrations)
            {
                var e = entry();
                if (reached < at && e.Value == oldDefault)
                {
                    e.Value = (int)e.DefaultValue;
                    Logger.LogInfo($"Config: [{e.Definition.Section}] {e.Definition.Key} moved from the old default {oldDefault} to {e.Value}.");
                }
                latest = Math.Max(latest, at);
            }
            if (version.Value != latest) version.Value = latest;
        }
        catch (Exception e)
        {
            Logger.LogWarning($"Config: couldn't apply the default migrations. {e.Message}");
        }
    }
}
