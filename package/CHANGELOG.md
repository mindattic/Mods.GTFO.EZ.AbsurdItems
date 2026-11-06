# Changelog

## 21.0.0

- **Loot settings follow the host (GAI-T40).** With GTFO.EZ installed, everything here that shapes
  the level - `[Loot]` (Consumables Are Kits, Kit Loot, the kit weights and charges, Loot Mix, the
  grenade/Stim Pack weights, OutInTheOpenShare), the Frag Grenade / Plasma Grenade / Stim Pack
  switches and the grenades' pickup counts - is the host's in someone else's lobby, so a client's
  consumable tables and grenade pickups match the host's instead of following its own `.cfg` (which
  is left untouched). GTFO.EZ sends them with its own level settings and runs the kit loot again
  with them before the level is built. Without GTFO.EZ your own values are used, as before.

- **The Hand Grenade is now two Halo 3 grenades, thrown instantly (GAI-T37).** Press fire and it's
  thrown at once - no wind-up - along your aim at Halo 3's speed (10 world units/s = 30.5 m/s, at
  1 wu = 3.048 m) under its own 90%-of-Halo gravity (about 11.4 m/s2), flown on Halo's 30 Hz tick so
  every player sees the same path. The **Frag Grenade** bounces and goes off 0.5 s after its first
  bounce off the ground (never before 1.3 s) - or the moment it hits an enemy. The **Plasma Grenade**
  (blue core and glow) sticks to the first wall, floor or enemy it hits, riding along on that enemy,
  and goes off 0.9 s after sticking (never before 1.3 s; `StuckFuseSeconds`). **An enemy with a
  plasma stuck to it panics**: it stops attacking and chasing and runs away from the players, or
  into its nearest friends - never at you (host-run, seen by all). Blasts follow Halo 3's falloff
  (full to 1.5 m, down to its edge share at 5.3 m); Halo's base gravity, the bounce, the throw angle
  and the time between throws are documented estimates (see the README). New `[Frag Grenade]` and
  `[Plasma Grenade]` sections and `[Loot] FragGrenadeWeight` / `PlasmaGrenadeWeight` (2 each); your
  `[Hand Grenade]` settings and `HandGrenadeWeight` seed them once, and its fuse, radius, edge share
  and throw strength settings are gone. Throws, sticks and blasts reach teammates over GTFO-API.

- **Bug fix (GAI-T39, for GTFO.EZ's G.EZ-T125 level-settings sync):** switching the Hand Grenade or
  Stim Pack off used to skip registering its item, so a host and a client with different switches
  held different items - a level could hold a pickup the other game had never heard of. Both items
  are now always registered; `Hand Grenade Enabled` / `Stim Pack Enabled` only decide whether they
  spawn. GTFO.EZ's next release also warns in the event log when your AbsurdItems level
  settings ([Loot], Hand Grenade pickup counts, these switches) differ from the host's.
- **Artifact pickups are announced to the whole team.** Every player's event log says who picked up
  what and what it gave ("Bob picked up a Bold artifact: +5% max health, Bold x3"), then the team's
  tally ("Team artifacts - Bob: Bold 3 | Amy: Aggressive 1, Muted 2").
- **New-area breadcrumbs for everyone, in their own colour.** Teammates' and bots' first steps into
  a new area now drop a marker too (it used to be only yours), tinted in that player's colour.
- **Hand Grenade pickups hold 2 to 5** (was 1), a whole number picked once per pickup from its
  level-generation seed, so every player sees the same count and whoever picks it up gets it.
  `[Hand Grenade] PickupCount` is replaced by `PickupCountMin` (2) and `PickupCountMax` (5).
- **Carry up to 20 Hand Grenades** (`MaxCarried`, was 6). An install still on the old default 6 is
  moved to 20 once; a value you set yourself is kept.

## v20

- Bumps the `BepInExPack_GTFO` dependency to 3.2.4.
- Moves in step with the GTFO.EZ family's version number (GTFO.EZ, GTFO.EZ.AbsurdWeapons,
  GTFO.EZ.AbsurdMoves and GTFO.EZ.AbsurdItems share one).

## v19

- Exhaustive bug-hunt pass over kit loot, Loot Mix (Hand Grenades/Stim Packs), the out-in-the-open
  share, and artifact glow/buffs: confirmed kits are purely additive on top of the zone's own
  resource budget, grenades and stim packs are in the default loot lottery and genuinely spawn out
  in the open, and artifact buffs persist for the whole level (only a checkpoint restart of that
  artifact removes them). No real bugs found - behavior unchanged.
- Moves in step with the GTFO.EZ family's version number (GTFO.EZ, GTFO.EZ.AbsurdWeapons,
  GTFO.EZ.AbsurdMoves and GTFO.EZ.AbsurdItems share one).

## v18

- Moves in step with the GTFO.EZ family's version number (GTFO.EZ, GTFO.EZ.AbsurdWeapons,
  GTFO.EZ.AbsurdMoves and GTFO.EZ.AbsurdItems share one). No change in this mod's own behavior.

## v17

- `GTFO.EZ.Shared` submodule bump: `SharedOwner.Order` no longer lists `GTFO.CrashTestDummies`,
  which dropped Insert/Delete (and this submodule) in its own v17. No change in this mod's own
  behavior.
- `GTFO.CrashTestDummies` and `GTFO.SkipDrop` join the `vN` scheme this release, so every GTFO.*
  mod now shares one version number.

## v16

- **Insert and Delete swapped: Insert is now infinite health, Delete is now infinite ammo.** Both
  also always announce in the console now, not just the event log. Shared code
  (`GTFO.EZ.Shared`), so GTFO.EZ, its Absurd companions and GTFO.CrashTestDummies all pick it up.
- Moves in step with the GTFO.EZ family's version number (GTFO.EZ, GTFO.EZ.AbsurdWeapons,
  GTFO.EZ.AbsurdMoves and GTFO.EZ.AbsurdItems share one).

## v15

- **Artifact glow:** artifacts still lying in the level now glow and are tinted in their category's
  color - Aggressive (Rare) red, Muted (Common) blue, Bold (Uncommon) green - so which buff one gives
  is visible from across the room. New `[Artifacts] Artifact Glow Enabled`.
- **Breadcrumbs:** a glow stick marker now drops wherever the team has already been or already
  looted - an opened resource container (locker/case), an unlocked door, an opened door, or a newly
  entered area - so a glance says what's been picked over and which way you've come. New
  `[Breadcrumbs]` section, one switch per kind.
- Moves in step with the GTFO.EZ family's version number (GTFO.EZ, GTFO.EZ.AbsurdWeapons,
  GTFO.EZ.AbsurdMoves and GTFO.EZ.AbsurdItems share one).

## v14

- **Bold artifact fix:** picking up a Bold artifact during a Stim Pack overheal no longer cuts health
  back to max; health above the old max is left to drain.
- **Hand Grenade fix:** the carry cap (`MaxCarried`) was also applied to teammates' consumable
  storage on your game while you held Hand Grenades; now only your own storage gets it.
- Config and README wording: Hand Grenade damage counts the Aggressive and Stim Pack damage boosts.
- Moves in step with the GTFO.EZ family's version number (GTFO.EZ, GTFO.EZ.AbsurdWeapons,
  GTFO.EZ.AbsurdMoves and GTFO.EZ.AbsurdItems share one).

## v13

- **Versioning:** the GTFO.EZ family - GTFO.EZ, GTFO.EZ.AbsurdWeapons, GTFO.EZ.AbsurdMoves and
  GTFO.EZ.AbsurdItems - now shares one version number, so this first release is **v13**, in step
  with GTFO.EZ, rather than v1.
- Renamed from GTFO.AbsurdItems to **GTFO.EZ.AbsurdItems** before release, as a GTFO.EZ companion mod built
  on GTFO.EZ's core (plugin GUID `MindAttic.GTFO.EZ.AbsurdItems`, config file
  `MindAttic.GTFO.EZ.AbsurdItems.cfg`, Thunderstore package `GtfoEZAbsurdItems`).
- Initial release. Every feature has its own on/off switch in the config.
- Master switch: `[0. Mod] Enabled` (default `true`) is the config file's first entry; set it to
  `false` and the whole mod is off - nothing is patched, no item is added, no hotkey or loot change.
  The other GTFO.* mods then treat it as not installed.
- **Artifact buffs** (moved here from GTFO.EZ, same settings): picking up an artifact buffs whoever
  picked it up for the rest of the level - each **Bold** +5% max health (healed too), each
  **Aggressive** +5% damage to enemies, each **Muted** 5% harder for enemies to detect you (capped
  at 75%). A checkpoint restart that puts an artifact back takes its buff off again.
- **Consumables Are Kits** (moved here from GTFO.EZ, same settings): consumables found in lockers,
  boxes or lying around are a few small health, tool and ammo kits of 1-2 charges each, adding about
  20% to the zone's normal resource packs, split the way the zone's resource budget is. *Kit Loot
  (%)*, *Kit Charges Min/Max* and the per-kind weights are configurable.
- **Hand Grenade** (moved here from GTFO.EZ.AbsurdWeapons, same `[Hand Grenade]` and `[Sounds]`
  settings): a consumable lock melter thrown like a glowstick that blows after 3 s (60 damage
  falling to a quarter at 6 m, x6 stagger, breaks padlocks, never hurts players). Found only as
  loot, 1 a pickup, up to 6 carried. The terminal lists it as a Hand Grenade (key `HAND_GRENADE`),
  not as the glow stick it is cloned from.
- **Stim Pack** (moved here from GTFO.EZ.AbsurdWeapons, same `[Stim Pack]` settings): the I2-LP
  cloned as a consumable of its own - heals 33% of max health and can overheal to 200%, draining
  back at 1% a second, and boosts your damage 33% for 15 s.
- **Loot Mix** (moved here from GTFO.EZ.AbsurdWeapons): with *Consumables Are Kits* on, Hand Grenades
  (`HandGrenadeWeight` 4) and Stim Packs (`StimPackWeight` 1) join the kits in one kit-loot run, and
  60% of the loot lies out in the open (`OutInTheOpenShare`). `Loot Mix Enabled` keeps its name.
  AbsurdWeapons' own copies of the kit settings (`KitLootPercent`, `HealthKitWeight`,
  `ToolKitWeight`, `AmmoKitWeight`, `KitChargesMin/Max`) are merged into the GTFO.EZ names here
  (`Kit Loot (%)`, `Health Kit Weight`, ..., `Kit Charges Min/Max`), same meanings and defaults.
- **Insert** (infinite ammo) and **Delete** (infinite health) toggles, announced as warden
  messages - the shared keys every GTFO.* mod has, so they never clash with several installed.
- Every Hand Grenade and Stim Pack number is held at 0 or more, like the other settings (a typo'd
  minus sign lands on 0: a negative `DrainPercentPerSecond` made the overheal grow without end), and
  `PickupCount` / `MaxCarried` at 1 or more (below that, pickups were empty).
- Settings carry over from GTFO.EZ and GTFO.EZ.AbsurdWeapons: on first run, every moved setting not yet
  in this mod's `.cfg` is copied from theirs (for the kit settings, from whichever mod's loot was in
  use), instead of starting from the defaults.
