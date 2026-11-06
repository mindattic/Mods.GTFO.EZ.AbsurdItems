using System.Numerics;
using NUnit.Framework;

namespace GTFO.EZ.AbsurdItems.Tests;

// GAI-T37: the Frag and Plasma Grenades' Halo 3 numbers, converted to metres.
[TestFixture]
public class HaloGrenadesTests
{
    [Test]
    public void Conversions_UseTenFeetPerWorldUnit()
    {
        Assert.That(HaloGrenades.ThrowSpeed, Is.EqualTo(30.48f).Within(1e-4));
        Assert.That(HaloGrenades.Gravity, Is.EqualTo(0.9f * 4.17126f * 3.048f).Within(1e-4));
        Assert.That(HaloGrenades.BlastInnerRadius, Is.EqualTo(1.524f).Within(1e-4));
        Assert.That(HaloGrenades.BlastOuterRadius, Is.EqualTo(5.334f).Within(1e-4));
    }

    [Test]
    public void LaunchVelocity_IsAlongTheAimAtThrowSpeed()
    {
        var v = HaloGrenades.LaunchVelocity(new Vector3(0f, 0f, 2f));
        Assert.That(v.Length(), Is.EqualTo(HaloGrenades.ThrowSpeed).Within(1e-3));
        Assert.That(v.Z, Is.EqualTo(HaloGrenades.ThrowSpeed).Within(1e-3));
    }

    [Test]
    public void Step_FallsByGravityOverOneSecond()
    {
        var position = Vector3.Zero;
        var velocity = new Vector3(0f, 0f, 10f);
        for (var i = 0; i < 30; i++)
            (position, velocity) = HaloGrenades.Step(position, velocity, HaloGrenades.Gravity, HaloGrenades.TickSeconds);
        Assert.That(velocity.Y, Is.EqualTo(-HaloGrenades.Gravity).Within(1e-3));
        Assert.That(position.Z, Is.EqualTo(10f).Within(1e-3));
        // Semi-implicit Euler at 30 Hz: within 2% of the exact g/2 drop.
        Assert.That(-position.Y, Is.EqualTo(HaloGrenades.Gravity / 2f).Within(HaloGrenades.Gravity * 0.02f));
    }

    [Test]
    public void Bounce_ReversesAndDampsIntoTheSurfaceAndSlowsAlongIt()
    {
        var v = HaloGrenades.Bounce(new Vector3(4f, -10f, 0f), Vector3.UnitY);
        Assert.That(v.Y, Is.EqualTo(10f * HaloGrenades.BounceRestitution).Within(1e-5));
        Assert.That(v.X, Is.EqualTo(4f * (1f - HaloGrenades.BounceFriction)).Within(1e-5));
    }

    [Test]
    public void FragFuse_HalfASecondAfterTheFirstGroundBounceButNotBefore1_3()
    {
        Assert.That(HaloGrenades.FragFuseDone(1.2f, 0.2f), Is.False);
        Assert.That(HaloGrenades.FragFuseDone(1.3f, 0.2f), Is.True);
        Assert.That(HaloGrenades.FragFuseDone(1.9f, 1.5f), Is.False);
        Assert.That(HaloGrenades.FragFuseDone(2.0f, 1.5f), Is.True);
        Assert.That(HaloGrenades.FragFuseDone(5.9f, -1f), Is.False);
        Assert.That(HaloGrenades.FragFuseDone(HaloGrenades.SafetyFuse, -1f), Is.True);
    }

    [Test]
    public void PlasmaFuse_AfterStickingButNotBefore1_3()
    {
        Assert.That(HaloGrenades.PlasmaFuseDone(1.0f, 0.1f), Is.False);
        Assert.That(HaloGrenades.PlasmaFuseDone(1.3f, 0.1f), Is.True);
        Assert.That(HaloGrenades.PlasmaFuseDone(1.5f, 0.7f), Is.False);
        Assert.That(HaloGrenades.PlasmaFuseDone(1.6f, 0.7f), Is.True);
        Assert.That(HaloGrenades.PlasmaFuseDone(3.0f, 0.5f, afterRest: 3f), Is.False);
        Assert.That(HaloGrenades.PlasmaFuseDone(3.5f, 0.5f, afterRest: 3f), Is.True);
        Assert.That(HaloGrenades.PlasmaFuseDone(2f, -1f), Is.False);
    }

    [Test]
    public void Falloff_FullInsideTheInnerRadiusThenLinearToTheEdge()
    {
        Assert.That(HaloGrenades.Falloff(1f, 1.5f, 5f, 0.25f), Is.EqualTo(1f));
        Assert.That(HaloGrenades.Falloff(5f, 1.5f, 5f, 0.25f), Is.EqualTo(0.25f).Within(1e-6));
        Assert.That(HaloGrenades.Falloff(3.25f, 1.5f, 5f, 0.25f), Is.EqualTo(0.625f).Within(1e-6));
        Assert.That(HaloGrenades.Falloff(9f, 1.5f, 5f, 0.25f), Is.EqualTo(0.25f).Within(1e-6));
    }
}
