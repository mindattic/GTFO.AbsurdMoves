# GTFO.AbsurdMoves

*(Listed on Thunderstore as `GtfoAbsurdMoves`. Package names there can only use letters, numbers
and underscores, so they can't contain a period.)*

A BepInEx plugin for GTFO that adds new player moves. No game files are shipped or edited. Every
move can be switched off and retuned in the config file BepInEx creates on first launch,
`BepInEx\config\MindAttic.GTFO.AbsurdMoves.cfg`.

## Dodge

Hold **Shift** and double-tap **W**, **A**, **S** or **D** to dodge that way, relative to where
you're looking. It's mainly for ducking out of a homing projectile's path on short notice. You
move through the game's own character controller, so walls stop the dodge short and it syncs like
any other movement.

### `[Dodge]`

| Setting | Default | What it does |
|---|---|---|
| `Dodge Enabled` | true | Turns the dodge on or off. |
| `Dodge Distance (meters)` | 6 | How far a full dodge goes (it stops sooner at a wall). |
| `Dodge Duration (seconds)` | 0.25 | How long a full dodge takes; distance / duration is its speed. |
| `Dodge Cooldown (seconds)` | 1.5 | Wait before another dodge can start. |
| `Dodge Double-Tap Window (seconds)` | 0.5 | How quickly the second tap must follow the first. |

## Building

```powershell
.\build.ps1                         # builds into the "GTFO - Easy Mode DEV" mod-manager profile and zips package\ into dist\
.\build.ps1 -Profile "My Profile"   # another Thunderstore Mod Manager profile
```

Needs the .NET 6 SDK and a GTFO profile with BepInEx installed (its `core` and `interop` DLLs are
the build references).

## Project layout

```
src/GTFO.AbsurdMoves/
  Plugin.cs   BepInEx entry point: binds each move's config, applies Harmony patches
  Dodge.cs    Shift + double-tap W/A/S/D dodge
package/      Thunderstore manifest, README and changelog
```
