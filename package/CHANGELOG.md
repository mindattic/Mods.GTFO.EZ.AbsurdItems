# Changelog

## Unreleased

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
