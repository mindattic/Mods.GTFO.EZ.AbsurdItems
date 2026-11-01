---
codex: 1
project: Mods.GTFO.EZ.AbsurdItems
code: GAI
layer: stories
status: living
updated: 2026-10-18
---
<!-- GENERATED from MindAttic.Tickets/data/Tickets.db by `tickets db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the tickets CLI. -->

# Mods.GTFO.EZ.AbsurdItems — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Priority backlog
_Nothing recorded yet._

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| GAI-T1 | 2026-10-18 | docs | closed | Start the permanent ticket log in the workflow database | — |
| GAI-T2 | 2026-10-18 | fix | closed | Push initial project scaffolding so origin/main exists and pull stops failing | 3309db6 |
| GAI-T3 | 2026-10-18 | feature | closed | v1: GTFO.AbsurdItems mod - artifact buffs (from GTFO.EZ), Hand Grenade and Stim Pack (from GTFO.AbsurdWeapons), kits as loot in lockers and out in the open (Loot Mix + shared KitLoot), plus the shared Insert/Delete toggles | 73ec87c |
| GAI-T4 | 2026-10-18 | feature | closed | Shared code republished from GTFO.EZ: SharedOwner treats a switched-off GTFO.* mod as not installed, IsActive/TryGetSetting helpers | 2ff597c |
| GAI-T5 | 2026-10-18 | feature | closed | Master switch: [0. Mod] Enabled is the cfg's first entry; false and GTFO.AbsurdItems loads nothing | baf57f5 |
| GAI-T6 | 2026-10-18 | fix | closed | Hand Grenade keeps the glowstick's terminal names, so LIST/QUERY/PING show every Hand Grenade as a glow stick; give it its own (Hand Grenade, key HAND_GRENADE) | 645d804 |
| GAI-T7 | 2026-10-18 | fix | closed | Hand Grenade and Stim Pack numbers aren't held at 0 or more (a negative DrainPercentPerSecond grows the overheal without end, negative damage/heal); PickupCount/MaxCarried below 1 make empty pickups - validate them like the other settings | ba9fb30 |
| GAI-T8 | 2026-10-18 | fix | closed | Moved features lost players' settings: artifact buffs and Consumables Are Kits (GTFO.EZ) and Hand Grenade, Stim Pack, Loot Mix (GTFO.AbsurdWeapons) bind in a new cfg, so values tuned in the old mods' cfgs silently reset to defaults; import each key once from the old cfg when it isn't in AbsurdItems' yet | 88a5e61 |
| GAI-T9 | 2026-10-18 | fix | closed | Shared code from GTFO.EZ (G.EZ-T72/T75): warden message never cuts the game's own short; infinite-health check can't throw | — |
| GAI-T10 | 2026-10-18 | chore | closed | Renamed to GTFO.EZ.AbsurdItems (project, namespace, plugin GUID, network events, package name, docs) as a GTFO.EZ companion mod built on its core | 6ac3e98 |
| GAI-T11 | 2026-10-18 | fix | closed | Shared code from GTFO.EZ (G.EZ-T78/T79): WardenMessage became MessageHelper; Insert/Delete announce in the event log | — |
| GAI-T12 | 2026-10-19 | chore | closed | Align the GTFO.EZ family on one version, v13 (user request), documented in the READMEs and changelog; README review corrections | manifest + Plugin.VERSION 13.0.0 |
| GAI-T13 | 2026-10-19 | chore | wont-do | Convert tracked source files to LF line endings via .gitattributes, per HOUSE-LAW-13 (low priority) | — |
| GAI-T14 | 2026-10-19 | fix | closed | Picking up a Bold artifact during a Stim Pack overheal cut health back to max - an overheal above the old max is now left to drain (bughunt for v14; uncommitted) | ee006e5 |
| GAI-T15 | 2026-10-19 | fix | closed | Hand Grenade carry cap was applied to teammates' consumable storage on your game - only your own storage now (bughunt for v14; uncommitted) | 247c0d4 |
| GAI-T16 | 2026-10-19 | chore | closed | Bump GTFO.EZ.AbsurdItems to v14.0.0 with the GTFO.EZ family; changelog for GAI-T14/T15, and config/README wording that Hand Grenade damage counts the damage boosts (user request) | b927b53 |
| GAI-T17 | 2026-10-19 | docs | closed | Mark everything new as confirmed in play: drop the not-yet-confirmed notes from the READMEs and config text (user confirmed after in-game testing) | 4021ccc |
| GAI-T18 | 2026-10-20 | docs | closed | Publish GTFO.EZ.AbsurdItems docs to GitHub Pages via the shared Agency DocFX pipeline: run adopt-docs-site.ps1 (README.md as home page, docs/*.md as articles, API reference only where the code builds in CI), enable Pages with build_type=workflow, set the repo homepage URL, retire tools/build-readme.ps1 and README.htm, confirm the docs Actions run is green and https://mindattic.github.io/GTFO.EZ.AbsurdItems/ returns 200. Depends on MAT-T8. | .github/workflows/docs.yml |
| GAI-T19 | 2026-10-20 | chore | open | Bug hunt: run an exhaustive /bughunt over the whole repo and fix every real bug found (each fix under its own fix ticket), then hunt again; close this only when a full pass finds nothing | user request 2026-10-20: approved repo sweep gap-fill (resets the untracked v14 bughunt partial) |
| GAI-T20 | 2026-10-21 | docs | closed | Update USER_STORIES.md | — |
| GAI-T21 | 2026-10-22 | docs | closed | Update USER_STORIES.md | — |
| GAI-T22 | 2026-10-24 | docs | closed | Update USER_STORIES.md | — |
| GAI-T23 | 2026-10-24 | feature | closed | Artifact glow (category-colored light + model tint: Aggressive red, Muted blue, Bold green) and Breadcrumbs (glow stick markers at opened containers/doors and newly entered areas) | ArtifactGlow.cs, Breadcrumbs.cs |
| GAI-T24 | 2026-10-24 | docs | closed | Reread and refresh README/package README: fix every Insert/Delete wording for the swap (Insert is now health, Delete is now ammo) | README.md |
| GAI-T25 | 2026-10-24 | chore | closed | Cut the v16 release: pull in GTFO.EZ.Shared's Insert/Delete swap and always-console-log fix (submodule bump), fix GTFO.* wording, rebuild and repackage dist/ | package/manifest.json |
| GAI-T26 | 2026-10-24 | chore | closed | Cut the v17 release: bump submodule to GTFO.EZ.Shared's Order cleanup, join CrashTestDummies/SkipDrop into the vN scheme, bump VERSION/manifest, rebuild and repackage dist/ | package/manifest.json |
| GAI-T27 | 2026-10-26 | chore | closed | Cut the v18 release: moves in step with the GTFO.* family's version number, bump VERSION/manifest, rebuild and repackage dist/ | package/CHANGELOG.md |
| GAI-T28 | 2026-11-01 | chore | closed | Rename repo to Mods.GTFO.EZ.AbsurdItems for mod organization (GitHub + folder + refs) | 682bb84cd5f98af41d4ac4b723f1559fddb80226 |
| GAI-T29 | 2026-11-01 | fix | closed | Verify/bug-hunt kits-as-loot balance, grenades out in the open, artifact glow/buffs; refresh docs, cut v19 release | No real bugs found in kits-as-loot/grenades-out-in-the-open/artifact glow+buffs after exhaustive review (incl. reflecting Modules-ASM.dll to confirm ePickupItemStatus only has PickedUp/PlacedInLevel); docs already accurate; v19 cut under GAI-T30 (2cb80f7) |
| GAI-T30 | 2026-11-01 | chore | closed | Cut the v19 release: bug-hunt pass found no new issues in kits/grenades/artifacts, bump VERSION/manifest, rebuild and repackage dist/ | 2cb80f7 on main; dotnet build 0 warnings/0 errors; dist/GTFO.EZ.AbsurdItems-19.0.0.zip built |
