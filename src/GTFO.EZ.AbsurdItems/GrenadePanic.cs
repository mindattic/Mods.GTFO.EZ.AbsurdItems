using System;
using System.Collections.Generic;
using Agents;
using Enemies;
using HarmonyLib;
using Player;
using SNetwork;
using UnityEngine;
using UnityEngine.AI;

namespace GTFO.EZ.AbsurdItems;

/// <summary>
/// GAI-T37: an enemy with a Plasma Grenade stuck to it panics until the grenade goes off. Runs on the
/// host only - enemies are simulated there, and their movement and states reach everyone else through
/// the game's own enemy sync, so every player sees the same panic.
///
/// While panicking its own behaviour (EnemyBehaviour.UpdateState: target picking, chasing, attacks,
/// screams) doesn't run, it can't detect, take or be handed a player as a target, any attack already
/// under way is cut off, and it runs - every half second it's sent somewhere new: away from the
/// nearest player, or (rolled once per stuck enemy, <see cref="Grenades.PlasmaPanicTowardEnemiesChance"/>)
/// into the nearest other enemy, taking the blast to them. It never comes at a player on purpose.
/// Left alone: a sleeping (hibernating) enemy stays asleep, and a flying one only stops targeting
/// (its flight isn't on the ground navmesh this steers); hit reactions, knockdowns, being glued,
/// climbing and jumping play out first.
/// </summary>
[HarmonyPatch]
internal static class GrenadePanic
{
    private const string AbsurdWeaponsGuid = "MindAttic.GTFO.EZ.AbsurdWeapons";
    private const float RepathSeconds = 0.5f;
    private const float FleeDistance = 12f;
    private const float FindOthersRadius = 30f;

    private sealed class Panic
    {
        internal EnemyAgent Enemy = null!;
        internal uint Grenade;
        internal float Until, NextPath;
        internal bool TowardEnemies;
    }

    // Keyed by the enemy's pointer.
    private static readonly Dictionary<IntPtr, Panic> panicking = new();

    internal static bool Any => panicking.Count > 0;

    internal static void Begin(EnemyAgent enemy, uint grenade, float maxSeconds)
    {
        try
        {
            if (!SNet.IsMaster || !Grenades.PlasmaPanicEnabled.Value || enemy == null || !enemy.Alive) return;
            var ai = enemy.AI;
            if (ai == null || ai.Mode == AgentMode.Hibernate) return;
            var toward = UnityEngine.Random.value < Grenades.PlasmaPanicTowardEnemiesChance.Value;
            panicking[enemy.Pointer] = new Panic { Enemy = enemy, Grenade = grenade, Until = Time.time + maxSeconds, TowardEnemies = toward };
            Plugin.Logger.LogInfo($"GTFO.EZ.AbsurdItems: a stuck Plasma Grenade panics {enemy.EnemyData?.name ?? "an enemy"} ({(toward ? "into the other enemies" : "away from the players")}).");
            Steer(panicking[enemy.Pointer]);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't panic an enemy: {e.Message}");
        }
    }

    // Its grenade went off (or was cleared): back to normal.
    internal static void End(uint grenade)
    {
        if (panicking.Count == 0) return;
        var done = new List<IntPtr>();
        foreach (var pair in panicking)
            if (pair.Value.Grenade == grenade) done.Add(pair.Key);
        foreach (var key in done) panicking.Remove(key);
    }

    internal static void Clear() => panicking.Clear();

    internal static bool IsPanicking(EnemyAgent? enemy) => panicking.Count > 0 && enemy != null && panicking.ContainsKey(enemy.Pointer);

    private static bool IsPanicking(EnemyAI? ai)
    {
        if (panicking.Count == 0 || ai == null) return false;
        try { return IsPanicking(ai.m_enemyAgent); }
        catch (Exception) { return false; }
    }

    // Every frame on the host.
    internal static void Tick()
    {
        if (panicking.Count == 0) return;
        if (!SNet.IsMaster)
        {
            panicking.Clear();
            return;
        }
        var now = Time.time;
        List<IntPtr>? done = null;
        foreach (var pair in panicking)
        {
            var panic = pair.Value;
            bool alive;
            try { alive = panic.Enemy != null && panic.Enemy.Alive; }
            catch (Exception) { alive = false; }
            if (!alive || now >= panic.Until)
            {
                (done ??= new List<IntPtr>()).Add(pair.Key);
                continue;
            }
            if (now < panic.NextPath) continue;
            try
            {
                Steer(panic);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't steer a panicking enemy: {e.Message}");
                (done ??= new List<IntPtr>()).Add(pair.Key);
            }
        }
        if (done != null) foreach (var key in done) panicking.Remove(key);
    }

    // States a panicking enemy is pulled out of into running: standing, attacking, screaming.
    private static bool Interruptible(ES_StateEnum state) => state is ES_StateEnum.None or ES_StateEnum.StandStill or ES_StateEnum.PathMove
        or ES_StateEnum.StrikerAttack or ES_StateEnum.StrikerMelee or ES_StateEnum.ShooterAttack or ES_StateEnum.TankAttack
        or ES_StateEnum.TankMultiTargetAttack or ES_StateEnum.Scream or ES_StateEnum.ScoutScream or ES_StateEnum.ScoutDetection
        or ES_StateEnum.TriggerFogSphere or ES_StateEnum.BirtherGiveBirth or ES_StateEnum.TentacleDragMove;

    private static void Steer(Panic panic)
    {
        panic.NextPath = Time.time + RepathSeconds;
        var enemy = panic.Enemy;
        var ai = enemy.AI;
        var locomotion = enemy.Locomotion;
        var navigation = ai?.m_navMeshAgent;
        if (ai == null || locomotion == null || navigation == null) return;
        // A flyer has no ground path to steer; it just stops targeting players (the patches below).
        if (locomotion.PathMove == null || locomotion.PathMove.TryCast<ES_PathMove>() == null) return;

        var from = enemy.Position;
        var goal = panic.TowardEnemies && TryNearestOtherEnemy(enemy, from, out var other) ? other : FleePoint(from);

        var state = locomotion.CurrentStateEnum;
        if (!Interruptible(state)) return;
        navigation.isStopped = false;
        navigation.SetDestination(goal);
        if (state != ES_StateEnum.PathMove) locomotion.ChangeState(ES_StateEnum.PathMove);
        try { locomotion.PathMove.TryCast<ES_PathMove>()?.SetMoveSpeed(EnemyMoveSpeed.Fast); } catch (Exception) { /* default speed */ }
    }

    private static bool TryNearestOtherEnemy(EnemyAgent self, Vector3 from, out Vector3 at)
    {
        at = from;
        var best = float.MaxValue;
        foreach (var collider in Physics.OverlapSphere(from, FindOthersRadius, LayerManager.MASK_ENEMY_DAMAGABLE))
        {
            var other = collider.GetComponent<Dam_EnemyDamageLimb>()?.m_base?.Owner;
            if (other == null || other.Pointer == self.Pointer || !other.Alive) continue;
            var position = other.Position;
            var distance = (position - from).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            at = position;
        }
        return best < float.MaxValue;
    }

    // A reachable spot about FleeDistance away from the nearest player (trying the straight-away
    // direction first, then up to 90 degrees either side).
    private static Vector3 FleePoint(Vector3 from)
    {
        var away = Vector3.zero;
        var nearest = float.MaxValue;
        foreach (var player in PlayerManager.PlayerAgentsInLevel)
        {
            if (player == null || !player.Alive) continue;
            var offset = from - player.Position;
            offset.y = 0f;
            if (offset.sqrMagnitude >= nearest) continue;
            nearest = offset.sqrMagnitude;
            away = offset;
        }
        if (away.sqrMagnitude < 0.01f) away = UnityEngine.Random.insideUnitSphere;
        away.y = 0f;
        away = away.sqrMagnitude > 1e-6f ? away.normalized : Vector3.forward;

        foreach (var angle in new[] { 0f, 35f, -35f, 70f, -70f, 90f, -90f })
        {
            var candidate = from + Quaternion.Euler(0f, angle, 0f) * away * FleeDistance;
            if (NavMesh.SamplePosition(candidate, out var hit, 4f, -1) /* every area */) return hit.position;
        }
        return from + away * FleeDistance;
    }

    // ---- No player targets while panicking (these decide on the host) ----
    // GTFO.EZ.AbsurdWeapons' Shadow Dagger patches the same four methods the same way (to hide a
    // stealthed player); each only ever turns a target away, so the order doesn't change the result -
    // it's pinned (after it) anyway, per the family's patch-ordering rule.

    // Its own behaviour - picking targets, chasing, attacking, screaming - is skipped; Steer drives it.
    [HarmonyPatch(typeof(EnemyBehaviour), nameof(EnemyBehaviour.UpdateState))]
    [HarmonyPrefix]
    private static bool BeforeBehaviour(EnemyBehaviour __instance) => panicking.Count == 0 || !IsPanicking(__instance.m_ai);

    [HarmonyPatch(typeof(EnemyDetection), nameof(EnemyDetection.IsTargetValid))]
    [HarmonyPostfix]
    [HarmonyAfter(AbsurdWeaponsGuid)]
    private static void TargetValid(EnemyDetection __instance, ref bool __result)
    {
        if (__result && panicking.Count > 0 && IsPanicking(__instance.m_ai)) __result = false;
    }

    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SetDetectedAgent))]
    [HarmonyPrefix]
    [HarmonyAfter(AbsurdWeaponsGuid)]
    private static bool DetectedAgent(EnemyAI __instance) => panicking.Count == 0 || !IsPanicking(__instance);

    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SetTarget))]
    [HarmonyPrefix]
    [HarmonyAfter(AbsurdWeaponsGuid)]
    private static bool Target(EnemyAI __instance) => panicking.Count == 0 || !IsPanicking(__instance);

    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.InjectPropagatedTarget))]
    [HarmonyPrefix]
    [HarmonyAfter(AbsurdWeaponsGuid)]
    private static bool PropagatedTarget(EnemyAI __instance) => panicking.Count == 0 || !IsPanicking(__instance);
}
