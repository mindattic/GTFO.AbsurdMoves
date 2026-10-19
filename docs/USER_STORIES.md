---
codex: 1
project: GTFO.EZ.AbsurdMoves
code: GAM
layer: stories
status: living
updated: 2026-10-18
---
<!-- GENERATED from MindAttic.Workflow/data/workflow.db by `workflow db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the workflow CLI. -->

# GTFO.EZ.AbsurdMoves — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Priority backlog
_Nothing recorded yet._

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| GAM-T1 | 2026-10-18 | docs | closed | Start the permanent ticket log in the workflow database | — |
| GAM-T2 | 2026-10-18 | feature | closed | Dodge (Shift + double-tap W/A/S/D) moved here from GTFO.EZ, with a Dodge Enabled on/off toggle in the .cfg | Release build clean (0 warnings) |
| GAM-T3 | 2026-10-18 | feature | closed | Bullet Time (tap Left Alt: enemies slow to a crawl, muffled sound) moved here from GTFO.AbsurdWeapons, keeping its Enabled toggle in the .cfg | Release build clean (0 warnings) |
| GAM-T4 | 2026-10-18 | feature | closed | Wall running and double jump: second Space mid-air near a wall runs along it, otherwise double-jumps (hold Space to glide); continuous burn loop sound; WallRunning and DoubleJump toggles in the .cfg | Release build clean (0 warnings); not yet confirmed in play |
| GAM-T5 | 2026-10-18 | feature | closed | Fire while jumping or mid-air, with an on/off toggle in the .cfg | Release build clean (0 warnings); not yet confirmed in play |
| GAM-T6 | 2026-10-18 | feature | closed | Fire while running (sprinting), with an on/off toggle in the .cfg | Release build clean (0 warnings); not yet confirmed in play |
| GAM-T7 | 2026-10-18 | feature | closed | Slide: crouch while sprinting to slide on in that direction while aiming freely, with an on/off toggle in the .cfg | Release build clean (0 warnings); not yet confirmed in play |
| GAM-T8 | 2026-10-18 | feature | closed | Run speed and stamina: on/off toggles plus Max Run Speed and Stamina multipliers in the .cfg | Release build clean (0 warnings); not yet confirmed in play |
| GAM-T9 | 2026-10-18 | feature | closed | Ground Pound: crouch mid-air (high enough above the ground) to slam down at double fall speed with no fall damage, landing in a shockwave that pushes enemies back with debris; on/off toggle in the .cfg | Release build clean (0 warnings); not yet confirmed in play |
| GAM-T10 | 2026-10-18 | docs | closed | Logo wired in as the package icon (also shown for local installs), full manifest description, README covering every move and its .cfg toggle | Release build clean (0 warnings); not yet confirmed in play |
| GAM-T11 | 2026-10-18 | fix | closed | Sprint-firing kept the first-person gun in its lowered sprint pose, so shots and tracers left a barrel pointed at the floor; it now comes up level while firing. Double jumps and wall jumps also play the gun's own jump sway | Release build clean; not yet confirmed in play |
| GAM-T12 | 2026-10-18 | fix | closed | Slide bled speed too fast and movement keys steered it, so turning to shoot pulled you off line; it now holds speed then brakes (about 7 m) and ignores movement keys | Release build clean; not yet confirmed in play |
| GAM-T13 | 2026-10-18 | feature | closed | Mantling: jumping at a ledge no higher than eye level while holding forward pulls you up onto it (room to stand checked, gun lowered for the pull-up); Mantling Enabled toggle | Release build clean; not yet confirmed in play |
| GAM-T14 | 2026-10-18 | fix | closed | Review fixes: moves reset on death/level change (stuck lowered gun, stale mantle/pound into next level); dodge once per frame with host bots; slide input zeroing skips bots and replaces the game's crouch-push; pound/mantle/dodge exclude each other; pound only lands on your feet, times out, restores game fall immunity, radius 0 safe; glide always sinks; teammate bullet-time trigger never refused; mantle uses live transform | Release build clean (0 warnings); not yet confirmed in play |
| GAM-T15 | 2026-10-18 | feature | closed | Shared Insert/Delete infinite ammo/health toggles imported from GTFO.EZ (Shared/, published by GTFO.EZ tools/publish-shared.ps1) - user request 2026-10-18 | d051093 |
| GAM-T16 | 2026-10-18 | fix | closed | Ground pound started inside the post-landing grace window recorded the mod's own fall-damage immunity as the game's, leaving fall damage off for good | — |
| GAM-T17 | 2026-10-18 | fix | closed | Slide start speed ignored how fast you were already going, though the README promises more if you were already faster | — |
| GAM-T18 | 2026-10-18 | fix | closed | Fire mid-air left the locomotion state faked as Stand if the gun's Update threw, since the restore was a postfix | — |
| GAM-T19 | 2026-10-18 | fix | closed | Dodge counted a first tap made without Shift held, so tap then Shift+tap dodged | — |
| GAM-T20 | 2026-10-18 | fix | closed | Ground pound config unvalidated: negative Pushback Distance pulled enemies in, huge Debris Count spawned unbounded effects | — |
| GAM-T21 | 2026-10-18 | fix | closed | Shared code from GTFO.EZ (G.EZ-T67/T68): one warden-message flag across mods that can't get stuck, owner skips mods without shared code | — |
| GAM-T22 | 2026-10-18 | chore | closed | Shared code republished from GTFO.EZ: SharedOwner.Order now lists GTFO.AbsurdItems | e92c94e |
| GAM-T23 | 2026-10-18 | feature | closed | Master switch: [0. Mod] Enabled as the cfg's first entry; off means the mod loads nothing (user request) | — |
| GAM-T24 | 2026-10-18 | feature | closed | Shared code republished from GTFO.EZ: SharedOwner treats a switched-off GTFO.* mod as not installed, IsActive/TryGetSetting helpers | 6c7fcb0 |
| GAM-T25 | 2026-10-18 | fix | closed | Fire while running puts the running flag back in a finalizer, so a throwing gun Update can't leave the sprint reading as not running | 675ead4 |
| GAM-T26 | 2026-10-18 | fix | closed | Ground pound and bullet time network handlers catch their own errors, so a failure never escapes into GTFO-API's packet hook on SNet_Replication.RecieveBytes | 6686df5 |
| GAM-T27 | 2026-10-18 | fix | closed | Bullet time starts when its key is released on its own, so Alt+Tab (and other Alt chords) no longer sets it off for the whole team | 5e5c5a9 |
| GAM-T28 | 2026-10-18 | fix | closed | Shared code from GTFO.EZ (G.EZ-T72/T75): warden message never cuts the game's own short; infinite-health check can't throw | — |
| GAM-T29 | 2026-10-18 | chore | closed | Renamed to GTFO.EZ.AbsurdMoves (project, namespace, plugin GUID, network events, package name, docs) as a GTFO.EZ companion mod built on its core | 2bc02fc |
| GAM-T30 | 2026-10-18 | fix | closed | Double jump floated: holding Jump after it glided you down. Now a classic double jump - glide and its settings removed | — |
| GAM-T31 | 2026-10-18 | fix | closed | Shared code from GTFO.EZ (G.EZ-T78/T79): WardenMessage became MessageHelper; Insert/Delete announce in the event log | — |
