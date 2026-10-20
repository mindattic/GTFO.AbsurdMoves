# Changelog

## v14

- **Removed: wall running.** GTFO's levels have too few long, flat walls for it to be useful. Its
  `[Wall Running]` and `[Air Move Sounds]` settings stay in an existing `.cfg` as leftover lines that
  do nothing; the double jump works anywhere in the air now, wall or no wall.
- **Double jump** goes higher: its upward speed is scaled by the new `[Double Jump] Second Jump
  Height Multiplier` (default 1.5), so the second jump clears well above a ground jump.
- **Bug fix:** the high jump never launched - the game's own jump took the charged press first, so
  you got a normal-height jump. The charge now carries into that jump.
- **Slide** goes twice as far: *Slide Duration* 1.2 s -> 2.4 s, about 14 m at the default speed. An
  install still on the old 1.2 is moved to 2.4 once; a value you set yourself is kept.
- Moves in step with the GTFO.EZ family's version number (GTFO.EZ, GTFO.EZ.AbsurdWeapons,
  GTFO.EZ.AbsurdMoves and GTFO.EZ.AbsurdItems share one).

## v13

- **Versioning:** the GTFO.EZ family - GTFO.EZ, GTFO.EZ.AbsurdWeapons, GTFO.EZ.AbsurdMoves and
  GTFO.EZ.AbsurdItems - now shares one version number, so this first release is **v13**, in step
  with GTFO.EZ, rather than v1.
- Renamed from GTFO.AbsurdMoves to **GTFO.EZ.AbsurdMoves** before release, as a GTFO.EZ companion mod built
  on GTFO.EZ's core (plugin GUID `MindAttic.GTFO.EZ.AbsurdMoves`, config file
  `MindAttic.GTFO.EZ.AbsurdMoves.cfg`, Thunderstore package `GtfoEZAbsurdMoves`).
- **Master switch**: `[0. Mod] Enabled` (default `true`) is the config file's first entry; set it to
  `false` and the whole mod is off - nothing is patched, no move or hotkey works.
- **Insert** (infinite ammo) and **Delete** (infinite health) toggles, announced as warden
  messages - the shared keys every GTFO.* mod has, so they never clash with several installed.
- Initial release. Every move has its own on/off switch in the config.
- **Wall running**: in the air next to a wall, press Jump again to run along it; Jump again kicks off.
- **Double jump**: press Jump again in the air for a whole new jump from where you are - the
  game's own jump, no glide or float.
  A small burning loop plays while wall running.
- **High jump**: stay crouched on the ground until you flash, then press Jump to launch about 10 m
  straight up, with no fall damage on the landing.
- **Ground pound**: press Crouch high enough in the air to slam down at double fall speed with no fall
  damage, landing in a shockwave that shoves and staggers enemies, with debris.
- **Mantling**: jump at a ledge no higher than your eyes while holding forward to pull up onto it.
- **Slide**: crouch while sprinting to slide on that way while aiming freely.
- **Fire on the move**: guns fire mid-air and while sprinting.
- **Faster sprint** (x1.25) and **more stamina** (x1.5), each a multiplier with its own switch.
- **Dodge**: hold Shift and double-tap W/A/S/D to dodge that way (moved here from GTFO.EZ's dash).
- **Bullet Time**: tap Left Alt to slow the whole game - everyone and everything - for 5 s, easing in and
  back out, Max Payne style (the event log says when it's ready again), with a muffled sound mix
  (moved here from GTFO.EZ.AbsurdWeapons). It starts as the key is let go, so Alt+Tab never sets it off.
