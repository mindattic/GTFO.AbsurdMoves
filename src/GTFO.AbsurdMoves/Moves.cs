using HarmonyLib;
using Player;
using UnityEngine;

namespace GTFO.AbsurdMoves;

// Shared per-frame driver and helpers for the movement features: runs once per frame for the
// local player's locomotion only (never a teammate's or a bot's).
[HarmonyPatch(typeof(PlayerLocomotion), nameof(PlayerLocomotion.Update))]
internal static class Moves
{
    internal static bool InPlay => FocusStateManager.CurrentState == eFocusState.FPS;

    internal static bool InAir(PlayerLocomotion loc) =>
        loc.m_currentStateEnum is PlayerLocomotion.PLOC_State.Jump or PlayerLocomotion.PLOC_State.Fall;

    internal static Vector3 FlatForward(PlayerAgent agent)
    {
        var forward = agent.FPSCamera != null ? agent.FPSCamera.FlatForward : agent.transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    // Fall damage is measured from where the fall started, so a glide or wall run moves that start
    // along with you: you only take damage for what you drop after it ends.
    internal static void ResetFallStart(PlayerAgent agent, PlayerLocomotion loc)
    {
        var fall = loc.CurrentState?.TryCast<PLOC_Fall>();
        if (fall != null) fall.m_startPos = agent.Position;
    }

    // The fall state caps air speed at the speed you left the ground with; raise it to fit ours.
    internal static void AllowAirSpeed(PlayerLocomotion loc, float speed)
    {
        var fall = loc.CurrentState?.TryCast<PLOC_Fall>();
        if (fall != null && fall.m_maxHorizontalVelocity < speed) fall.m_maxHorizontalVelocity = speed;
    }

    // The held gun's own jump bob, as a normal jump plays it.
    internal static void JumpSway(PlayerAgent agent) => agent.FPItemHolder?.PlayJumpAnimation();

    [HarmonyPostfix]
    private static void Postfix(PlayerLocomotion __instance)
    {
        try
        {
            if (!__instance.LocallyOwned) return;
            var agent = __instance.m_owner;
            if (agent == null || !agent.Alive || agent.Owner == null || agent.Owner.IsBot) return;
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            GroundPound.Tick(agent, __instance);
            AirMoves.Tick(agent, __instance);
            Slide.Tick(agent, __instance);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.AbsurdMoves movement update failed: {e}");
        }
    }
}
