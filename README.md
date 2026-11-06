# GTFO.EZ.AbsurdItems

![GTFO.EZ.AbsurdItems](logo.png)

*(Listed on Thunderstore as `GtfoEZAbsurdItems`. Package names there can only use letters, numbers
and underscores, so they can't contain a period.)*

GTFO.EZ.AbsurdItems is a GTFO.EZ companion mod, built on [GTFO.EZ](https://github.com/mindattic/GTFO.EZ)'s core: it carries GTFO.EZ's shared code (the Insert/Delete infinite health and ammo toggles, and MessageHelper, which puts on-screen messages wherever they fit) and works with or without GTFO.EZ installed. It was called GTFO.AbsurdItems before its first release.

A BepInEx plugin for GTFO that changes items and pickups: artifacts buff whoever picks them up for
the rest of the level, three new consumables - the **Frag Grenade** and **Plasma Grenade** (Halo 3's, thrown instantly with
Halo's physics) and the **Stim Pack** - and the consumables found in lockers, boxes and lying around
are a few small health, tool and ammo kits plus those three. No game files are shipped or edited.

**Every feature has its own on/off switch, and its numbers can be retuned,** in the config file
BepInEx creates on first launch: `BepInEx\config\MindAttic.GTFO.EZ.AbsurdItems.cfg`. Its first entry,
`[0. Mod] Enabled`, is the master switch: set it to `false` and the whole mod is off - nothing is
patched, no item is added, no hotkey or loot change - and the other mods sharing its code treat it
as not installed.

## What it changes

| Feature | Vanilla | Default | Switch |
|---|---|---|---|
| [Artifact buffs](#artifact-buffs) (Bold / Aggressive / Muted) | count toward boosters only | +5% max health / +5% damage / 5% harder to detect, each, for the level | `[Artifacts] Artifact Buffs Enabled` |
| [Artifact glow](#artifact-glow) | plain model | glows and is tinted in its category's color: Aggressive red, Muted blue, Bold green | `[Artifacts] Artifact Glow Enabled` |
| [Breadcrumbs](#breadcrumbs) | nothing marks where you've been | a glow stick marker drops at an opened container, an unlocked door, an opened door and a newly entered area | `[Breadcrumbs]` (one switch per kind) |
| [Frag and Plasma Grenades](#frag-and-plasma-grenades) (new consumables) | - | Halo 3's grenades, thrown the instant you press fire with Halo's speed, gravity and fuses; the frag blows on hitting an enemy, the plasma sticks and makes it panic; found as loot, 2-5 a pickup, up to 20 carried | `[Frag Grenade] Frag Grenade Enabled`, `[Plasma Grenade] Plasma Grenade Enabled` |
| [Stim Pack](#stim-pack) (new consumable) | - | heals 33% of max health and can overheal to 200%, draining back down; +33% damage for 15 s | `[Stim Pack] Stim Pack Enabled` |
| [Consumables are kits](#consumables-are-kits) (lockers, boxes, lying around) | mixed items | a few small health, tool and ammo kits, 1-2 charges each, adding about 20% to the zone's resource packs | `[Loot] Consumables Are Kits` |
| [Loot mix](#loot-mix) | - | Frag and Plasma Grenades and Stim Packs join the kits; 60% of the loot lies out in the open | `[Loot] Loot Mix Enabled` |
| [Infinite ammo / health](#infinite-ammo-and-health) | none | **Insert** / **Delete** toggles, off at the start | - |

Artifact buffs and *Consumables Are Kits* moved here from GTFO.EZ, the Hand Grenade (since split into
the Frag and Plasma Grenades), Stim Pack and Loot Mix from GTFO.EZ.AbsurdWeapons - each with the same settings and defaults, except that the kit
settings both of those mods had are now one set (see [`[Loot]`](#loot)).

## Artifact buffs

Picking up an artifact buffs whoever picked it up until the level ends: each **Bold** (the game's
Uncommon) adds max health, healed too; each **Aggressive** (Rare) adds damage to enemies, guns and
melee; each **Muted** (Common) makes you harder for enemies to detect, capped at 75%. A checkpoint
restart that puts an artifact back in the level takes its buff off again. Every player's pickups
count on every game, so the host's enemies hear the Muted and each player's own hits carry the
Aggressive; max health only changes on your own game. Every pickup is also announced in everyone's
event log - "Bob picked up a Bold artifact: +5% max health, Bold x3" - followed by the team's tally,
e.g. "Team artifacts - Bob: Bold 3 | Amy: Aggressive 1, Muted 2".

### `[Artifacts]`

| Setting | Default | What it does |
|---|---|---|
| `Artifact Buffs Enabled` | true | Turns artifact buffs on or off. |
| `Bold: Max Health (%)` | 5 | Max health added per Bold artifact, in percent of your starting max health. |
| `Aggressive: Damage (%)` | 5 | Damage to enemies (guns, melee and grenades) added per Aggressive artifact. |
| `Muted: Quieter (%)` | 5 | How much harder you are to detect per Muted artifact (capped at 75%). |

## Artifact glow

Every artifact still lying in the level glows softly in its category's color and its own model is
tinted to match, so which buff it gives is visible from across the room, not just once you're close
enough to read its name: **Aggressive** (Rare) red, **Muted** (Common) blue, **Bold** (Uncommon)
green. The light and tint are attached to the artifact itself, so they follow it, disappear with it
on pickup, and come back on their own if a checkpoint restart puts the artifact back in the level.

| Setting | Default | What it does |
|---|---|---|
| `Artifact Glow Enabled` | true | Turns the glow and model tint on or off. |

## Breadcrumbs

A small glowing marker - a cosmetic stand-in for a glow stick, no pickup behavior of its own - drops
wherever the team has already been or already looted: a resource container (locker/case) opened, a
door unlocked, a door opened, or a new area walked into for the first time. A glance at the trail
says what's already been picked over and which way you've already come. Each marker drops once per
container, door or area for the level - a checkpoint restart doesn't duplicate them. New-area
markers are dropped for every agent - you, your teammates and bots - each in that player's own
colour, so the trail shows who has been where (one marker per player per area).

### `[Breadcrumbs]`

| Setting | Default | What it does |
|---|---|---|
| `Mark Opened Containers` | true | Drops a marker at a resource container (locker/case) the moment it's opened. |
| `Mark Unlocked Doors` | true | Drops a marker at a door the moment it's unlocked. |
| `Mark Opened Doors` | true | Drops a marker at a door the moment it's opened. |
| `Mark New Areas` | true | Drops a marker, in that player's colour, the first time anyone (you, a teammate or a bot) walks into a new area. |

## Consumables are kits

Consumables found in lockers, boxes or lying around are the game's own health, tool and ammo packs,
as small kits of 1 or 2 charges (one charge is one use, 20% of a pack), and sparse: together they
add about 20% (*Kit Loot*) to the zone's normal resource packs, which spawn as usual. The split
between health, ammo and tool kits follows the zone's own resource budget. Turn it off for vanilla
consumables (and no Loot Mix). The host's settings decide what spawns (see Compatibility).

With **GTFO.EZ** installed, its data changes (consumable spawn multiplier, resource budget) apply
first, and its own consumable spawn odds are skipped while this is on.

## Loot mix

On top of the kits, with *Loot Mix Enabled* (on by default): **Frag Grenades** and **Plasma Grenades**
at 2 each (4 together) to the kits' 3, so grenades are common, and **Stim Packs** at 1 - all in the same lottery as the kits, so the kits
still add their 20% and these come on top. 60% of the loot (*OutInTheOpenShare*) lies out in the
open, like artifacts, instead of in boxes and lockers. Off: kits only, where the game puts them.
It's one kit-loot run either way, with one set of kit settings.

### `[Loot]`

All of it takes effect on restart.

| Setting | Default | What it does |
|---|---|---|
| `Consumables Are Kits` | true | Turns kit loot on or off. Off: vanilla consumables, and no Loot Mix. |
| `Kit Loot (%)` | 20 | How much the kits add, in percent of a zone's normal health, ammo and tool pack budget (averaged over the difficulty tiers). |
| `Health Kit Weight` | 1 | Multiplies the odds a kit is a health kit (the zone's budget already sets the split; 1 keeps it). 0 leaves health kits out. |
| `Tool Kit Weight` | 1 | The same for tool kits. |
| `Ammo Kit Weight` | 1 | The same for ammo kits. |
| `Kit Charges Min` | 1 | Fewest charges a kit holds. |
| `Kit Charges Max` | 2 | Most charges a kit holds. |
| `Loot Mix Enabled` | true | Frag and Plasma Grenades and Stim Packs join the kits, and *OutInTheOpenShare* of the loot lies out in the open. |
| `FragGrenadeWeight` | 2 | With Loot Mix on: odds a loot consumable is a Frag Grenade, against the kits' weights (1 each). |
| `PlasmaGrenadeWeight` | 2 | With Loot Mix on: odds it's a Plasma Grenade. (Together they're the old `HandGrenadeWeight`, 4, which is split between them once.) |
| `StimPackWeight` | 1 | With Loot Mix on: relative odds it's a Stim Pack (only while the Stim Pack is switched on). |
| `OutInTheOpenShare` | 0.6 | With Loot Mix on: share (0-1) of the loot left lying out in the open instead of in boxes and lockers. |

With every kit weight at 0, no kits spawn - only Loot Mix's grenades and Stim Packs, if it's on.

GTFO.EZ.AbsurdWeapons' Loot Mix had its own copies of the kit settings (`KitLootPercent`,
`HealthKitWeight`, `ToolKitWeight`, `AmmoKitWeight`, `KitChargesMin`, `KitChargesMax`); they're
merged into GTFO.EZ's names above (`Kit Loot (%)`, `Health Kit Weight`, ...), which mean the same
and have the same defaults. Its `Loot Mix Enabled`, `StimPackWeight` and `OutInTheOpenShare` keep
their names; its `HandGrenadeWeight` is split between `FragGrenadeWeight` and `PlasmaGrenadeWeight`.

## Frag and Plasma Grenades

Two consumables built on Halo 3's grenades, with Halo's numbers converted to GTFO's metres (Halo's
world unit is taken as 10 ft = 3.048 m). **Press fire and it's thrown at once** - no wind-up - straight
along your aim at **30.5 m/s** (Halo 3: 10 world units/s), under **11.4 m/s2** of its own gravity
(Halo 3: 90% of Halo's gravity), flown on Halo's 30-ticks-a-second step so it takes the same path on
every player's screen. The next one is ready about 0.6 s later.

- **Frag Grenade** - the lock melter's look, blinking red faster and faster. It bounces off walls and
  floors (keeping about half its speed) and goes off **0.5 s after its first bounce off the ground,
  never sooner than 1.3 s after the throw** (Halo 3's fuse) - or **the moment it hits an enemy**.
- **Plasma Grenade** - the same shell with a glowing blue core and a blue light. It **sticks to the
  first thing it hits**: a wall, a floor, or an enemy, riding along on that enemy's body part. It goes
  off **0.9 s after sticking, never sooner than 1.3 s after the throw** (Halo 3; `StuckFuseSeconds` -
  Halo: Combat Evolved's was 3 s). **An enemy with one stuck to it panics**: it stops attacking and
  chasing and runs - away from the players, or (half the time, rolled per enemy) into its nearest
  friends, taking the blast to them. It never comes at you.

Both blasts are Halo 3's shape: full damage out to 1.5 m, falling to Halo's edge share at 5.3 m (the
frag to 60/160 of it, the plasma to a quarter), x6 stagger, and they break padlocks in reach. They
never hurt players. Halo's damage numbers are against Halo's health, so only its ratios carry over:
the frag does **60** at the center, the plasma **90** (Halo's 240 to 160) plus **112.5** to the enemy
it's stuck to (Halo's "300, attached only"). Damage boosts (Aggressive artifacts, a Stim Pack) add to
all of it.

Nobody starts with any: **each one you find** as loot (see [Loot mix](#loot-mix)) **holds 2 to 5**
(picked once per pickup, the same for every player), up to **20** carried. Each is its own consumable
with its own stack; the one consumable slot holds one kind at a time, so picking up the other kind
swaps it for what you carry, like any two different consumables. Thrown ones are gone. The terminal
lists them as Frag / Plasma Grenades (FRAG_GRENADE, PLASMA_GRENADE), not glow sticks.

**In multiplayer** every throw is sent to the whole team and flown on every game; the thrower's game
decides what it hits and when it goes off, deals the damage (which syncs the game's own way) and
tells everyone else, so sticks and blasts land in the same place for all. The host runs the panic -
enemies are the host's - and everyone sees it through the game's own enemy sync.

What's Halo's and what's an estimate: the throw speed, the 90% gravity, both fuses, the blast radii
and the damage ratios are Halo 3's own values (Halopedia's H3:Frag Grenade and H3:Plasma Grenade
pages). Halo's base gravity (4.17 world units/s2, about 12.7 m/s2) is an estimate - no source states
it; it fits Halo 3's jump timing (a 3.08 wu/s jump lasting about 1.4 s gives 4.4). The bounce (half
the speed kept, 30% lost along the surface), the throw leaving straight along the crosshair, the
grenade's size and the time between throws are estimates too. Every value and its source is
commented in `src/GTFO.EZ.AbsurdItems/Pure/HaloGrenades.cs`.

### `[Frag Grenade]` and `[Plasma Grenade]`

The same keys in each section unless marked.

| Setting | Default | What it does |
|---|---|---|
| `Frag Grenade Enabled` / `Plasma Grenade Enabled` | true | Whether it spawns at all (takes effect on restart). Off: not in the loot. The item is registered either way, so everyone in a lobby holds the same items. |
| `BlastDamage` | 60 frag / 90 plasma | Damage at the blast's center, before damage boosts. |
| `BlastStagger` | 6 | Blast stagger multiplier. |
| `PickupCountMin` | 2 | Fewest one pickup holds; each holds a random count from this to `PickupCountMax` (takes effect on restart). |
| `PickupCountMax` | 5 | Most one pickup holds (takes effect on restart). |
| `MaxCarried` | 20 | The most you can carry. |
| `StuckDamage` (plasma) | 112.5 | Extra damage to the enemy it's stuck to, on top of the blast. |
| `StuckFuseSeconds` (plasma) | 0.9 | Seconds from sticking until it goes off (never sooner than 1.3 s after the throw). Halo: Combat Evolved's 3 gives a longer panic. |
| `Panic` (plasma) | true | Whether a stuck enemy panics. |
| `PanicTowardEnemiesChance` (plasma) | 0.5 | Chance (0-1), per stuck enemy, that it runs into the nearest other enemies instead of away from the players. |

**From the Hand Grenade:** your `[Hand Grenade]` settings seed both grenades once: its switch,
`MaxCarried`, pickup counts and stagger go to both, its `BlastDamage` to the frag (and x1.5 / x1.875
to the plasma's blast and stuck damage), and `[Loot] HandGrenadeWeight` is split in half between
`FragGrenadeWeight` and `PlasmaGrenadeWeight`. Its `FuseSeconds`, `BlastRadius`, `BlastEdgeFraction`
and `ThrowForceMultiplier` are gone - Halo's values replace them.

### `[Sounds]`

| Setting | Default | What it does |
|---|---|---|
| `HandGrenadeThrow` | FRAGGRENADETHROW | Throwing a grenade (the key keeps the Hand Grenade's name). Empty = silent. |
| `HandGrenadeBounce` | FRAGGRENADEBOUNCE | A Frag Grenade bouncing, or a Plasma Grenade sticking. Empty = silent. |

## Stim Pack

A consumable of its own - the I2-LP syringe's item cloned, so the I2-LP itself is left as it is -
that's an **overheal like Doom's**. It gives **33%** of your max health and can take you **past
max, up to 200%**; anything above max then **drains back down to 100%** at 1% of max a second.
Damage eats into the overheal first. For **15 seconds** after a Stim Pack, everything you hit (guns
and melee) takes **33% more damage**. The overheal only adds and takes back its own share of max
health, so a Bold artifact's bonus survives it. Found as loot (see [Loot mix](#loot-mix)).

### `[Stim Pack]`

| Setting | Default | What it does |
|---|---|---|
| `Stim Pack Enabled` | true | Whether the Stim Pack spawns at all (takes effect on restart). Off: not in the loot. The item is registered either way, so everyone in a lobby holds the same items. |
| `HealPercent` | 33 | Health a Stim Pack gives, in percent of max health. |
| `MaxHealthPercent` | 200 | How high it can take your health, in percent of max health. |
| `DrainPercentPerSecond` | 1 | How fast health above max drains back to max, in percent of max a second. |
| `DamageBoostPercent` | 33 | Extra damage dealt (guns, melee and grenades) while the boost runs, in percent. |
| `DamageBoostSeconds` | 15 | How long the damage boost lasts after a Stim Pack. |

## Infinite ammo and health

**Insert** toggles infinite health (no damage to anyone on the team, bots included, whoever presses it) and
**Delete** infinite ammo (every gun, tool, placed sentry and grenade stays full for the whole team, no reloads), each announced in the event
log and always in the console too. Both start off. They're the same two keys GTFO.EZ, its Absurd
companions and GTFO.CrashTestDummies all have, from one shared copy of the code written in GTFO.EZ;
with several of the mods installed, only one answers, so a press never toggles
twice.

## Compatibility

- **GTFO.EZ** used to have artifact buffs and *Consumables Are Kits*; both moved here. Running an
  older GTFO.EZ build alongside this mod buffs each artifact twice: switch one of them off in its
  config.
- **GTFO.EZ.AbsurdWeapons** used to have the Hand Grenade, Stim Pack and Loot Mix; all three moved
  here. Running an older GTFO.EZ.AbsurdWeapons build alongside this mod adds both mods' copies of the
  items (the second one to load finds the item id taken and skips it) and fills the loot twice:
  update it, or switch its Hand Grenade, Stim Pack and Loot Mix off. Otherwise compatible - it
  leaves artifacts in place, so artifact buffs work with it installed.
- **Your old settings carry over.** On first run, each moved setting that isn't in this mod's
  `.cfg` yet is copied from `MindAttic.GTFO.EZ.cfg` (artifact buffs, *Consumables Are Kits* and the
  kit settings) or `MindAttic.GTFO.EZ.AbsurdWeapons.cfg` (Hand Grenade - into both grenades, Stim Pack, their sounds, Loot
  Mix - and the kit settings instead, if its Loot Mix was on, since those were the ones in use).
  After that this mod's `.cfg` is the one that counts; a setting already in it is never overwritten.
- **GTFO.EZ.AbsurdWeapons' Shadow Dagger** patches the same four enemy-targeting methods the Plasma
  Grenade's panic does (each only ever turns a target away); the panic's patches are pinned after
  it.
- **Multiplayer, with GTFO.EZ:** the settings that shape the level - all of `[Loot]` (kit loot and Loot
  Mix), `Frag Grenade Enabled` / `Plasma Grenade Enabled` / `Stim Pack Enabled` and the grenades'
  `PickupCountMin` / `PickupCountMax` - follow the host: in someone else's lobby your game uses the
  host's values (sent by GTFO.EZ's level-settings sync) before the level is built, so everyone's loot
  and pickup counts match, and your `.cfg` is left as it is. Without GTFO.EZ, or with a host whose
  GTFO.EZ doesn't send them, your own values are used.
- **Turning a mod off:** every GTFO.* mod's `.cfg` starts with `[0. Mod] Enabled`; a mod switched
  off there counts as not installed for the others sharing its code.
- Artifact buffs are confirmed in play. Muted scales the game's per-player detection modifier
  (`PlayerAgent.GetDetectionMod`).

## Versioning

v1, v2, v3... - major-only bumps, matching the convention across other MindAttic projects.
Thunderstore requires `manifest.json`'s `version_number` to be strict `x.y.z`, so "v18" is `18.0.0`
(minor/patch stay 0). `package/CHANGELOG.md` headers use the short `vN` form.

The GTFO.EZ family - GTFO.EZ, GTFO.EZ.AbsurdWeapons, GTFO.EZ.AbsurdMoves and GTFO.EZ.AbsurdItems -
shares one version number, so a matching set is easy to spot: the companions' first release is
**v13**, in step with GTFO.EZ (not v1), and every release of the family bumps all four together
(current: v20).

## Building

```powershell
.\build.ps1                         # builds into the "GTFO - Easy Mode DEV" mod-manager profile and zips package\ into dist\
.\build.ps1 -Profile "My Profile"   # another Thunderstore Mod Manager profile
```

Needs the .NET 6 SDK and a GTFO profile with BepInEx installed (BepInEx's `core` and `interop`
DLLs are the build references).

## Project layout

```
src/GTFO.EZ.AbsurdItems/
  Plugin.cs              BepInEx entry point: binds each feature's config, applies Harmony patches
  ArtifactBuffs.cs       Per-player artifact counts and the max health, damage and detection buffs
  ArtifactGlow.cs        Lights and tints each artifact in its category's color
  Breadcrumbs.cs         Glow stick markers at opened containers/doors and newly entered areas
  ConsumablesAreKits.cs  Runs the shared kit loot at GameDataInit, after GTFO.EZ's tweaks, with Loot
                         Mix's Frag/Plasma Grenades and Stim Packs as extras
  Grenades.cs            Frag and Plasma Grenade items: glowstick clones, looks, instant throw, pickup counts, cap
  HaloGrenade.cs         A thrown grenade flown with Halo 3's physics: bounce/stick, fuses, blast, team sync
  GrenadePanic.cs        Host: an enemy with a plasma stuck to it stops targeting players and runs
  StimPack.cs            Stim Pack consumable: I2-LP clone, overheal, damage boost
  Blast.cs               The grenades' explosion, area damage, padlock damage, model and flash
  Sounds.cs              The grenades' [Sounds] events
  HostLevelSettings.cs   The host's level settings in someone else's lobby (filled by GTFO.EZ)
  ConfigImport.cs        Carries moved settings over from GTFO.EZ's and GTFO.EZ.AbsurdWeapons' cfgs, once
  DataBlocks.cs          Datablock cloning
  Pure/Formulas.cs       The items' numbers (no game types; unit tested)
  Pure/HaloGrenades.cs   Halo 3's grenade values converted to metres, with sources (unit tested)
  LocalPlayerDamage.cs   Tells your own damage component from the host's bots'
  NonNegative.cs         Holds percentage and weight settings at 0 or more
  Shared/                linked from external/GTFO.EZ.Shared (a git submodule; change it there):
                         SharedOwner, the Insert/Delete toggles, MessageHelper and the kit loot (KitLoot.cs)
tests/GTFO.EZ.AbsurdItems.Tests/  NUnit tests for Pure/ - `dotnet test tests/GTFO.EZ.AbsurdItems.Tests`, no game needed
package/                 Thunderstore manifest, icon (256x256, from logo.png), README and changelog
logo.png                 Full-size logo
```
