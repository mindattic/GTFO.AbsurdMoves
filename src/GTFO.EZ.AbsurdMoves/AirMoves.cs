using BepInEx.Configuration;
using HarmonyLib;
using Player;
using UnityEngine;

namespace GTFO.EZ.AbsurdMoves;

/// <summary>
/// Mid-air moves on a second press of Jump: next to a wall it starts a wall run along it, anywhere
/// else it's a classic double jump - one more jump, your sideways momentum kept, normal gravity after.
/// Both work by writing the game's own locomotion velocities while it's in its Jump/Fall state, so the
/// game moves the player (and syncs it) exactly as it does a normal jump. A small fire loop plays while
/// wall running.
/// </summary>
internal static class AirMoves
{
    internal static ConfigEntry<bool> WallRunEnabled = null!;
    internal static ConfigEntry<float> WallRunSpeed = null!;
    internal static ConfigEntry<float> WallRunMaxSeconds = null!;
    internal static ConfigEntry<float> WallRunReach = null!;
    internal static ConfigEntry<float> WallRunSinkSpeed = null!;
    internal static ConfigEntry<float> WallJumpUpSpeed = null!;
    internal static ConfigEntry<float> WallJumpPushSpeed = null!;

    internal static ConfigEntry<bool> DoubleJumpEnabled = null!;
    internal static ConfigEntry<float> DoubleJumpSpeed = null!;

    internal static ConfigEntry<string> BurnLoopSound = null!;
    internal static ConfigEntry<string> BurnLoopStopSound = null!;

    internal static void Bind(ConfigFile config)
    {
        const string wall = "Wall Running";
        WallRunEnabled = config.Bind(wall, "Wall Running Enabled", true,
            "Turns wall running on or off. On: while in the air next to a wall, press Jump a second time to run along " +
            "the wall in the direction you're looking. Press Jump again to kick off it.");
        WallRunSpeed = config.Bind(wall, "Wall Run Speed (m/s)", 8f, "How fast you run along the wall.");
        WallRunMaxSeconds = config.Bind(wall, "Wall Run Max Duration (seconds)", 2.5f, "Longest a single wall run lasts before you drop off.");
        WallRunReach = config.Bind(wall, "Wall Reach (meters)", 1.2f, "How close a wall has to be to start (and keep) a wall run.");
        WallRunSinkSpeed = config.Bind(wall, "Wall Run Sink Speed (m/s)", 0.4f, "How fast you slide down the wall while running on it. 0 = level.");
        WallJumpUpSpeed = config.Bind(wall, "Wall Jump Up Speed (m/s)", 6f, "Upward speed when you jump off a wall.");
        WallJumpPushSpeed = config.Bind(wall, "Wall Jump Push Speed (m/s)", 5f, "Speed away from the wall when you jump off it.");

        const string jump = "Double Jump";
        DoubleJumpEnabled = config.Bind(jump, "Double Jump Enabled", true,
            "Turns the double jump on or off. On: press Jump a second time in the air (away from a wall) to jump " +
            "again. Once per jump; kicking off a wall gives it back.");
        DoubleJumpSpeed = config.Bind(jump, "Double Jump Speed (m/s)", 6.5f, "Upward speed the double jump gives.");

        const string sound = "Air Move Sounds";
        BurnLoopSound = config.Bind(sound, "Burn Loop Sound", "FLAMERRESIDUALFIRELOOP",
            "A small burning loop while wall running (a game sound event name). Empty = silent.");
        BurnLoopStopSound = config.Bind(sound, "Burn Loop Stop Sound", "FLAMERRESIDUALFIRELOOPEND", "Stops the burn loop.");
    }

    // A press this soon after leaving the ground is the press that jumped, not a second one.
    private const float SecondPressAfter = 0.1f;
    private const float WallRegrabDelay = 0.35f;
    private const float WallStick = 0.5f;

    private static float _airborneSince = -1f;
    private static bool _doubleJumpUsed;
    private static bool _wallRunning;
    private static float _wallRunUntil;
    private static float _wallRegrabAt;
    private static Vector3 _wallNormal;
    private static Vector3 _wallTangent;

    internal static void Reset()
    {
        _airborneSince = -1f;
        _doubleJumpUsed = false;
        _wallRunning = false;
        _wallRegrabAt = 0f;
    }

    internal static void Tick(PlayerAgent agent, PlayerLocomotion loc)
    {
        if (!Moves.InAir(loc))
        {
            _airborneSince = -1f;
            _doubleJumpUsed = false;
            _wallRunning = false;
            return;
        }
        if (_airborneSince < 0f) _airborneSince = Time.time;
        if (GroundPound.Active)
        {
            _wallRunning = false;
            return;
        }

        var jumpDown = Moves.InPlay && InputMapper.GetButtonDown.Invoke(InputAction.Jump, eFocusState.FPS);
        var secondPress = jumpDown && Time.time - _airborneSince > SecondPressAfter;

        if (_wallRunning)
        {
            if (secondPress)
            {
                WallJump(agent, loc);
                return;
            }
            if (!WallRunEnabled.Value || Time.time > _wallRunUntil || !FindWall(agent, out var normal))
            {
                _wallRunning = false;
                _wallRegrabAt = Time.time + WallRegrabDelay;
            }
            else
            {
                _wallNormal = normal;
                WallRun(agent, loc);
                return;
            }
        }

        if (secondPress)
        {
            if (WallRunEnabled.Value && Time.time >= _wallRegrabAt && FindWall(agent, out var normal))
            {
                _wallNormal = normal;
                _wallTangent = Vector3.zero;
                _wallRunning = true;
                _wallRunUntil = Time.time + WallRunMaxSeconds.Value;
                WallRun(agent, loc);
                return;
            }
            if (DoubleJumpEnabled.Value && !_doubleJumpUsed)
            {
                _doubleJumpUsed = true;
                loc.m_verticalVelocity = Vector3.up * DoubleJumpSpeed.Value;
                Moves.ResetFallStart(agent, loc);
                Moves.JumpSway(agent);
            }
        }
    }

    private static void WallRun(PlayerAgent agent, PlayerLocomotion loc)
    {
        var look = Moves.FlatForward(agent);
        var tangent = Vector3.ProjectOnPlane(look, _wallNormal);
        tangent.y = 0f;
        // Looking straight at the wall leaves no direction along it: keep going the way we were.
        if (tangent.sqrMagnitude > 0.01f) _wallTangent = tangent.normalized;
        else if (_wallTangent == Vector3.zero) _wallTangent = Vector3.Cross(_wallNormal, Vector3.up).normalized;

        var speed = WallRunSpeed.Value;
        loc.m_horizontalVelocity = _wallTangent * speed - _wallNormal * WallStick;
        loc.m_verticalVelocity = Vector3.down * Mathf.Max(0f, WallRunSinkSpeed.Value);
        Moves.AllowAirSpeed(loc, speed);
        Moves.ResetFallStart(agent, loc);
        Burn(agent);
    }

    private static void WallJump(PlayerAgent agent, PlayerLocomotion loc)
    {
        _wallRunning = false;
        _wallRegrabAt = Time.time + WallRegrabDelay;
        _doubleJumpUsed = false;

        var push = _wallNormal * WallJumpPushSpeed.Value + _wallTangent * WallRunSpeed.Value * 0.5f;
        loc.m_horizontalVelocity = push;
        loc.m_verticalVelocity = Vector3.up * WallJumpUpSpeed.Value;
        Moves.AllowAirSpeed(loc, push.magnitude);
        Moves.ResetFallStart(agent, loc);
        Moves.JumpSway(agent);
    }

    private static void Burn(PlayerAgent agent) =>
        LoopSounds.Touch("air-burn", BurnLoopSound.Value, BurnLoopStopSound.Value, agent.Position);

    // The closest wall beside or ahead of you, within reach. Floors, ceilings and slopes don't count.
    private static bool FindWall(PlayerAgent agent, out Vector3 normal)
    {
        normal = Vector3.zero;
        var forward = Moves.FlatForward(agent);
        var right = Vector3.Cross(Vector3.up, forward);
        var origin = agent.Position + Vector3.up;
        var best = float.MaxValue;
        foreach (var dir in new[] { right, -right, (forward + right).normalized, (forward - right).normalized, forward })
        {
            if (!Physics.Raycast(origin, dir, out var hit, WallRunReach.Value, LayerManager.MASK_WORLD)) continue;
            if (Mathf.Abs(hit.normal.y) > 0.35f || hit.distance >= best) continue;
            best = hit.distance;
            normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
        }
        return best < float.MaxValue;
    }
}
