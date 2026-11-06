using System;
using Agents;
using BepInEx.Configuration;
using GameData;
using Gear;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using LevelGeneration;
using Localization;
using Player;
using SNetwork;
using UnityEngine;

namespace GTFO.EZ.AbsurdItems;

internal enum GrenadeKind : byte
{
    Frag = 0,
    Plasma = 1,
}

/// <summary>
/// One of the two grenades' item and settings. Each is its own consumable (its own item id, so its
/// own stack); GTFO's one consumable slot holds a single kind at a time, so picking up the other kind
/// swaps it for what you carry, as with any two different consumables.
/// </summary>
internal sealed class GrenadeType
{
    internal readonly GrenadeKind Kind;
    internal readonly uint ItemId;
    internal readonly string Name, Section, TerminalShortName;

    internal ConfigEntry<bool> Enabled = null!;
    internal ConfigEntry<int> PickupMin = null!;
    internal ConfigEntry<int> PickupMax = null!;
    internal ConfigEntry<int> MaxCarried = null!;
    internal ConfigEntry<float> BlastDamage = null!;
    internal ConfigEntry<float> BlastStagger = null!;

    internal GrenadeType(GrenadeKind kind, uint itemId, string name, string terminalShortName)
    {
        Kind = kind;
        ItemId = itemId;
        Name = name;
        Section = name;
        TerminalShortName = terminalShortName;
    }

    internal void Bind(ConfigFile config, float blastDamage, string what)
    {
        Enabled = config.Bind(Section, $"{Name} Enabled", true,
            $"Lets the {Name} ({what}) spawn at all. It only spawns as loot through [Loot] Loot Mix Enabled, which adds it to the kit loot (Consumables Are Kits). " +
            "Off: none spawn (the item is still registered, so lobbies agree on it). Takes effect on restart.");
        BlastDamage = NonNegative.Bind(config, Section, "BlastDamage", blastDamage,
            "Damage at the blast's center, before damage boosts (Aggressive artifacts, a Stim Pack). It's full out to Halo 3's 0.5 world units (1.5 m) " +
            "and falls to Halo's edge share at 1.75 world units (5.3 m).");
        BlastStagger = NonNegative.Bind(config, Section, "BlastStagger", 6f, "Blast stagger multiplier.");
        MaxCarried = AtLeastOne.Bind(config, Section, "MaxCarried", 20, $"The most {Name}s you can carry.");
        PickupMin = AtLeastOne.Bind(config, Section, "PickupCountMin", 2, $"Fewest {Name}s one loot pickup holds; each pickup holds a random count from this to PickupCountMax. Takes effect on restart.");
        PickupMax = AtLeastOne.Bind(config, Section, "PickupCountMax", 5, $"Most {Name}s one loot pickup holds (below PickupCountMin counts as PickupCountMin). Takes effect on restart.");
    }

    internal bool Is(ItemDataBlock? block) => block != null && block.persistentID == ItemId;
}

/// <summary>
/// GAI-T37: the Frag Grenade and the Plasma Grenade (they replace the Hand Grenade). Each is the
/// glowstick's item cloned under its own id, so carrying, picking up and the stack stay vanilla - but
/// pressing fire throws one at once (no wind-up), flown by this mod with Halo 3's throw speed, gravity
/// and fuses (<see cref="HaloGrenades"/>, <see cref="HaloGrenade"/>). The Frag bounces and blows up on
/// its fuse or the moment it hits an enemy; the Plasma sticks to the first thing it hits - an enemy it
/// sticks to panics (<see cref="GrenadePanic"/>) - and blows up on its fuse. Nobody starts with any:
/// every one is found as loot.
/// </summary>
[HarmonyPatch]
internal static class Grenades
{
    // ItemDataBlock ids (not gear categories). 1203 was the Hand Grenade's; 1204 is the Stim Pack.
    internal static readonly GrenadeType Frag = new(GrenadeKind.Frag, 1203, "Frag Grenade", "FRAG_GRENADE");
    internal static readonly GrenadeType Plasma = new(GrenadeKind.Plasma, 1205, "Plasma Grenade", "PLASMA_GRENADE");
    internal static readonly GrenadeType[] All = { Frag, Plasma };

    // Plasma-only settings.
    internal static ConfigEntry<float> PlasmaStuckDamage = null!;
    internal static ConfigEntry<float> PlasmaStuckFuseSeconds = null!;
    internal static ConfigEntry<bool> PlasmaPanicEnabled = null!;
    internal static ConfigEntry<float> PlasmaPanicTowardEnemiesChance = null!;

    internal static void Bind(ConfigFile config)
    {
        // Halo's damage is against Halo health, so only its ratios carry over: the frag keeps the old
        // Hand Grenade's 60, the plasma's splash is Halo's 240/160 of it and its stuck hit 300/160.
        Frag.Bind(config, 60f, "bounces; blows up on its fuse or the moment it hits an enemy");
        Plasma.Bind(config, 60f * HaloGrenades.PlasmaToFragDamage, "sticks to the first thing it hits; an enemy it sticks to panics");
        PlasmaStuckDamage = NonNegative.Bind(config, Plasma.Section, "StuckDamage", 60f * HaloGrenades.PlasmaAttachedToFragDamage,
            "Extra damage to the enemy a Plasma Grenade is stuck to when it goes off (Halo 3's \"300, attached only\" against the frag's 160), on top of the blast.");
        PlasmaStuckFuseSeconds = NonNegative.Bind(config, Plasma.Section, "StuckFuseSeconds", HaloGrenades.PlasmaFuseAfterRest,
            "Seconds from sticking until it goes off (never sooner than Halo 3's 1.3 s after the throw). Halo 3: 0.9. Halo: Combat Evolved's was 3 - longer panic.");
        PlasmaPanicEnabled = config.Bind(Plasma.Section, "Panic", true,
            "An enemy with a Plasma Grenade stuck to it panics until it goes off: it stops attacking and chasing players and runs - away from the players, or into the nearest other enemies.");
        PlasmaPanicTowardEnemiesChance = config.Bind(Plasma.Section, "PanicTowardEnemiesChance", 0.5f,
            new ConfigDescription("Chance (0-1), rolled per stuck enemy, that it runs into the nearest other enemies instead of away from the players.",
                new AcceptableValueRange<float>(0f, 1f)));
    }

    internal static GrenadeType? Of(ItemDataBlock? block)
    {
        if (block == null) return null;
        var id = block.persistentID;
        return id == Frag.ItemId ? Frag : id == Plasma.ItemId ? Plasma : null;
    }

    internal static GrenadeType Of(GrenadeKind kind) => kind == GrenadeKind.Plasma ? Plasma : Frag;

    // ---- The stack: the cap and how many a pickup holds (GAI-T36) ----

    // While one of the grenades is in the local consumable slot, the slot holds its MaxCarried (the
    // item's own cap is the biggest pickup, PickupCountMax). GTFO.EZ.Shared's infinite ammo (Delete,
    // GES-T13) refills the slot to this raised AmmoMaxCap.
    internal static void Uncap(InventorySlotAmmo? ammo)
    {
        var type = InLocalConsumableSlot();
        if (ammo == null || type == null) return;
        var cap = Mathf.Max(1, type.MaxCarried.Value);
        if (ammo.BulletsMaxCap == cap) return;
        ammo.AmmoMaxCap = Mathf.Max(0.0001f, ammo.CostOfBullet) * cap;
        ammo.BulletsMaxCap = cap;
    }

    private static GrenadeType? InLocalConsumableSlot()
    {
        var backpack = PlayerBackpackManager.LocalBackpack;
        if (backpack == null || !backpack.TryGetBackpackItem(InventorySlot.Consumable, out var item) || item == null) return null;
        foreach (var type in All)
            if (item.ItemID == type.ItemId || type.Is(item.Instance?.ItemDataBlock)) return type;
        return null;
    }

    // The game re-sets the slot's cap from the item whenever it sets the slot's ammo up (picking one
    // up included), so it's lifted again straight after.
    [HarmonyPatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.SetupAmmoType))]
    [HarmonyPostfix]
    private static void AfterSetupAmmoType(PlayerAmmoStorage __instance, AmmoType type)
    {
        try
        {
            // Only your own storage: a teammate's on your game setting its consumable up mustn't get your cap.
            var local = PlayerBackpackManager.LocalBackpack?.AmmoStorage;
            if (type == AmmoType.CurrentConsumable && local != null && local.Pointer == __instance.Pointer) Uncap(__instance.ConsumableAmmo);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't set the grenade cap: {e.Message}");
        }
    }

    // GAI-T36: each grenade pickup lying in the level holds PickupCountMin-PickupCountMax of them, a
    // whole number picked from the pickup's own level-generation seed - the same on every machine, so
    // whoever picks it up, host or client, gets the same number. GTFO.EZ.Shared's KitLoot postfixes
    // these same two methods for the kits; each leaves the other's items alone. GTFO.EZ doesn't patch them.
    [HarmonyPatch(typeof(LG_PickupItem), nameof(LG_PickupItem.SetupAsConsumable), new[] { typeof(int), typeof(uint) })]
    [HarmonyPostfix]
    private static void AfterSetupAsConsumable(LG_PickupItem __instance, int __0) => SetPickupCount(__instance, __0);

    [HarmonyPatch(typeof(LG_PickupItem), nameof(LG_PickupItem.SetupAsConsumable), new[] { typeof(ConsumableDistributionDataBlock), typeof(int), typeof(uint) })]
    [HarmonyPostfix]
    private static void AfterSetupAsConsumableFromTable(LG_PickupItem __instance, int __1) => SetPickupCount(__instance, __1);

    private static int loggedPickups;

    private static void SetPickupCount(LG_PickupItem pickup, int seed)
    {
        try
        {
            var item = pickup.GetComponentInChildren<ConsumablePickup_Core>();
            var type = Of(item?.ItemDataBlock);
            if (item == null || type == null) return;

            var count = Formulas.GrenadePickupCount(seed, HostLevelSettings.Effective(type.PickupMin), HostLevelSettings.Effective(type.PickupMax));
            var custom = item.GetCustomData();
            if (custom.ammo == count) return;
            custom.ammo = count;
            item.SetCustomData(custom, false);
            try { item.UpdatePickupName(); } catch (Exception) { /* name only */ }
            if (loggedPickups++ < 3) Plugin.Logger.LogInfo($"GTFO.EZ.AbsurdItems: a {type.Name} pickup holds {count}.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't set a grenade pickup's count: {e.Message}");
        }
    }

    // ---- The items: the glowstick's, cloned. After GTFO.EZ's datablock tweaks. ----

    private static readonly bool[] added = new bool[2];

    // The game's own roll stays in range; SetPickupCount then settles each pickup on a whole count.
    // Effective: the host's PickupCountMin/Max in someone else's lobby (GAI-T40).
    private static void SetPickupRange(ItemDataBlock item, GrenadeType type)
    {
        item.ConsumableAmmoMin = Mathf.Max(1, HostLevelSettings.Effective(type.PickupMin));
        item.ConsumableAmmoMax = Mathf.Max(item.ConsumableAmmoMin, HostLevelSettings.Effective(type.PickupMax));
    }

    // The host's pickup counts arrived (or were dropped): the registered items' ranges follow.
    internal static void RefreshPickupRanges()
    {
        foreach (var type in All)
        {
            try
            {
                if (!added[(int)type.Kind]) continue;
                var item = GameDataBlockBase<ItemDataBlock>.GetBlock(type.ItemId);
                if (item != null) SetPickupRange(item, type);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't update the {type.Name}'s pickup range: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameDataInit), nameof(GameDataInit.Initialize))]
    [HarmonyPostfix]
    [HarmonyAfter(Plugin.GtfoEzGuid)]
    private static void AddItems()
    {
        foreach (var type in All) AddItem(type);
    }

    private static void AddItem(GrenadeType type)
    {
        try
        {
            // Registered whatever its Enabled says (GAI-T39, G.EZ-T125): every machine in a lobby then
            // holds the same items; Enabled only decides whether it spawns (Loot Mix).
            if (GameDataBlockBase<ItemDataBlock>.HasBlock(type.ItemId))
            {
                if (!added[(int)type.Kind]) Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems: item id {type.ItemId} is already taken by another mod's datablock - not adding the {type.Name}.");
                return;
            }
            var source = FindGlowstick();
            if (source == null)
            {
                Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems: couldn't find the glowstick's item datablock - not adding the {type.Name}.");
                return;
            }

            var item = DataBlocks.Clone(source);
            item.persistentID = type.ItemId;
            item.name = type.Name.Replace(" ", string.Empty);
            item.publicName = Formulas.DisplayName(type.Name);
            item.LocalizedName = new LocalizedText { Id = 0, UntranslatedText = Formulas.DisplayName(type.Name) };
            // Its own terminal names too, or LIST/QUERY/PING would show it as a glow stick.
            item.terminalItemLongName = new LocalizedText { Id = 0, UntranslatedText = type.Name };
            item.terminalItemShortName = type.TerminalShortName;
            SetPickupRange(item, type);
            GameDataBlockBase<ItemDataBlock>.AddBlock(item, -1);
            added[(int)type.Kind] = true;
            Plugin.Logger.LogInfo($"GTFO.EZ.AbsurdItems added the {type.Name} (consumable; item id {type.ItemId}, cloned from '{source.name}' {source.persistentID}).");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems failed to add the {type.Name}: {e}");
        }
    }

    // The plain glowstick: a consumable whose thrown prefab is a glowstick, preferring the one
    // without a seasonal color in its name.
    private static ItemDataBlock? FindGlowstick()
    {
        ItemDataBlock? best = null;
        var bestScore = -1;
        foreach (var block in GameDataBlockBase<ItemDataBlock>.GetAllBlocks())
        {
            if (block == null || Of(block) != null || block.inventorySlot != InventorySlot.Consumable) continue;
            var glow = Contains(block.name, "glowstick");
            var prefabs = block.InstancePrefabs;
            if (prefabs != null)
                for (var i = 0; i < prefabs.Count; i++)
                    if (Contains(prefabs[i], "glowstick")) glow = true;
            if (!glow) continue;

            var score = Contains(block.name, "christmas") || Contains(block.name, "halloween") || Contains(block.name, "yellow") ? 0 : 1;
            if (score > bestScore) { best = block; bestScore = score; }
        }
        return best;
    }

    private static bool Contains(string? text, string part) => text != null && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

    // ---- In your hand: instant throw ----

    // The local player's wielded grenade (its ThrowingWeapon), or zero.
    private static IntPtr held;
    private static GrenadeType? heldType;
    private static float readyAt;
    [ThreadStatic] private static bool readingInput;

    [HarmonyPatch(typeof(ThrowingWeapon), nameof(ThrowingWeapon.OnWield))]
    [HarmonyPostfix]
    private static void AfterWield(ThrowingWeapon __instance)
    {
        try
        {
            var type = Of(__instance.ItemDataBlock);
            if (type == null) return;
            var mover = __instance.m_movingThrowObj;
            if (mover != null) Reskin(mover.gameObject, type.Kind);
            if (!string.IsNullOrWhiteSpace(Sounds.HandGrenadeThrow.Value)) __instance.m_throwSoundEvent = Sounds.HandGrenadeThrow.Value.Trim();
            if (IsLocal(__instance.Owner))
            {
                held = __instance.Pointer;
                heldType = type;
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't ready the held grenade: {e.Message}");
        }
    }

    [HarmonyPatch(typeof(ThrowingWeapon), nameof(ThrowingWeapon.OnUnWield))]
    [HarmonyPostfix]
    private static void AfterUnWield(ThrowingWeapon __instance)
    {
        if (__instance.Pointer != held) return;
        held = IntPtr.Zero;
        heldType = null;
    }

    // No wind-up: while a grenade is in the local player's hand, the game's own throw never sees the
    // fire button (so it never charges); BeforeThrowingUpdate throws on the press instead.
    [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.FireButton), MethodType.Getter)]
    [HarmonyPostfix]
    private static void HideFireButton(ItemEquippable __instance, ref bool __result)
    {
        if (__result && !readingInput && held != IntPtr.Zero && __instance.Pointer == held) __result = false;
    }

    [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.FireButtonPressed), MethodType.Getter)]
    [HarmonyPostfix]
    private static void HideFireButtonPressed(ItemEquippable __instance, ref bool __result)
    {
        if (__result && !readingInput && held != IntPtr.Zero && __instance.Pointer == held) __result = false;
    }

    [HarmonyPatch(typeof(ThrowingWeapon), nameof(ThrowingWeapon.Update))]
    [HarmonyPrefix]
    private static void BeforeThrowingUpdate(ThrowingWeapon __instance)
    {
        if (held == IntPtr.Zero || __instance.Pointer != held || heldType == null) return;
        try
        {
            bool pressed;
            readingInput = true;
            try { pressed = __instance.FireButtonPressed; }
            finally { readingInput = false; }

            var ready = Time.time >= readyAt && Count() > 0;
            ShowHeld(__instance, ready);
            if (pressed && ready) ThrowNow(__instance, heldType);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems grenade throw failed: {e}");
        }
    }

    // A fallback: if the game's own throw runs anyway (the fire button got through), it still throws
    // a Halo grenade, not a glowstick-physics one - and only once.
    [HarmonyPatch(typeof(ThrowingWeapon), nameof(ThrowingWeapon.Throw))]
    [HarmonyPrefix]
    private static bool BeforeVanillaThrow(ThrowingWeapon __instance)
    {
        try
        {
            var type = Of(__instance.ItemDataBlock);
            if (type == null || !IsLocal(__instance.Owner)) return true;
            if (Count() > 0) ThrowNow(__instance, type);
            return false;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems grenade throw failed: {e}");
            return true;
        }
    }

    private static int Count() => PlayerBackpackManager.LocalBackpack?.AmmoStorage?.GetBulletsInPack(AmmoType.CurrentConsumable) ?? 0;

    private static bool IsLocal(PlayerAgent? owner)
    {
        var local = PlayerManager.GetLocalPlayerAgent();
        return owner != null && local != null && owner.Pointer == local.Pointer;
    }

    // One throw: one off the stack (the same call the game's own throw makes), out along the aim from
    // the camera at Halo's speed, shown to everyone.
    [HideFromIl2Cpp]
    private static void ThrowNow(ThrowingWeapon weapon, GrenadeType type)
    {
        var owner = weapon.Owner;
        if (owner == null) return;
        readyAt = Time.time + HaloGrenades.ThrowRecoverySeconds;
        PlayerBackpackManager.LocalBackpack.AmmoStorage.UpdateBulletsInPack(AmmoType.CurrentConsumable, -1);

        var camera = owner.FPSCamera;
        var eye = camera.Position;
        var aim = camera.Forward;
        var right = Vector3.Cross(Vector3.up, aim);
        right = right.sqrMagnitude > 1e-6f ? right.normalized : Vector3.right;
        // From the throwing hand, just in front of the eye - but never out the other side of a wall.
        var start = eye + aim * 0.35f + right * 0.12f - Vector3.up * 0.1f;
        var toStart = start - eye;
        if (Physics.SphereCast(eye, HaloGrenades.Radius, toStart.normalized, out var block, toStart.magnitude, LayerManager.MASK_WORLD))
            start = eye + toStart.normalized * Mathf.Max(0f, block.distance - 0.02f);

        var velocity = HaloGrenade.ToUnity(HaloGrenades.LaunchVelocity(HaloGrenade.ToNumerics(aim)));
        HaloGrenade.ThrowLocal(type.Kind, owner, start, velocity);
        Sounds.Play(weapon.m_throwSoundEvent ?? string.Empty, eye);
        try { owner.Sync?.SendThrowStatus(pPlayerThrowStatus.StatusEnum.Throw, false); } catch (Exception) { /* third-person animation only */ }
    }

    // The grenade in your hand disappears for the moment after a throw (it's in the air) and while the
    // stack is empty.
    private static void ShowHeld(ThrowingWeapon weapon, bool show)
    {
        var mover = weapon.m_movingThrowObj;
        var model = mover != null ? mover.Find(ModelName) : null;
        if (model != null && model.gameObject.activeSelf != show) model.gameObject.SetActive(show);
    }

    // ---- Looks ----

    // The pickup lying in the level: a separate object from the held/thrown one, so its own reskin.
    [HarmonyPatch(typeof(ItemInLevel), nameof(ItemInLevel.Setup))]
    [HarmonyPostfix]
    private static void AfterPickupSetup(ItemInLevel __instance, ItemDataBlock data)
    {
        try
        {
            var type = Of(data);
            if (type != null) Reskin(__instance.gameObject, type.Kind);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't reskin a grenade pickup: {e.Message}");
        }
    }

    internal const string ModelName = "GTFO.EZ.AbsurdItems Grenade Model";

    // Hides every renderer and light already on `go` and adds the grenade's look (GrenadeModel), on
    // the same layer (the first-person item has its own).
    internal static void Reskin(GameObject go, GrenadeKind kind)
    {
        if (go.transform.Find(ModelName) != null) return;
        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (var light in go.GetComponentsInChildren<Light>(true)) light.enabled = false;
        foreach (var light in go.GetComponentsInChildren<EffectLight>(true)) light.enabled = false;

        var model = new GameObject(ModelName);
        model.transform.SetParent(go.transform, false);
        GrenadeModel.Build(model.transform, kind);
        foreach (var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = go.layer;
    }

    // ---- A grenade thrown the game's own way (a bot's throw): it still blows up ----

    // A grenade bounces with a grenade's clank instead of a glowstick's clatter.
    [HarmonyPatch(typeof(GlowstickInstance), nameof(GlowstickInstance.OnCollisionEnter))]
    [HarmonyPrefix]
    private static bool BeforeCollision(GlowstickInstance __instance)
    {
        try
        {
            if (Of(__instance.ItemDataBlock) == null) return true;
            Sounds.PlayLimited($"grenade:{__instance.Pointer}", 0.15f, Sounds.HandGrenadeBounce.Value, __instance.transform.position);
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    // Players' own throws never make one (they're flown by HaloGrenade); a bot throwing one through
    // the game's own throw does, and it gets the look and a fuse. Runs on every machine's copy.
    [HarmonyPatch(typeof(GlowstickInstance), nameof(GlowstickInstance.Setup))]
    [HarmonyPostfix]
    private static void AfterInstanceSetup(GlowstickInstance __instance, ItemDataBlock data)
    {
        try
        {
            var existing = __instance.GetComponent<GlowstickGrenadeFuse>();
            var type = Of(data);
            if (type == null)
            {
                // GAI-T32: a reused (pooled) instance that was a grenade before is a plain glowstick now.
                if (existing != null) Unarm(__instance, existing);
                return;
            }
            __instance.enabled = false; // no glow fading in and out
            if (__instance.m_light != null) __instance.m_light.enabled = false;
            // A reused instance may have been the other kind: its old model goes.
            var old = __instance.transform.Find(ModelName);
            if (old != null && (existing == null || existing.Kind != type.Kind))
            {
                old.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(old.gameObject);
                old.SetParent(null);
            }
            Reskin(__instance.gameObject, type.Kind);
            if (existing == null) existing = __instance.gameObject.AddComponent<GlowstickGrenadeFuse>();
            existing.Arm(type.Kind);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems couldn't arm a grenade: {e}");
        }
    }

    // GAI-T32: undoes AfterInstanceSetup on an instance reused as a plain glowstick.
    private static void Unarm(GlowstickInstance instance, GlowstickGrenadeFuse fuse)
    {
        fuse.enabled = false;
        UnityEngine.Object.Destroy(fuse);
        var model = instance.transform.Find(ModelName);
        if (model != null)
        {
            model.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(model.gameObject);
        }
        var blink = instance.transform.Find(GlowstickGrenadeFuse.BlinkName);
        if (blink != null) UnityEngine.Object.Destroy(blink.gameObject);
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            if (model == null || !renderer.transform.IsChildOf(model)) renderer.enabled = true;
        instance.enabled = true;
        if (instance.m_light != null) instance.m_light.enabled = true;
    }
}

/// <summary>
/// A grenade thrown through the game's own glowstick throw (in practice a bot's): the glowstick's
/// physics, but blinking and blowing up after Halo's fuse for its kind (no sticking - that's only on
/// players' throws). The thrower's game (or the host's, for a bot's) deals the damage, through the bullet
/// path so it syncs; every game shows the blast. The host then despawns the item - at once when it threw
/// it itself, otherwise a short grace later (GAI-T32), so a client thrower's blast lands first.
/// </summary>
internal sealed class GlowstickGrenadeFuse : MonoBehaviour
{
    internal const string BlinkName = "Blink";
    private const float RemoteThrowerDespawnGraceSeconds = 1.5f;

    private float age, nextFlash;
    private float despawnAt = -1f;
    private bool exploded, lit;
    private EffectLight? blink;

    internal GrenadeKind Kind; // a plain field: not exposed to Il2Cpp

    public GlowstickGrenadeFuse(IntPtr ptr) : base(ptr) { }

    private float Fuse => Kind == GrenadeKind.Plasma
        ? HaloGrenades.PlasmaMinFuse + Grenades.PlasmaStuckFuseSeconds.Value
        : HaloGrenades.FragMinFuse + HaloGrenades.FragFuseAfterGroundBounce;

    // A fresh fuse (GAI-T32: also on a reused instance thrown again).
    [HideFromIl2Cpp]
    internal void Arm(GrenadeKind kind)
    {
        Kind = kind;
        age = 0f;
        nextFlash = 0f;
        despawnAt = -1f;
        exploded = false;
        lit = false;
        if (blink != null) blink.Intensity = 0f;
        var model = transform.Find(Grenades.ModelName);
        if (model != null) model.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (exploded)
        {
            if (despawnAt < 0f) return;
            age += Time.deltaTime;
            if (age >= despawnAt)
            {
                despawnAt = -1f;
                Despawn();
            }
            return;
        }
        try
        {
            age += Time.deltaTime;
            var fuse = Fuse;
            if (age >= fuse)
            {
                Explode();
                return;
            }

            if (blink == null)
            {
                var lightObject = new GameObject(BlinkName);
                lightObject.transform.SetParent(transform, false);
                blink = lightObject.AddComponent<EffectLight>();
                blink.Color = HaloGrenade.FlashColor(Kind);
                blink.Range = 2.5f;
                blink.enabled = true;
            }
            var interval = Formulas.GrenadeFlashInterval(age, fuse);
            if (age >= nextFlash)
            {
                nextFlash = age + interval;
                lit = true;
                if (interval > 0.12f) CellSound.Post(AK.EVENTS.DEPLOYER_BEEP_TARGET_ACQUIRED, transform.position);
            }
            else if (lit && age >= nextFlash - interval * 0.55f)
            {
                lit = false;
            }
            blink.transform.position = transform.position + Vector3.up * 0.1f;
            blink.Intensity = lit ? 3f + 3f * Mathf.Clamp01(age / fuse) : 0f;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems grenade fuse failed: {e.Message}");
            Explode();
        }
    }

    [HideFromIl2Cpp]
    private void Explode()
    {
        if (exploded) return;
        exploded = true;
        var position = transform.position;
        Explosion.Show(position, Kind);

        var item = GetComponent<Item>();
        try
        {
            var owner = item != null ? item.Owner : null;
            var source = owner != null ? (owner.IsLocallyOwned ? owner : null) : (SNet.IsMaster ? PlayerManager.GetLocalPlayerAgent() : null);
            if (source != null) HaloGrenade.Blast(Kind, position, source.Cast<Agent>());
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems grenade blast failed: {e.Message}");
        }

        // GAI-T32: on the host, a grenade a client threw waits (hidden) for the thrower's own fuse.
        try
        {
            var owner = item != null ? item.Owner : null;
            if (SNet.IsMaster && owner != null && !owner.IsLocallyOwned)
            {
                if (blink != null) blink.Intensity = 0f;
                var model = transform.Find(Grenades.ModelName);
                if (model != null) model.gameObject.SetActive(false);
                despawnAt = age + RemoteThrowerDespawnGraceSeconds;
                return;
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't hold a client's grenade for its thrower: {e.Message}");
        }
        Despawn();
    }

    // Gone: the host despawns the synced item for everyone; until that arrives it's hidden here.
    [HideFromIl2Cpp]
    private void Despawn()
    {
        var item = GetComponent<Item>();
        try
        {
            var replicator = item?.ReplicationWrapper?.Replicator;
            if (SNet.IsMaster && replicator != null)
            {
                ItemReplicationManager.DeSpawn(replicator);
                return;
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't despawn a grenade: {e.Message}");
        }
        gameObject.SetActive(false);
    }
}

/// <summary>
/// Every frame in a level: keeps the local grenade stack's cap at its MaxCarried, runs the host's
/// panicking enemies, and clears the grenades and panic away when the level ends.
/// </summary>
internal sealed class GrenadeSupply : MonoBehaviour
{
    public GrenadeSupply(IntPtr ptr) : base(ptr) { }

    private bool inLevel;

    private void Update()
    {
        try
        {
            var nowInLevel = GameStateManager.CurrentStateName == eGameStateName.InLevel;
            if (!nowInLevel)
            {
                if (inLevel)
                {
                    HaloGrenade.ClearAll();
                    GrenadePanic.Clear();
                }
                inLevel = false;
                return;
            }
            inLevel = true;
            Grenades.Uncap(PlayerBackpackManager.LocalBackpack?.AmmoStorage?.ConsumableAmmo);
            GrenadePanic.Tick();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems grenade upkeep failed: {e.Message}");
        }
    }
}
