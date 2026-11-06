using System;
using System.Collections.Generic;
using AIGraph;
using BepInEx.Configuration;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using LevelGeneration;
using Player;
using UnityEngine;

namespace GTFO.EZ.AbsurdItems;

/// <summary>
/// A small glowing marker - a cosmetic stand-in for a glow stick, built from a primitive rather than
/// the real item's mesh, with no pickup behavior of its own - drops wherever the team has already
/// been or already looted: a resource container (locker/case) opened, a door unlocked, a door
/// opened, or a new area walked into for the first time. A glance at the trail says what's already
/// been picked over and which way you've already come. Each kind of marker has its own on/off
/// switch; every marker drops once per container/door/area, even across checkpoint restarts within
/// the same life (nothing is un-marked by <see cref="ResetForLevel"/> until the level actually ends).
/// New-area markers are dropped for every agent - you, your teammates and bots - not just you (GAI-T38):
/// each marks the areas it walks into first, in that player's own colour, so the trail says who's been where.
/// </summary>
internal static class Breadcrumbs
{
    internal static ConfigEntry<bool> ContainersEnabled = null!;
    internal static ConfigEntry<bool> DoorsUnlockedEnabled = null!;
    internal static ConfigEntry<bool> DoorsOpenedEnabled = null!;
    internal static ConfigEntry<bool> AreasEnabled = null!;

    private static readonly Color MarkerColor = new(0.65f, 0.95f, 0.25f); // glow-stick green-yellow
    private const float Range = 2f;
    private const float Intensity = 1f;

    internal static void Bind(ConfigFile config)
    {
        const string s = "Breadcrumbs";
        ContainersEnabled = config.Bind(s, "Mark Opened Containers", true,
            "Drops a glow stick marker wherever a resource container (locker/case) is opened, so the team can see " +
            "what's already been looted.");
        DoorsUnlockedEnabled = config.Bind(s, "Mark Unlocked Doors", true,
            "Drops a glow stick marker at a door the moment it's unlocked.");
        DoorsOpenedEnabled = config.Bind(s, "Mark Opened Doors", true,
            "Drops a glow stick marker at a door the moment it's opened.");
        AreasEnabled = config.Bind(s, "Mark New Areas", true,
            "Drops a glow stick marker the first time anyone - you, a teammate or a bot - walks into a new area, " +
            "in that player's colour, leaving a breadcrumb trail of where each of you has already been.");
    }

    private static readonly HashSet<IntPtr> openedContainers = new();
    private static readonly HashSet<IntPtr> unlockedDoors = new();
    private static readonly HashSet<IntPtr> openedDoors = new();
    // Per player: each agent marks the areas it's first into, so the same area can carry several colours.
    private static readonly HashSet<(IntPtr Area, ulong Player)> visitedAreas = new();

    // The markers dropped this level. They're plain root objects the level's own cleanup doesn't
    // know about, so without this they'd stay floating where the last level's doors and lockers were.
    private static readonly List<GameObject> markers = new();

    internal static void ResetForLevel()
    {
        foreach (var marker in markers)
            if (marker != null) UnityEngine.Object.Destroy(marker);
        markers.Clear();
        openedContainers.Clear();
        unlockedDoors.Clear();
        openedDoors.Clear();
        visitedAreas.Clear();
    }

    [HideFromIl2Cpp]
    internal static void ContainerOpened(IntPtr container, Vector3 position)
    {
        if (!ContainersEnabled.Value || !openedContainers.Add(container)) return;
        Drop(position);
    }

    [HideFromIl2Cpp]
    internal static void DoorUnlocked(IntPtr door, Vector3 position)
    {
        if (!DoorsUnlockedEnabled.Value || !unlockedDoors.Add(door)) return;
        Drop(position);
    }

    [HideFromIl2Cpp]
    internal static void DoorOpened(IntPtr door, Vector3 position)
    {
        if (!DoorsOpenedEnabled.Value || !openedDoors.Add(door)) return;
        Drop(position);
    }

    [HideFromIl2Cpp]
    internal static void AreaEntered(IntPtr area, ulong player, Color color, Vector3 position)
    {
        if (!AreasEnabled.Value || !visitedAreas.Add((area, player))) return;
        Drop(position, color);
    }

    [HideFromIl2Cpp]
    private static void Drop(Vector3 position) => Drop(position, MarkerColor);

    [HideFromIl2Cpp]
    private static void Drop(Vector3 position, Color color)
    {
        try
        {
            var go = new GameObject("GTFO.EZ.AbsurdItems Breadcrumb");
            markers.Add(go);
            go.transform.position = position + Vector3.up * 0.05f;
            BuildModel(go.transform, color);
            var light = go.AddComponent<EffectLight>();
            light.Color = color;
            light.Range = Range;
            light.Intensity = Intensity;
            light.enabled = true;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems breadcrumb marker failed: {e.Message}");
        }
    }

    // A small capsule standing in for a glow stick: no real item or pickup behavior, just the look.
    [HideFromIl2Cpp]
    private static void BuildModel(Transform parent, Color color)
    {
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        UnityEngine.Object.Destroy(capsule.GetComponent<Collider>());
        capsule.transform.SetParent(parent, false);
        capsule.transform.localScale = new Vector3(0.05f, 0.09f, 0.05f);
        var renderer = capsule.GetComponent<Renderer>();
        if (renderer == null) return;
        // Shader.Find returns null when the build doesn't ship the shader; a Material can't be made
        // from that, and the throw would skip the marker's light. Tint the capsule's own material then.
        var shader = Shader.Find("Unlit/Color");
        if (shader != null) renderer.material = new Material(shader) { color = color };
        else renderer.material.color = color;
    }
}

[HarmonyPatch]
internal static class Breadcrumbs_Patches
{
    [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.Setup))]
    [HarmonyPostfix]
    private static void AfterLevelSetup(Dam_PlayerDamageLocal __instance)
    {
        if (LocalPlayerDamage.IsMine(__instance)) Breadcrumbs.ResetForLevel();
    }

    [HarmonyPatch(typeof(LG_WeakResourceContainer), nameof(LG_WeakResourceContainer.OnSyncStateChanged))]
    [HarmonyPostfix]
    private static void AfterContainerStateChanged(LG_WeakResourceContainer __instance, eResourceContainerStatus status, bool isRecall)
    {
        if (isRecall || status != eResourceContainerStatus.Open) return;
        try
        {
            Breadcrumbs.ContainerOpened(__instance.Pointer, __instance.transform.position);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems: couldn't mark an opened container. {e.Message}");
        }
    }

    [HarmonyPatch(typeof(LG_WeakDoor), nameof(LG_WeakDoor.LastStatus), MethodType.Setter)]
    [HarmonyPostfix]
    private static void AfterWeakDoorStatus(LG_WeakDoor __instance, eDoorStatus value)
    {
        try
        {
            Mark(value, __instance.Pointer, __instance.transform.position);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems: couldn't mark a weak door. {e.Message}");
        }
    }

    [HarmonyPatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.LastStatus), MethodType.Setter)]
    [HarmonyPostfix]
    private static void AfterSecurityDoorStatus(LG_SecurityDoor __instance, eDoorStatus value)
    {
        try
        {
            Mark(value, __instance.Pointer, __instance.transform.position);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems: couldn't mark a security door. {e.Message}");
        }
    }

    // The player's own colour (the one the game uses for their name and marker), at full opacity
    // for the glow; the plain glow-stick green when it can't be read.
    private static Color PlayerColor(SNetwork.SNet_Player owner)
    {
        try
        {
            var c = owner.PlayerColor;
            return new Color(c.r, c.g, c.b, 1f);
        }
        catch (Exception)
        {
            return new Color(0.65f, 0.95f, 0.25f);
        }
    }

    private static void Mark(eDoorStatus status, IntPtr door, Vector3 position)
    {
        if (status == eDoorStatus.Unlocked) Breadcrumbs.DoorUnlocked(door, position);
        else if (status == eDoorStatus.Open) Breadcrumbs.DoorOpened(door, position);
    }

    [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.CourseNode), MethodType.Setter)]
    [HarmonyPostfix]
    private static void AfterSetCourseNode(PlayerAgent __instance, AIG_CourseNode value)
    {
        try
        {
            // Every agent, not just the local player (GAI-T38): each machine sees every agent's course
            // node change, so each drops the same per-player trail.
            if (value == null) return;
            var area = value.m_area;
            var owner = __instance.Owner;
            if (area == null || owner == null) return;
            Breadcrumbs.AreaEntered(area.Pointer, owner.Lookup, PlayerColor(owner), __instance.Position);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems: couldn't mark a new area. {e.Message}");
        }
    }
}
