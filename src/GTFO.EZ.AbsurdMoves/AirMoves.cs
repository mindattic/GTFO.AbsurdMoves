using BepInEx.Configuration;
using HarmonyLib;
using Player;
using UnityEngine;

namespace GTFO.EZ.AbsurdMoves;

/// <summary>
/// The double jump: a second press of Jump in the air is a classic double jump - the game's own jump
/// started again from your current height, so the game moves the player (and syncs it) exactly as it
/// does a normal jump, its upward speed scaled by Second Jump Height Multiplier. Once per jump.
/// (Wall running used to share this press; it was removed - the levels have too few long flat walls
/// for it to be useful.)
/// </summary>
internal static class AirMoves
{
    internal static ConfigEntry<bool> DoubleJumpEnabled = null!;
    internal static ConfigEntry<float> SecondJumpHeightMultiplier = null!;

    internal static void Bind(ConfigFile config)
    {
        const string jump = "Double Jump";
        DoubleJumpEnabled = config.Bind(jump, "Double Jump Enabled", true,
            "Turns the double jump on or off. On: press Jump a second time in the air to jump again - a whole " +
            "new jump, the game's own, from wherever you are. Once per jump.");
        SecondJumpHeightMultiplier = config.Bind(jump, "Second Jump Height Multiplier", 1.5f,
            "Scales the second jump's upward speed against a jump off the ground. 1 = the same as a ground jump, " +
            "1.5 = half again as fast up (well over twice as high, since height grows with the square of the speed).");
    }

    // A press this soon after leaving the ground is the press that jumped, not a second one.
    private const float SecondPressAfter = 0.1f;

    private static float _airborneSince = -1f;
    private static bool _doubleJumpUsed;

    internal static void Reset()
    {
        _airborneSince = -1f;
        _doubleJumpUsed = false;
    }

    internal static void Tick(PlayerAgent agent, PlayerLocomotion loc)
    {
        if (!Moves.InAir(loc))
        {
            _airborneSince = -1f;
            _doubleJumpUsed = false;
            return;
        }
        if (_airborneSince < 0f) _airborneSince = Time.time;
        if (!MovesToggle.Active || GroundPound.Active || !DoubleJumpEnabled.Value || _doubleJumpUsed) return;

        var jumpDown = Moves.InPlay && InputMapper.GetButtonDown.Invoke(InputAction.Jump, eFocusState.FPS);
        if (!jumpDown || Time.time - _airborneSince <= SecondPressAfter) return;

        // A whole new jump from where you are: the game's own jump state re-entered (so it moves and
        // syncs as a ground jump does), then its upward speed scaled so the second jump goes higher.
        _doubleJumpUsed = true;
        Moves.ResetFallStart(agent, loc);
        loc.ChangeState(PlayerLocomotion.PLOC_State.Jump, false);
        loc.m_verticalVelocity *= Mathf.Max(0.1f, SecondJumpHeightMultiplier.Value);
    }
}
