using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace GTFO.EZ.AbsurdItems;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInProcess("GTFO.exe")]
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
        HandGrenade.Bind(Config);
        StimPack.Bind(Config);
        Sounds.Bind(Config);
        ConfigImport.Apply(Config); // after every Bind: values from GTFO.EZ's / GTFO.EZ.AbsurdWeapons' cfgs, once
        ClassInjector.RegisterTypeInIl2Cpp<BurstFlash>();
        ClassInjector.RegisterTypeInIl2Cpp<HandGrenadeFuse>();
        ClassInjector.RegisterTypeInIl2Cpp<ArtifactGlow>();

        new Harmony(GUID).PatchAll();
        AddComponent<HandGrenadeSupply>();
        AddComponent<ArtifactGlow>();

        Logger.LogInfo($"{NAME} v{VERSION} loaded.");
    }
}
