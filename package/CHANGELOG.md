# Changelog

## v1

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
- **Bullet Time**: tap Left Alt to slow every enemy to a crawl for 5 s, with a muffled sound mix
  (moved here from GTFO.EZ.AbsurdWeapons). It starts as the key is let go, so Alt+Tab never sets it off.
