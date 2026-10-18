# GTFO.AbsurdMoves

![GTFO.AbsurdMoves](logo.png)

*(Listed on Thunderstore as `GtfoAbsurdMoves`. Package names there can only use letters, numbers
and underscores, so they can't contain a period.)*

A BepInEx plugin for GTFO that adds new player moves: wall running, double jump and glide, ground
pound, slide, dodge and bullet time, plus firing on the move and a faster sprint with more stamina.
No game files are shipped or edited.

**Every move has its own on/off switch, and its numbers can be retuned,** in the config file
BepInEx creates on first launch: `BepInEx\config\MindAttic.GTFO.AbsurdMoves.cfg`.

## The moves

| Move | How | Switch |
|---|---|---|
| [Wall running](#wall-running) | In the air next to a wall, press **Jump** again | `[Wall Running] Wall Running Enabled` |
| [Double jump](#double-jump-and-glide) | In the air away from a wall, press **Jump** again | `[Double Jump] Double Jump Enabled` |
| [Glide](#double-jump-and-glide) | Keep holding **Jump** after a double jump | `[Double Jump] Glide Enabled` |
| [Ground pound](#ground-pound) | In the air, high enough up, press **Crouch** | `[Ground Pound] Ground Pound Enabled` |
| [Mantling](#mantling) | Jump at a ledge no higher than your eyes, holding **forward** | `[Mantling] Mantling Enabled` |
| [Slide](#slide) | Press **Crouch** while sprinting | `[Slide] Slide Enabled` |
| [Dodge](#dodge) | Hold **Shift** and double-tap **W/A/S/D** | `[Dodge] Dodge Enabled` |
| [Bullet time](#bullet-time) | Tap **Left Alt** | `[Bullet Time] Enabled` |
| [Infinite ammo / health](#infinite-ammo-and-health) | Press **Insert** / **Delete** | Toggles, off at the start |
| [Fire mid-air](#fire-on-the-move) | Shoot while jumping or falling | `[Fire On The Move] Fire Mid-Air Enabled` |
| [Fire while running](#fire-on-the-move) | Shoot while sprinting | `[Fire On The Move] Fire While Running Enabled` |
| [Faster sprint](#run-speed-and-stamina) | Always on | `[Run Speed] Run Speed Enabled` |
| [More stamina](#run-speed-and-stamina) | Always on | `[Stamina] Stamina Enabled` |

Jump and Crouch are the game's own bindings, so they follow whatever you've rebound them to.

## Wall running

While you're in the air next to a wall, press Jump a second time to run along the wall, in
whichever direction along it you're looking. Press Jump again to kick off the wall, which also gives
you your double jump back. A wall run ends when the wall runs out, when it times out, or when you
land. A small burning sound loops while you run.

### `[Wall Running]`

| Setting | Default | What it does |
|---|---|---|
| `Wall Running Enabled` | true | Turns wall running on or off. |
| `Wall Run Speed (m/s)` | 8 | Speed along the wall. |
| `Wall Run Max Duration (seconds)` | 2.5 | Longest a single wall run lasts. |
| `Wall Reach (meters)` | 1.2 | How close a wall must be. |
| `Wall Run Sink Speed (m/s)` | 0.4 | How fast you slide down the wall while on it (0 = level). |
| `Wall Jump Up Speed (m/s)` | 6 | Upward speed kicking off a wall. |
| `Wall Jump Push Speed (m/s)` | 5 | Speed away from the wall kicking off it. |

## Double jump and glide

Press Jump a second time in the air, away from a wall, to jump again (once per jump). Keep holding
Jump afterwards and you glide down slowly instead of dropping, with the same burning loop. A glide
takes no fall damage for the height it covers.

### `[Double Jump]`

| Setting | Default | What it does |
|---|---|---|
| `Double Jump Enabled` | true | Turns the double jump on or off. |
| `Double Jump Speed (m/s)` | 6.5 | Upward speed the double jump gives. |
| `Glide Enabled` | true | Turns gliding on or off. |
| `Glide Fall Speed (m/s)` | 1.5 | How fast you sink while gliding. |

### `[Air Move Sounds]`

| Setting | Default | What it does |
|---|---|---|
| `Burn Loop Sound` | `FLAMERRESIDUALFIRELOOP` | The loop under a wall run or glide (a game sound event name). Empty = silent. |
| `Burn Loop Stop Sound` | `FLAMERRESIDUALFIRELOOPEND` | Stops it. |

## Ground pound

Press Crouch in the air and you slam straight down at twice your fall speed, taking no fall damage.
You land in a concussion wave that shoves every enemy around you back, staggers and hurts them (more
up close), and throws up a ring of debris. It only starts if you're at least `Min Height` above the
floor, so a crouch-jump never sets it off. The landing is broadcast: everyone sees the debris, and
the host shoves the enemies, so it works for the whole team. Needs the GTFO-API plugin.

### `[Ground Pound]`

| Setting | Default | What it does |
|---|---|---|
| `Ground Pound Enabled` | true | Turns the ground pound on or off. |
| `Min Height (meters)` | 2 | How high above the floor you must be to start one. |
| `Min Slam Speed (m/s)` | 12 | Slowest the slam drops; it doubles your fall speed when that's faster. |
| `Shockwave Radius (meters)` | 5 | How far the shockwave reaches. |
| `Pushback Distance (meters)` | 4 | How far an enemy right next to you is shoved (less further out). |
| `Shockwave Damage` | 10 | Damage to each enemy caught in it (less further out). 0 = push only. |
| `Shockwave Stagger` | 3 | Stagger multiplier of that hit. |
| `Debris Count` | 10 | Dust-and-scorch bursts scattered around the landing. |
| `Impact Sound` | `STINGER_LOW_BOOM` | Played on landing. Empty = silent. |

## Mantling

Jump at a ledge while holding forward. If its top is no higher than your eyes and there's room to
stand up there, you're pulled up the face and over onto it. Your gun drops out of the way for the
pull-up, like a vault. Anything below knee height is left to the game's own step-up.

### `[Mantling]`

| Setting | Default | What it does |
|---|---|---|
| `Mantling Enabled` | true | Turns mantling on or off. |
| `Mantle Duration (seconds)` | 0.35 | How long the pull-up takes. |
| `Ledge Reach (meters)` | 0.8 | How close the ledge's face must be in front of you. |
| `Eye Level Margin (meters)` | 0.1 | How far above eye level a ledge top can still be grabbed. |

## Slide

Crouch while sprinting to slide on in the direction you were running. The slide holds most of your
speed at first, then brakes into a stop, covering about 7 m at the defaults. Movement keys are
ignored while you slide and the camera stays free. You can slide past an open doorway, turn and shoot
into it, and keep sliding the same way. Standing up, jumping or hitting a wall ends it.

### `[Slide]`

| Setting | Default | What it does |
|---|---|---|
| `Slide Enabled` | true | Turns the slide on or off. |
| `Slide Start Speed (m/s)` | 9 | Speed the slide starts at. |
| `Slide Duration (seconds)` | 1.2 | How long a slide lasts; it covers about two thirds of start speed x duration. |

## Dodge

Hold Shift and double-tap W, A, S or D to dodge that way, relative to where you're looking. It's
mainly for ducking out of a homing projectile's path on short notice. Walls stop it short.

### `[Dodge]`

| Setting | Default | What it does |
|---|---|---|
| `Dodge Enabled` | true | Turns the dodge on or off. |
| `Dodge Distance (meters)` | 6 | How far a full dodge goes. |
| `Dodge Duration (seconds)` | 0.25 | How long a full dodge takes; distance / duration is its speed. |
| `Dodge Cooldown (seconds)` | 1.5 | Wait before another dodge can start. |
| `Dodge Double-Tap Window (seconds)` | 0.5 | How quickly the second tap must follow the first. |

## Bullet time

Tap Left Alt and every enemy slows to a crawl while you keep full speed, Max Payne style. The sound
drops to a hollow, muffled mix, with a deep boom going in and a heartbeat underneath. It's broadcast,
so the host slows the enemies for the whole team. Needs the GTFO-API plugin.

### `[Bullet Time]`

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | Turns bullet time on or off. |
| `Key` | LeftAlt | Starts bullet time. Presses while it's running or cooling down do nothing. |
| `DurationSeconds` | 5 | How long it lasts. |
| `CooldownSeconds` | 10 | Wait after it ends before it can start again. |
| `EnemySpeed` | 0.25 | Enemy speed during bullet time (1 = normal). |
| `SoundMuffle` | `SOUND_EFFECT_MUFFLE=100, GAME_MENU_MUFFLE=100` | Wwise mix parameters pushed for the hollow sound, restored after. |
| `EnterSound` / `ExitSound` | `STINGER_LOW_BOOM` / `COMBAT_FADING_PULSE_LEVEL_1` | Played going in and coming out. |
| `LoopSound` / `LoopStopSound` | `ENEMYHEARTBEATLOOP` / `ENEMYHEARTBEATLOOPEND` | The heartbeat under it. |

Any [Unity `KeyCode`](https://docs.unity3d.com/ScriptReference/KeyCode.html) name works for `Key`.

## Fire on the move

Vanilla GTFO ignores the trigger while you're sprinting or in the air. With these on, your guns fire
while you jump, fall, wall run, glide or sprint, and sprinting carries on while you shoot. Only your
own guns are affected, not teammates' or bots'.

Shooting mid-sprint brings your gun up out of its lowered sprint pose for as long as you hold the
trigger, and for a moment after. The shots, tracers and muzzle flash then leave a barrel pointed where
you're aiming. Teammates already see your gun tracking where you look while you sprint. In the air the
gun is never lowered in the first place.

### `[Fire On The Move]`

| Setting | Default | What it does |
|---|---|---|
| `Fire Mid-Air Enabled` | true | Fire while jumping or falling. |
| `Fire While Running Enabled` | true | Fire while sprinting. |

## Run speed and stamina

A faster top sprint, and stamina that goes further: every action's stamina cost (sprinting, jumping,
walking, crouching, strafing) is divided by the multiplier and stamina regenerates that much faster.
Both stack with GTFO.EZ's own movement and stamina settings if it's installed. These change the
game's player data as it loads, so changes take effect on restart.

### `[Run Speed]` and `[Stamina]`

| Setting | Default | What it does |
|---|---|---|
| `Run Speed Enabled` | true | Turns the faster sprint on or off. |
| `Max Run Speed Multiplier` | 1.25 | Multiplies top sprint speed. |
| `Stamina Enabled` | true | Turns the extra stamina on or off. |
| `Stamina Multiplier` | 1.5 | Divides stamina costs and multiplies stamina regen. |

## Infinite ammo and health

**Insert** toggles infinite ammo (every gun and tool stays full, no reloads) and **Delete** infinite
health (no damage to you; bots and teammates are unaffected), each announced as a warden message.
Both start off. They're the same two keys every GTFO.* mod has, from one shared copy of the code
written in GTFO.EZ; with several of the mods installed, only one answers, so a press never toggles
twice.

## Compatibility

- **GTFO.EZ** used to have the dodge (as its dash), and **GTFO.AbsurdWeapons** used to have bullet
  time. Both moved here. Running an older build of either alongside this mod gives you two of the
  same move: switch one of them off in its config.
- Wall running, double jump, glide, ground pound, mantling, slide and fire on the move were worked out from the
  game's own movement and weapon code but haven't been confirmed in play yet. If one misbehaves,
  switch it off and the rest keep working.

## Building

```powershell
.\build.ps1                         # builds into the "GTFO - Easy Mode DEV" mod-manager profile and zips package\ into dist\
.\build.ps1 -Profile "My Profile"   # another Thunderstore Mod Manager profile
```

Needs the .NET 6 SDK and a GTFO profile with BepInEx and GTFO-API installed (BepInEx's `core` and
`interop` DLLs and `plugins\GTFO-API.dll` are the build references).

## Project layout

```
src/GTFO.AbsurdMoves/
  Plugin.cs            BepInEx entry point: binds each move's config, applies Harmony patches
  Moves.cs             Per-frame driver for the local player's movement, and shared helpers
  AirMoves.cs          Wall running, double jump, glide
  GroundPound.cs       Ground pound and its shockwave (host-side enemy pushes)
  Mantle.cs            Pull-up onto ledges no higher than eye level
  Slide.cs             Crouch-while-sprinting slide
  Dodge.cs             Shift + double-tap W/A/S/D dodge
  BulletTime.cs        Left Alt: slows every enemy (animator + navmesh speed) with a muffled Wwise mix
  FireMidAir.cs        Lets your guns fire in the air
  FireWhileRunning.cs  Lets your guns fire while sprinting
  RunAndStamina.cs     Sprint speed and stamina multipliers on the player datablock
  Sounds.cs            One-shot and looping Wwise sound helpers
  Shared/              generated from GTFO.EZ's src/GTFO.EZ/Shared by its tools/publish-shared.ps1 -
                       never edit here: the Insert/Delete infinite ammo/health toggles
package/               Thunderstore manifest, icon (256x256, from logo.png), README and changelog
logo.png               Full-size logo
```
