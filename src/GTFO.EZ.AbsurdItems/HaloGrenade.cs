using System;
using System.Collections.Generic;
using Agents;
using Enemies;
using GTFO.API;
using Il2CppInterop.Runtime.Attributes;
using MindAttic.GTFO.Shared;
using Player;
using SNetwork;
using UnityEngine;

namespace GTFO.EZ.AbsurdItems;

/// <summary>
/// GAI-T37: one thrown Frag or Plasma Grenade, flown by hand on Halo's fixed 30 Hz tick with Halo 3's
/// speed, gravity and fuses (<see cref="HaloGrenades"/>) - not a Unity Rigidbody, so nothing about
/// Unity's gravity, mass or drag gets in. Every machine flies its own copy from the thrower's throw
/// message along the same path; the thrower's game decides what it hits (an enemy: the frag blows up,
/// the plasma sticks to that body part), when it goes off, and deals the damage (the bullet path, which
/// syncs), and tells everyone else, whose copies follow. The host makes an enemy with a plasma stuck
/// to it panic (<see cref="GrenadePanic"/>).
/// </summary>
internal sealed class HaloGrenade : MonoBehaviour
{
    private const string ThrowEvent = "MindAttic.GTFO.EZ.AbsurdItems.GrenadeThrow";
    private const string StickEvent = "MindAttic.GTFO.EZ.AbsurdItems.GrenadeStick";
    private const string DetonateEvent = "MindAttic.GTFO.EZ.AbsurdItems.GrenadeDetonate";

    // A teammate's copy waits this long past its own fuse for the thrower's detonation message.
    private const float RemoteGraceSeconds = 0.75f;

    private static readonly Dictionary<uint, HaloGrenade> live = new();
    // Teammates' copies that blew on their own fuse before the thrower's detonation arrived.
    private static readonly HashSet<uint> selfDetonated = new();
    private static uint nextId;
    private static Material? glowMaterial;

    private uint id;
    private GrenadeKind kind;
    private PlayerAgent? owner; // the thrower, on the thrower's game only; null on everyone else's copy
    private Vector3 position, previous, velocity, spinAxis;
    private float age, tickTime, spinSpeed, groundBounceAge = -1f, restAge = -1f, nextFlash, lastBounceSound;
    private bool resting, exploded, lit;
    private Dam_EnemyDamageLimb? stuckLimb;
    private EnemyAgent? stuckEnemy;
    private Vector3 stuckOffset;
    private EffectLight light = null!;

    public HaloGrenade(IntPtr ptr) : base(ptr) { }

    [HideFromIl2Cpp]
    internal static System.Numerics.Vector3 ToNumerics(Vector3 v) => new(v.x, v.y, v.z);
    [HideFromIl2Cpp]
    internal static Vector3 ToUnity(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

    [HideFromIl2Cpp]
    internal static Color FlashColor(GrenadeKind kind) =>
        kind == GrenadeKind.Plasma ? new Color(0.25f, 0.55f, 1f, 1f) : new Color(1f, 0.05f, 0.05f, 1f);

    // ---- Network ----

    [HideFromIl2Cpp]
    internal static void Init()
    {
        try
        {
            if (!NetworkAPI.IsEventRegistered(ThrowEvent))
                RemoteEvents.Register<ThrowMessage>(ThrowEvent, (_, m) =>
                    Spawn(m.Id, (GrenadeKind)m.Kind, null, new Vector3(m.X, m.Y, m.Z), new Vector3(m.VelX, m.VelY, m.VelZ)));
            if (!NetworkAPI.IsEventRegistered(StickEvent))
                RemoteEvents.Register<StickMessage>(StickEvent, (_, m) => OnStick(m));
            if (!NetworkAPI.IsEventRegistered(DetonateEvent))
                RemoteEvents.Register<DetonateMessage>(DetonateEvent, (_, m) => OnDetonate(m.Id, new Vector3(m.X, m.Y, m.Z)));
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems couldn't register the grenade network events - teammates won't see your grenades: {e.Message}");
        }
    }

    // Plain fields so GTFO-API can copy them as raw bytes.
    private readonly struct ThrowMessage
    {
        public readonly uint Id;
        public readonly byte Kind;
        public readonly float X, Y, Z, VelX, VelY, VelZ;

        public ThrowMessage(uint id, GrenadeKind kind, Vector3 position, Vector3 velocity)
        {
            Id = id;
            Kind = (byte)kind;
            (X, Y, Z) = (position.x, position.y, position.z);
            (VelX, VelY, VelZ) = (velocity.x, velocity.y, velocity.z);
        }
    }

    // Where a plasma stuck: to the world at X/Y/Z, or (OnEnemy) to that enemy's body part number Limb,
    // at the offset LX/LY/LZ in that part's own space.
    private readonly struct StickMessage
    {
        public readonly uint Id;
        public readonly byte OnEnemy;
        public readonly ushort Enemy;
        public readonly int Limb;
        public readonly float X, Y, Z, LX, LY, LZ;

        public StickMessage(uint id, bool onEnemy, ushort enemy, int limb, Vector3 position, Vector3 offset)
        {
            Id = id;
            OnEnemy = (byte)(onEnemy ? 1 : 0);
            Enemy = enemy;
            Limb = limb;
            (X, Y, Z) = (position.x, position.y, position.z);
            (LX, LY, LZ) = (offset.x, offset.y, offset.z);
        }
    }

    private readonly struct DetonateMessage
    {
        public readonly uint Id;
        public readonly float X, Y, Z;

        public DetonateMessage(uint id, Vector3 position)
        {
            Id = id;
            (X, Y, Z) = (position.x, position.y, position.z);
        }
    }

    [HideFromIl2Cpp]
    private static void Send<T>(string name, T message) where T : struct
    {
        try
        {
            if (SNet.IsInLobby) NetworkAPI.InvokeEvent(name, message);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.EZ.AbsurdItems couldn't send {name}: {e.Message}");
        }
    }

    // ---- Spawning ----

    // The local player's throw: flown here, and sent to everyone else.
    [HideFromIl2Cpp]
    internal static void ThrowLocal(GrenadeKind kind, PlayerAgent owner, Vector3 position, Vector3 velocity)
    {
        var slot = owner.Owner != null ? owner.Owner.PlayerSlotIndex() : 0;
        var id = ((uint)(slot & 0xFF) << 24) | (++nextId & 0xFFFFFF);
        Spawn(id, kind, owner, position, velocity);
        Send(ThrowEvent, new ThrowMessage(id, kind, position, velocity));
    }

    [HideFromIl2Cpp]
    private static void Spawn(uint id, GrenadeKind kind, PlayerAgent? owner, Vector3 position, Vector3 velocity)
    {
        if (live.ContainsKey(id)) return;
        var go = new GameObject($"GTFO.EZ.AbsurdItems {Grenades.Of(kind).Name}");
        go.transform.position = position;
        GrenadeModel.Build(go.transform, kind);

        var grenade = go.AddComponent<HaloGrenade>();
        grenade.id = id;
        grenade.kind = kind;
        grenade.owner = owner;
        grenade.position = grenade.previous = position;
        grenade.velocity = velocity;
        grenade.spinAxis = UnityEngine.Random.onUnitSphere;
        grenade.spinSpeed = UnityEngine.Random.Range(360f, 900f);

        var lightObject = new GameObject("Glow");
        lightObject.transform.SetParent(go.transform, false);
        grenade.light = lightObject.AddComponent<EffectLight>();
        grenade.light.Color = FlashColor(kind);
        grenade.light.Range = kind == GrenadeKind.Plasma ? 3f : 2.5f;
        grenade.light.Intensity = 0f;
        grenade.light.enabled = true;

        live[id] = grenade;
    }

    // Level over: every grenade still in the air is gone.
    [HideFromIl2Cpp]
    internal static void ClearAll()
    {
        foreach (var grenade in live.Values)
            if (grenade != null) Destroy(grenade.gameObject);
        live.Clear();
        selfDetonated.Clear();
    }

    // ---- Messages from the thrower ----

    [HideFromIl2Cpp]
    private static void OnStick(StickMessage m)
    {
        var at = new Vector3(m.X, m.Y, m.Z);
        if (!live.TryGetValue(m.Id, out var grenade) || grenade == null || grenade.exploded) return;
        if (m.OnEnemy == 0)
        {
            grenade.StickToWorld(at);
            return;
        }
        var enemy = FindEnemy(m.Enemy, at);
        var limbs = enemy?.Damage?.DamageLimbs;
        if (enemy != null && limbs != null && m.Limb >= 0 && m.Limb < limbs.Length && limbs[m.Limb] != null)
            grenade.StickToLimb(enemy, limbs[m.Limb], new Vector3(m.LX, m.LY, m.LZ));
        else
            grenade.StickToWorld(at);
    }

    // The enemy with `globalId`, looked for around where it was hit.
    [HideFromIl2Cpp]
    private static EnemyAgent? FindEnemy(ushort globalId, Vector3 near)
    {
        foreach (var collider in Physics.OverlapSphere(near, 3f, LayerManager.MASK_ENEMY_DAMAGABLE))
        {
            var enemy = collider.GetComponent<Dam_EnemyDamageLimb>()?.m_base?.Owner;
            if (enemy != null && enemy.GlobalID == globalId) return enemy;
        }
        return null;
    }

    [HideFromIl2Cpp]
    private static void OnDetonate(uint id, Vector3 position)
    {
        if (live.TryGetValue(id, out var grenade) && grenade != null)
        {
            grenade.position = position;
            grenade.transform.position = position;
            grenade.Explode(fromMessage: true);
        }
        else if (!selfDetonated.Remove(id))
        {
            Explosion.Show(position, GrenadeKind.Frag); // never arrived here; still show the blast
            if (SNet.IsMaster) GrenadePanic.End(id);
        }
        else if (SNet.IsMaster)
        {
            GrenadePanic.End(id);
        }
    }

    // ---- Flight ----

    private void Update()
    {
        if (exploded) return;
        try
        {
            var dt = Time.deltaTime;
            age += dt;

            if (FuseDone())
            {
                Explode();
                return;
            }

            if (stuckLimb != null)
            {
                FollowLimb();
            }
            else if (!resting && restAge < 0f)
            {
                tickTime += dt;
                var ticks = 0;
                while (tickTime >= HaloGrenades.TickSeconds && ticks++ < 8 && !exploded)
                {
                    tickTime -= HaloGrenades.TickSeconds;
                    previous = position;
                    Tick(HaloGrenades.TickSeconds);
                }
                if (exploded) return;
                var blend = resting || restAge >= 0f ? 1f : Mathf.Clamp01(tickTime / HaloGrenades.TickSeconds);
                transform.position = Vector3.Lerp(previous, position, blend);
                if (!resting && restAge < 0f) transform.Rotate(spinAxis, spinSpeed * dt, Space.World);
            }
            Glow();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems grenade failed: {e.Message}");
            exploded = true;
            live.Remove(id);
            Destroy(gameObject);
        }
    }

    // Whose fuse is done: the thrower's copy by Halo's rules; everyone else's waits a little past
    // that for the thrower's message (and then goes off on its own if it never comes).
    [HideFromIl2Cpp]
    private bool FuseDone()
    {
        var at = owner != null ? age : age - RemoteGraceSeconds;
        return kind == GrenadeKind.Plasma
            ? HaloGrenades.PlasmaFuseDone(at, restAge, Grenades.PlasmaStuckFuseSeconds.Value)
            : HaloGrenades.FragFuseDone(at, groundBounceAge);
    }

    // One 30 Hz tick: gravity, then the move, swept against the world (and, on the thrower's game,
    // enemies) - a bounce may use up part of the tick and carry on with the rest.
    [HideFromIl2Cpp]
    private void Tick(float dt)
    {
        var (_, newVelocity) = HaloGrenades.Step(ToNumerics(position), ToNumerics(velocity), HaloGrenades.Gravity, dt);
        velocity = ToUnity(newVelocity);
        var remaining = dt;
        for (var i = 0; i < 3 && remaining > 0f && !exploded && !resting && restAge < 0f; i++)
        {
            var step = velocity * remaining;
            var distance = step.magnitude;
            if (distance < 0.0001f) return;
            var direction = step / distance;
            var radius = HaloGrenades.Radius;

            var hitWorld = Physics.SphereCast(position, radius, direction, out var hit, distance, LayerManager.MASK_WORLD);
            var reach = hitWorld ? hit.distance : distance;

            // An enemy before the world: the thrower's game decides.
            if (owner != null && Physics.SphereCast(position, radius, direction, out var enemyHit, distance, LayerManager.MASK_ENEMY_DAMAGABLE)
                && (!hitWorld || enemyHit.distance < reach))
            {
                var limb = enemyHit.collider != null ? enemyHit.collider.GetComponent<Dam_EnemyDamageLimb>() : null;
                var enemy = limb?.m_base?.Owner;
                if (limb != null && enemy != null && enemy.Alive)
                {
                    position = enemyHit.point;
                    if (kind == GrenadeKind.Frag)
                    {
                        transform.position = position;
                        Explode();
                    }
                    else
                    {
                        StickLocalToEnemy(enemy, limb, enemyHit.point);
                    }
                    return;
                }
            }

            if (!hitWorld)
            {
                position += step;
                return;
            }

            position = hit.point + hit.normal * (radius + 0.01f);
            remaining *= 1f - Mathf.Clamp01(hit.distance / distance);
            if (kind == GrenadeKind.Plasma)
            {
                // Sticks to the world where it hits. The thrower tells everyone where, so every copy agrees.
                StickToWorld(position);
                if (owner != null) Send(StickEvent, new StickMessage(id, false, 0, -1, position, Vector3.zero));
                return;
            }

            velocity = ToUnity(HaloGrenades.Bounce(ToNumerics(velocity), ToNumerics(hit.normal)));
            spinSpeed *= 0.7f;
            if (HaloGrenades.IsGround(hit.normal.y) && groundBounceAge < 0f) groundBounceAge = age;
            if (velocity.magnitude > 2f && age - lastBounceSound > 0.1f)
            {
                lastBounceSound = age;
                Sounds.Play(Sounds.HandGrenadeBounce.Value, hit.point, local: false);
            }
            // Settled on a floor: it lies there for the rest of its fuse.
            if (HaloGrenades.IsGround(hit.normal.y) && velocity.magnitude < HaloGrenades.RestSpeed)
            {
                resting = true;
                velocity = Vector3.zero;
                transform.position = previous = position;
            }
        }
    }

    // ---- Sticking (the Plasma Grenade) ----

    [HideFromIl2Cpp]
    private void StickLocalToEnemy(EnemyAgent enemy, Dam_EnemyDamageLimb limb, Vector3 point)
    {
        var offset = limb.transform.InverseTransformPoint(point);
        var index = LimbIndex(enemy, limb);
        StickToLimb(enemy, limb, offset);
        if (index >= 0) Send(StickEvent, new StickMessage(id, true, enemy.GlobalID, index, point, offset));
        else Send(StickEvent, new StickMessage(id, false, 0, -1, point, Vector3.zero));
    }

    [HideFromIl2Cpp]
    private static int LimbIndex(EnemyAgent enemy, Dam_EnemyDamageLimb limb)
    {
        var limbs = enemy.Damage?.DamageLimbs;
        if (limbs == null) return -1;
        for (var i = 0; i < limbs.Length; i++)
            if (limbs[i] != null && limbs[i].Pointer == limb.Pointer) return i;
        return -1;
    }

    [HideFromIl2Cpp]
    private void StickToLimb(EnemyAgent enemy, Dam_EnemyDamageLimb limb, Vector3 offset)
    {
        if (restAge < 0f) restAge = age;
        stuckEnemy = enemy;
        stuckLimb = limb;
        stuckOffset = offset;
        velocity = Vector3.zero;
        FollowLimb();
        Sounds.Play(Sounds.HandGrenadeBounce.Value, transform.position, local: false);
        if (SNet.IsMaster) GrenadePanic.Begin(enemy, id, Grenades.PlasmaStuckFuseSeconds.Value + HaloGrenades.PlasmaMinFuse + RemoteGraceSeconds + 1f);
    }

    [HideFromIl2Cpp]
    private void StickToWorld(Vector3 at)
    {
        if (stuckLimb != null) return;
        if (restAge < 0f) restAge = age;
        position = previous = at;
        velocity = Vector3.zero;
        transform.position = at;
        Sounds.Play(Sounds.HandGrenadeBounce.Value, at, local: false);
    }

    // Rides along on the body part it's stuck to (not parented: a dying enemy's parts may be
    // destroyed, and the grenade must still go off where it was).
    [HideFromIl2Cpp]
    private void FollowLimb()
    {
        if (stuckLimb == null) return;
        try
        {
            var part = stuckLimb.transform;
            if (part == null) return;
            position = previous = part.TransformPoint(stuckOffset);
            transform.position = position;
        }
        catch (Exception)
        {
            stuckLimb = null; // gone with its enemy; stays where it last was
        }
    }

    // ---- Look ----

    // The frag blinks red faster and faster, beeping; the plasma glows blue, pulsing faster.
    [HideFromIl2Cpp]
    private void Glow()
    {
        var fuse = kind == GrenadeKind.Plasma
            ? Mathf.Max(HaloGrenades.PlasmaMinFuse, (restAge >= 0f ? restAge : age) + Grenades.PlasmaStuckFuseSeconds.Value)
            : Mathf.Max(HaloGrenades.FragMinFuse, groundBounceAge >= 0f ? groundBounceAge + HaloGrenades.FragFuseAfterGroundBounce : HaloGrenades.SafetyFuse);
        var progress = Mathf.Clamp01(age / Mathf.Max(0.01f, fuse));
        light.transform.position = transform.position + Vector3.up * 0.1f;
        if (kind == GrenadeKind.Plasma)
        {
            var pulse = 0.5f + 0.5f * Mathf.Sin(age * Mathf.Lerp(10f, 40f, progress));
            light.Intensity = 2f + 3f * pulse;
            return;
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
        light.Intensity = lit ? 3f + 3f * progress : 0f;
    }

    // The plasma's glowing blue core (shared material).
    [HideFromIl2Cpp]
    internal static Material GlowMaterial()
    {
        if (glowMaterial != null) return glowMaterial; // Unity's null: rebuilt if unloaded
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        glowMaterial = new Material(shader) { color = new Color(0.35f, 0.7f, 1f, 1f) };
        return glowMaterial;
    }

    // ---- Going off ----

    [HideFromIl2Cpp]
    private void Explode(bool fromMessage = false)
    {
        if (exploded) return;
        exploded = true;
        live.Remove(id);
        var at = transform.position;
        Destroy(gameObject);
        Explosion.Show(at, kind);
        if (SNet.IsMaster) GrenadePanic.End(id);

        if (owner == null)
        {
            // Someone else's: if this was its own fuse, the thrower's message is still coming.
            if (!fromMessage) selfDetonated.Add(id);
            return;
        }
        Send(DetonateEvent, new DetonateMessage(id, at));
        try
        {
            var source = owner.Cast<Agent>();
            // The plasma's stuck hit, on the body part it's stuck to, before the blast.
            if (kind == GrenadeKind.Plasma && stuckLimb != null && stuckEnemy != null && stuckEnemy.Alive)
            {
                var direction = (stuckLimb.transform.position - at).sqrMagnitude > 0.0001f ? (stuckLimb.transform.position - at).normalized : Vector3.up;
                stuckLimb.BulletDamage(Grenades.PlasmaStuckDamage.Value, source, at, direction, -direction,
                    allowDirectionalBonus: false, Grenades.Plasma.BlastStagger.Value, 1f, Grenades.Plasma.ItemId);
            }
            Blast(kind, at, source);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdItems grenade blast failed: {e.Message}");
        }
    }

    // The blast's damage, by Halo's falloff: call it only on the thrower's game.
    [HideFromIl2Cpp]
    internal static void Blast(GrenadeKind kind, Vector3 at, Agent source)
    {
        var type = Grenades.Of(kind);
        var edge = kind == GrenadeKind.Plasma ? HaloGrenades.PlasmaEdgeFraction : HaloGrenades.FragEdgeFraction;
        AreaDamage.Apply(at, HaloGrenades.BlastInnerRadius, HaloGrenades.BlastOuterRadius, type.BlastDamage.Value, edge,
            type.BlastStagger.Value, source, type.ItemId);
    }
}
