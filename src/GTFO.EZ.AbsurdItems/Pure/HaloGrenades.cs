using System;
using System.Numerics;

namespace GTFO.EZ.AbsurdItems;

/// <summary>
/// GAI-T37: the Frag and Plasma Grenades' flight, fuse and blast, taken from Halo 3 and converted to
/// GTFO's metres. Kept free of game and Unity types (System.Numerics only) so it's unit tested directly.
///
/// Units: Halo measures distance in world units (wu). The usual conversion - and the one used here -
/// is 1 wu = 10 ft = 3.048 m (the Halo Editing Kit's "about 10 units = 1 foot" for its 3ds Max units,
/// with 100 of those to a world unit; c20.reclaimers.net "Scale and units"; Open Carnage's "Set up
/// metric units in Blender"). GTFO is built in Unity metres (a player is about 1.8 m tall), so every
/// Halo distance below is simply multiplied by 3.048 - no extra rescaling to the characters' heights.
///
/// What's sourced and what's estimated is said on each value. Sourced = Halopedia's per-game
/// gameplay pages, which list the tag values ("H3:Frag Grenade", "H3:Plasma Grenade",
/// "CE:Frag Grenade", "CE:Plasma Grenade", "H3ODST:Spike Grenade").
/// </summary>
internal static class HaloGrenades
{
    // ---- Units and time ----

    // 1 wu = 10 ft = 3.048 m (see above). Exact by definition of the conversion, not of Halo itself.
    internal const float MetresPerWorldUnit = 3.048f;

    // Halo simulates projectiles once per game tick at 30 ticks a second (c20.reclaimers.net,
    // "projectile" tag: "the game being simulated at 30 ticks per second" - stated for Halo CE; Halo 3
    // also runs its simulation at 30 Hz). Flown here on the same fixed 30 Hz tick, so a throw follows
    // the same path on every machine whatever its frame rate.
    internal const float TickSeconds = 1f / 30f;

    // ---- Throw ----

    // Throw speed: 10 wu/s for both the Halo 3 frag and plasma grenade (Halopedia H3:Frag Grenade and
    // H3:Plasma Grenade, "Projectile speed: 10 wu/s"; also the Halo CE value). 10 x 3.048 = 30.48 m/s.
    internal const float ThrowSpeedWorldUnits = 10f;
    internal static float ThrowSpeed => ThrowSpeedWorldUnits * MetresPerWorldUnit;

    // ESTIMATE: no source found for an upward offset between the crosshair and the throw direction,
    // so it's thrown straight along the aim and the arc comes from gravity alone.
    internal static readonly float LaunchPitchDegrees = 0f;

    // ESTIMATE: Halo's grenades aren't described anywhere as inheriting the thrower's own movement,
    // so they don't here either (the throw speed is the same standing or running).

    // ESTIMATE: how soon the next grenade can be thrown - roughly Halo 3's throw animation. Not a tag
    // value found anywhere.
    internal const float ThrowRecoverySeconds = 0.6f;

    // ---- Gravity ----

    // ESTIMATE (the least certain value here): Halo's base gravity. 4.17126 wu/s2 is the Halo 2/3
    // globals physics gravity as remembered from the tag layout; no page reachable online states it.
    // Cross-checks: Halo 3's jump speed is 3.08 wu/s (Halopedia "Master Chief (gameplay)") and a full
    // jump is timed at about 1.4 s (bungie.net forum post 3291870), giving 2 x 3.08 / 1.4 = 4.4 wu/s2;
    // 4.17126 predicts 1.48 s. A bungie.net fall test measured about 14.9 m/s2 (4.9 wu/s2, timed by
    // hand over a 102 m drop). So Halo's gravity is roughly 4.2-4.9 wu/s2, i.e. 12.7-14.9 m/s2 - heavier
    // than Earth's 9.81, which is why Halo grenades drop faster than a real one would.
    internal const float BaseGravityWorldUnits = 4.17126f;

    // Both Halo 3 grenades fly at 90% gravity (Halopedia H3:Frag Grenade and H3:Plasma Grenade,
    // "90% gravity"). 0.9 x 4.17126 x 3.048 = 11.44 m/s2. Applied by hand to the grenade only - Unity's
    // global Physics.gravity (9.81) is never touched.
    internal const float GravityScale = 0.9f;
    internal static float Gravity => GravityScale * BaseGravityWorldUnits * MetresPerWorldUnit;

    // ESTIMATE: Halo's projectile movement is described as velocity plus gravity each tick (c20
    // "projectile"), with no air drag for a grenade, so there's none here. Mass doesn't enter it either:
    // a Halo projectile's path doesn't depend on a mass, and these grenades are flown by hand (not a
    // Unity Rigidbody), so no Rigidbody mass/drag is involved at all.
    internal static readonly float AirDrag = 0f;

    // ---- Bounce (the Frag Grenade; the Plasma Grenade sticks instead) ----

    // ESTIMATE: Halo's projectile tags have a "material response" with parallel/perpendicular friction
    // (c20 "projectile"), but no page gives the frag grenade's numbers. Chosen to match how a Halo 3 frag
    // looks: about half its speed off a wall or floor, a little sliding friction along it.
    internal const float BounceRestitution = 0.5f;   // share of the speed into the surface kept, reversed
    internal const float BounceFriction = 0.3f;      // share of the speed along the surface lost

    // ESTIMATE: the grenade's collision size (a Halo frag is a fist-sized ball) and the speed below
    // which it settles on a floor ("comes to rest").
    internal const float Radius = 0.08f;
    internal const float RestSpeed = 0.6f;

    // ---- Fuses ----

    // Frag: "Detonates 0.5s after first bounce off ground, no sooner than 1.3s after being thrown"
    // (Halopedia H3:Frag Grenade). This mod also blows it up the moment it hits an enemy - not Halo
    // (a Halo frag bounces off people), asked for in GAI-T37.
    internal const float FragFuseAfterGroundBounce = 0.5f;
    internal const float FragMinFuse = 1.3f;

    // Plasma: "Detonates 0.9s after coming to rest, no sooner than 1.3s after being thrown"; "Attaches
    // to units" (Halopedia H3:Plasma Grenade). Stuck counts as coming to rest. (Halo CE's plasma had a
    // 3 s fuse after sticking - Halopedia CE:Plasma Grenade - so the stuck fuse is a setting, default
    // Halo 3's.)
    internal const float PlasmaFuseAfterRest = 0.9f;
    internal const float PlasmaMinFuse = 1.3f;

    // Not Halo: a grenade that never touches a floor (stuck in odd geometry) still goes off.
    internal const float SafetyFuse = 6f;

    // A floor, for "bounce off ground": a surface facing mostly up. ESTIMATE of what Halo counts as ground.
    internal static bool IsGround(float normalY) => normalY > 0.6f;

    // Whether a Frag Grenade's fuse is done `age` seconds after the throw, its first ground bounce
    // having been at `groundBounceAge` (negative: none yet).
    internal static bool FragFuseDone(float age, float groundBounceAge) =>
        age >= SafetyFuse || (groundBounceAge >= 0f && age >= Math.Max(FragMinFuse, groundBounceAge + FragFuseAfterGroundBounce));

    // Whether a Plasma Grenade's fuse is done: `restAge` is when it stuck (negative: still flying),
    // `afterRest`/`minimum` the stuck fuse and the shortest fuse from the throw.
    internal static bool PlasmaFuseDone(float age, float restAge, float afterRest = PlasmaFuseAfterRest, float minimum = PlasmaMinFuse) =>
        age >= Math.Max(SafetyFuse, minimum + afterRest) || (restAge >= 0f && age >= Math.Max(minimum, restAge + afterRest));

    // ---- Blast ----

    // Both Halo 3 grenades: full damage out to 0.5 wu, falling to the edge value at 1.75 wu (Halopedia:
    // frag "160 out to 0.5 wu, down to 60 at 1.75 wu"; plasma "240 out to 0.5 wu, down to 60 at 1.75 wu";
    // plasma "300 (attached only)" on the one it's stuck to). 0.5 wu = 1.52 m, 1.75 wu = 5.33 m.
    internal const float BlastInnerWorldUnits = 0.5f;
    internal const float BlastOuterWorldUnits = 1.75f;
    internal static float BlastInnerRadius => BlastInnerWorldUnits * MetresPerWorldUnit;
    internal static float BlastOuterRadius => BlastOuterWorldUnits * MetresPerWorldUnit;

    // Halo's damage numbers are against Halo's health; GTFO's enemies have their own, so only the
    // ratios carry over: the edge shares and plasma vs frag. Frag edge 60/160, plasma edge 60/240,
    // plasma splash 240/160 of the frag's, the stuck hit 300/160 of the frag's splash.
    internal const float FragEdgeFraction = 60f / 160f;
    internal const float PlasmaEdgeFraction = 60f / 240f;
    internal const float PlasmaToFragDamage = 240f / 160f;
    internal const float PlasmaAttachedToFragDamage = 300f / 160f;

    // Halo's falloff: full damage inside `inner`, then linear down to `edgeFraction` at `outer`;
    // nothing beyond (the caller only looks within `outer`).
    internal static float Falloff(float distance, float inner, float outer, float edgeFraction)
    {
        if (distance <= inner || outer <= inner) return 1f;
        var t = Math.Clamp((distance - inner) / (outer - inner), 0f, 1f);
        return 1f + (edgeFraction - 1f) * t;
    }

    // ---- Flight ----

    // The throw's velocity: `aim` (unit length, y up) tilted up by LaunchPitchDegrees, at ThrowSpeed.
    internal static Vector3 LaunchVelocity(Vector3 aim)
    {
        aim = aim.LengthSquared() > 1e-8f ? Vector3.Normalize(aim) : Vector3.UnitZ;
        if (LaunchPitchDegrees != 0f)
        {
            var flat = new Vector3(aim.X, 0f, aim.Z);
            var flatLength = flat.Length();
            var pitch = MathF.Atan2(aim.Y, flatLength) + LaunchPitchDegrees * MathF.PI / 180f;
            var horizontal = flatLength > 1e-6f ? flat / flatLength : Vector3.UnitZ;
            aim = horizontal * MathF.Cos(pitch) + Vector3.UnitY * MathF.Sin(pitch);
        }
        return aim * ThrowSpeed;
    }

    // One tick of free flight: gravity first, then the move (semi-implicit Euler - ESTIMATE of Halo's
    // order; over a 30 Hz tick the two orders differ by under 2 cm a tick at these speeds).
    internal static (Vector3 Position, Vector3 Velocity) Step(Vector3 position, Vector3 velocity, float gravity, float dt)
    {
        velocity.Y -= gravity * dt;
        if (AirDrag > 0f) velocity *= MathF.Max(0f, 1f - AirDrag * dt);
        return (position + velocity * dt, velocity);
    }

    // Velocity after bouncing off a surface with unit `normal`: the part into it reversed and scaled by
    // BounceRestitution, the part along it scaled by 1 - BounceFriction.
    internal static Vector3 Bounce(Vector3 velocity, Vector3 normal)
    {
        var into = Vector3.Dot(velocity, normal);
        var normalPart = normal * into;
        var along = velocity - normalPart;
        return along * (1f - BounceFriction) - normalPart * BounceRestitution;
    }
}
