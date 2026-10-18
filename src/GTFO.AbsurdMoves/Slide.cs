using BepInEx.Configuration;
using Player;
using UnityEngine;

namespace GTFO.AbsurdMoves;

/// <summary>
/// Crouch while sprinting to slide: you keep going the way you were running, slowing to a stop,
/// while the camera stays free to aim anywhere. Moves through the game's own character controller
/// with a wall-stopping SphereCast each step, like the dodge.
/// </summary>
internal static class Slide
{
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<float> Speed = null!;
    internal static ConfigEntry<float> Seconds = null!;

    internal static void Bind(ConfigFile config)
    {
        const string s = "Slide";
        Enabled = config.Bind(s, "Slide Enabled", true,
            "Turns the slide on or off. On: crouch while sprinting to slide on in the direction you were running, " +
            "slowing to a stop, while you aim freely. Standing up, jumping or hitting a wall ends it.");
        Speed = config.Bind(s, "Slide Start Speed (m/s)", 9f, "Speed the slide starts at (more if you were already running faster).");
        Seconds = config.Bind(s, "Slide Duration (seconds)", 0.9f, "How long a slide takes to slow to a stop.");
    }

    private const float WallProbeRadius = 0.3f;
    private const float WallSkin = 0.15f;

    private static PlayerLocomotion.PLOC_State _lastState;
    private static bool _sliding;
    private static Vector3 _direction;
    private static float _startSpeed;
    private static float _startedAt;

    internal static void Tick(PlayerAgent agent, PlayerLocomotion loc)
    {
        var state = loc.m_currentStateEnum;
        var wasRunning = _lastState == PlayerLocomotion.PLOC_State.Run;
        _lastState = state;

        if (!_sliding)
        {
            if (!Enabled.Value || !wasRunning || state != PlayerLocomotion.PLOC_State.Crouch || Seconds.Value <= 0f) return;

            var velocity = loc.HorizontalVelocity;
            velocity.y = 0f;
            _direction = velocity.sqrMagnitude > 0.25f ? velocity.normalized : Moves.FlatForward(agent);
            _startSpeed = Mathf.Max(Speed.Value, velocity.magnitude);
            _startedAt = Time.time;
            _sliding = true;
        }

        var t = (Time.time - _startedAt) / Seconds.Value;
        if (!Enabled.Value || state != PlayerLocomotion.PLOC_State.Crouch || t >= 1f)
        {
            _sliding = false;
            return;
        }

        var step = _startSpeed * (1f - t) * Time.deltaTime;
        var from = agent.Position + Vector3.up * 0.5f;
        var blocked = Physics.SphereCast(from, WallProbeRadius, _direction, out var wall, step + WallSkin, LayerManager.MASK_WORLD);
        if (blocked) step = Mathf.Max(0f, wall.distance - WallSkin);

        agent.PlayerCharacterController?.Move(_direction * step);
        if (blocked) _sliding = false;
    }
}
