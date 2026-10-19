using BepInEx.Configuration;
using HarmonyLib;
using Player;
using UnityEngine;

namespace GTFO.EZ.AbsurdMoves;

/// <summary>
/// Crouch while sprinting to slide: you carry on the way you were running, holding most of your
/// speed before easing to a stop, while the camera and gun stay free - slide past a doorway, turn
/// and shoot into it, and you keep sliding the same way. Movement keys are ignored for the slide
/// so looking or strafing never steers it. Moves through the game's own character controller with
/// a wall-stopping SphereCast each step, like the dodge.
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
            "Turns the slide on or off. On: crouch while sprinting to slide on in the direction you were running - " +
            "holding your speed, then easing to a stop - while you look and shoot anywhere. Standing up, jumping or " +
            "hitting a wall ends it.");
        Speed = config.Bind(s, "Slide Start Speed (m/s)", 9f, "Speed the slide starts at (more if you were already faster).");
        Seconds = config.Bind(s, "Slide Duration (seconds)", 1.2f,
            "How long a slide lasts. Speed holds near the start speed early on and drops off toward the end; " +
            "a slide covers about two thirds of start speed x duration (7 m at the defaults).");
    }

    private const float WallProbeRadius = 0.3f;
    private const float WallSkin = 0.15f;

    private static PlayerLocomotion.PLOC_State _lastState;
    private static bool _sliding;
    private static Vector3 _direction;
    private static float _startSpeed;
    private static float _startedAt;

    internal static bool Active => _sliding;

    internal static void Reset()
    {
        _sliding = false;
        _lastState = PlayerLocomotion.PLOC_State.Stand;
    }

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
            // Never slower than you were already running (a boosted sprint keeps its edge).
            _startSpeed = Mathf.Max(Speed.Value, velocity.magnitude);
            // The game gave the crouch its own little push as it left the sprint; the slide replaces
            // it rather than stacking on top (which would move you twice and overshoot).
            loc.ResetExternalPushForce();
            loc.m_horizontalVelocity = Vector3.zero;
            _startedAt = Time.time;
            _sliding = true;
        }

        var t = (Time.time - _startedAt) / Seconds.Value;
        if (!Enabled.Value || state != PlayerLocomotion.PLOC_State.Crouch || t >= 1f)
        {
            _sliding = false;
            return;
        }

        // 1 - t^2: barely slows at first, then brakes into the stop.
        var step = _startSpeed * (1f - t * t) * Time.deltaTime;
        var from = agent.Position + Vector3.up * 0.5f;
        var blocked = Physics.SphereCast(from, WallProbeRadius, _direction, out var wall, step + WallSkin, LayerManager.MASK_WORLD);
        if (blocked) step = Mathf.Max(0f, wall.distance - WallSkin);

        agent.PlayerCharacterController?.Move(_direction * step);
        if (blocked) _sliding = false;
    }

    // While sliding, the game's own crouch movement sees no movement keys, so it adds nothing on
    // top of the slide and turning to aim (or strafing) can't steer it.
    [HarmonyPatch(typeof(PlayerLocomotion), nameof(PlayerLocomotion.GetInputMoveVector))]
    private static class IgnoreMoveKeys
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerLocomotion __instance, ref Vector3 __result)
        {
            // Bots on the host are locally owned too; only the player's own input is dropped.
            if (!_sliding || !__instance.LocallyOwned) return;
            var owner = __instance.m_owner;
            if (owner != null && owner.Owner != null && !owner.Owner.IsBot) __result = Vector3.zero;
        }
    }
}
