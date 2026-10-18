using BepInEx.Configuration;
using HarmonyLib;
using Player;
using UnityEngine;

namespace GTFO.AbsurdMoves;

// Hold Shift and double-tap W/A/S/D for a quick dodge that way - mainly to duck out of the way of a
// homing projectile on short notice. Moves the player through the game's own character controller
// with a wall-stopping SphereCast each step, the same approach as GTFO.AbsurdWeapons' Katana dash,
// so it collides and syncs like any other movement rather than teleporting through geometry.
internal static class Dodge
{
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<float> Distance = null!;
    internal static ConfigEntry<float> Seconds = null!;
    internal static ConfigEntry<float> CooldownSeconds = null!;
    internal static ConfigEntry<float> DoubleTapWindowSeconds = null!;

    internal static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("Dodge", "Dodge Enabled", true,
            "Turns the dodge on or off. On: hold Shift and double-tap W, A, S or D for a quick dodge that way, " +
            "relative to where you look - mainly to duck out of a homing projectile's path on short notice. " +
            "Moves you through the game's own character controller, so it collides with walls and enemies like " +
            "any other movement. Off: the keys do nothing extra.");

        Distance = config.Bind("Dodge", "Dodge Distance (meters)", 6f,
            "Meters a full dodge covers (it stops sooner if it hits a wall). Default 6.");

        Seconds = config.Bind("Dodge", "Dodge Duration (seconds)", 0.25f,
            "Seconds a full dodge takes - distance / duration is the dodge speed. Default 0.25 (24 m/s at the default distance).");

        CooldownSeconds = config.Bind("Dodge", "Dodge Cooldown (seconds)", 1.5f,
            "Seconds before another dodge can trigger after one starts. Default 1.5.");

        DoubleTapWindowSeconds = config.Bind("Dodge", "Dodge Double-Tap Window (seconds)", 0.5f,
            "How quickly the two presses of the same direction key must land to count as a double-tap. Default 0.5.");
    }
}

[HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.Update))]
internal static class DodgeHotkey_Update_Patch
{
    // Key, then its direction relative to the camera: (right, forward).
    private static readonly (KeyCode Key, Vector2 Dir)[] DirectionKeys =
    {
        (KeyCode.W, new Vector2(0f, 1f)),
        (KeyCode.S, new Vector2(0f, -1f)),
        (KeyCode.A, new Vector2(-1f, 0f)),
        (KeyCode.D, new Vector2(1f, 0f)),
    };

    private static readonly float[] LastTap = { -10f, -10f, -10f, -10f };

    // The host's bots have a local damage component too, so this runs once per bot as well: only
    // the first call each frame counts, or one tap would read as a double-tap and the dodge would
    // step several times a frame.
    private static int _lastFrame = -1;

    private static bool InPlay => FocusStateManager.CurrentState == eFocusState.FPS;
    private static bool ShiftHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    [HarmonyPostfix]
    private static void Postfix()
    {
        if (Time.frameCount == _lastFrame) return;
        _lastFrame = Time.frameCount;

        DodgeRunner.Tick();

        if (!Dodge.Enabled.Value || !InPlay) return;

        for (var i = 0; i < DirectionKeys.Length; i++)
        {
            if (!Input.GetKeyDown(DirectionKeys[i].Key)) continue;

            var sincePrevious = Time.time - LastTap[i];
            LastTap[i] = Time.time;
            if (ShiftHeld && sincePrevious <= Dodge.DoubleTapWindowSeconds.Value)
            {
                LastTap[i] = -10f; // a third tap starts a fresh pair rather than chaining off this one
                DodgeRunner.TryStart(DirectionKeys[i].Dir);
            }
        }
    }
}

internal static class DodgeRunner
{
    private const float WallProbeRadius = 0.3f;
    private const float WallSkin = 0.15f;

    private static float _cooldownUntil;
    private static PlayerAgent? _player;
    private static Vector3 _direction;
    private static float _timeLeft;

    // local: x = right, y = forward, relative to where you're looking (flattened to the ground).
    internal static void TryStart(Vector2 local)
    {
        if (_player != null || Time.time < _cooldownUntil || Mantle.Active || GroundPound.Active) return;
        // A zero duration would divide distance by 0 into a NaN step and move the player to NaN.
        if (Dodge.Seconds.Value <= 0f || Dodge.Distance.Value <= 0f) return;

        try
        {
            if (!PlayerManager.TryGetLocalPlayerAgent(out var agent) || agent == null || !agent.Alive) return;

            var forward = agent.FPSCamera != null ? agent.FPSCamera.FlatForward : agent.transform.forward;
            var right = agent.FPSCamera != null ? agent.FPSCamera.FlatRight : agent.transform.right;

            var direction = forward * local.y + right * local.x;
            if (direction.sqrMagnitude < 0.0001f) return;

            _player = agent;
            _direction = direction.normalized;
            _timeLeft = Dodge.Seconds.Value;
            _cooldownUntil = Time.time + Dodge.CooldownSeconds.Value;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Dodge: couldn't start. {e}");
            _player = null;
        }
    }

    internal static void Tick()
    {
        if (_player == null) return;

        try
        {
            if (!_player.Alive)
            {
                _player = null;
                return;
            }

            var dt = Mathf.Min(Time.deltaTime, _timeLeft);
            _timeLeft -= dt;
            var from = _player.Position;
            var step = Dodge.Distance.Value / Dodge.Seconds.Value * dt;

            // A wall or prop ahead within this step stops the dodge short - looked ahead rather than
            // measured after moving, since the agent's position only catches up later.
            var blocked = Physics.SphereCast(from + Vector3.up, WallProbeRadius, _direction, out var wall, step + WallSkin, LayerManager.MASK_WORLD);
            if (blocked) step = Mathf.Max(0f, wall.distance - WallSkin);

            _player.PlayerCharacterController?.Move(_direction * step);

            if (blocked || _timeLeft <= 0f) _player = null;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Dodge: step failed. {e}");
            _player = null;
        }
    }
}
