using BepInEx.Configuration;
using Player;
using UnityEngine;

namespace GTFO.AbsurdMoves;

/// <summary>
/// Mantling: in the air, pushing forward into a ledge whose top is no higher than your eyes, you're
/// pulled up and over onto it - as long as there's room to stand up there. The pull-up goes through
/// the game's own character controller (up the face first, then forward onto the top), so it
/// collides and syncs like any other movement, and the gun is lowered for it like a vault.
/// </summary>
internal static class Mantle
{
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<float> Seconds = null!;
    internal static ConfigEntry<float> Reach = null!;
    internal static ConfigEntry<float> EyeMargin = null!;

    internal static void Bind(ConfigFile config)
    {
        const string s = "Mantling";
        Enabled = config.Bind(s, "Mantling Enabled", true,
            "Turns mantling on or off. On: jump at a ledge while holding forward and, if its top is no higher than " +
            "your eyes and there's room to stand on it, you're pulled up on top.");
        Seconds = config.Bind(s, "Mantle Duration (seconds)", 0.35f, "How long the pull-up takes.");
        Reach = config.Bind(s, "Ledge Reach (meters)", 0.8f, "How close the ledge's face must be in front of you.");
        EyeMargin = config.Bind(s, "Eye Level Margin (meters)", 0.1f,
            "How far above eye level a ledge top can still be grabbed. 0 = strictly at or below your eyes.");
    }

    private const float MinLedgeHeight = 0.6f;   // anything lower is a step the game handles itself
    private const float OntoTop = 0.6f;          // how far past the edge you end up
    private const float StandRadius = 0.35f;
    private const float StandHeight = 1.8f;
    private const float FallbackEyeHeight = 1.6f;

    private static bool _mantling;
    private static float _startedAt;
    private static Vector3 _from, _edge, _to;
    private static bool _gunLowered;

    internal static bool Active => _mantling;

    internal static void Tick(PlayerAgent agent, PlayerLocomotion loc)
    {
        if (_mantling)
        {
            Step(agent, loc);
            return;
        }

        if (!Enabled.Value || !Moves.InPlay || !Moves.InAir(loc)) return;
        if (InputMapper.GetAxis.Invoke(InputAction.MoveVertical, eFocusState.FPS) < 0.5f) return;
        if (!FindLedge(agent, out var top, out var forward)) return;

        _mantling = true;
        _startedAt = Time.time;
        _from = agent.Position;
        _edge = new Vector3(_from.x, top.y + 0.05f, _from.z);
        _to = top + forward * 0.05f + Vector3.up * 0.05f;
        LowerGun(agent, true);
        Step(agent, loc);
    }

    // Up the face for the first 60% of the time, then forward onto the top.
    private static void Step(PlayerAgent agent, PlayerLocomotion loc)
    {
        try
        {
            if (!agent.Alive || Seconds.Value <= 0f)
            {
                Finish(agent);
                return;
            }

            var t = Mathf.Clamp01((Time.time - _startedAt) / Seconds.Value);
            var target = t < 0.6f
                ? Vector3.Lerp(_from, _edge, Mathf.SmoothStep(0f, 1f, t / 0.6f))
                : Vector3.Lerp(_edge, _to, Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f));

            agent.PlayerCharacterController?.Move(target - agent.Position);
            loc.m_verticalVelocity = Vector3.zero;
            loc.m_horizontalVelocity = Vector3.zero;
            Moves.ResetFallStart(agent, loc);

            if (t >= 1f) Finish(agent);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"GTFO.AbsurdMoves mantle failed: {e.Message}");
            Finish(agent);
        }
    }

    private static void Finish(PlayerAgent agent)
    {
        _mantling = false;
        LowerGun(agent, false);
    }

    // The game's own "item down" counter: the gun drops out of the way while it's above zero.
    private static void LowerGun(PlayerAgent agent, bool down)
    {
        var holder = agent.FPItemHolder;
        if (holder == null || down == _gunLowered) return;
        _gunLowered = down;
        holder.m_itemDownCounter = Mathf.Max(0, holder.m_itemDownCounter + (down ? 1 : -1));
    }

    // A wall right in front, with a flat top between knee height and eye level (plus margin), room
    // to stand on that top, and nothing overhead in the way of climbing to it.
    private static bool FindLedge(PlayerAgent agent, out Vector3 top, out Vector3 forward)
    {
        top = Vector3.zero;
        forward = Moves.FlatForward(agent);
        var feet = agent.Position;
        var eye = agent.EyePosition.y - feet.y;
        if (eye < 0.5f || eye > 3f) eye = FallbackEyeHeight;

        if (!Physics.Raycast(feet + Vector3.up * MinLedgeHeight, forward, out var face, Reach.Value, LayerManager.MASK_WORLD)) return false;
        if (Mathf.Abs(face.normal.y) > 0.35f) return false;

        var highest = eye + EyeMargin.Value;
        var probe = feet + forward * (face.distance + OntoTop * 0.5f) + Vector3.up * (highest + 0.05f);
        if (!Physics.Raycast(probe, Vector3.down, out var surface, highest + 0.05f - MinLedgeHeight + 0.05f, LayerManager.MASK_WORLD)) return false;
        if (surface.normal.y < 0.7f) return false;

        var height = surface.point.y - feet.y;
        if (height < MinLedgeHeight || height > highest) return false;

        var land = feet + forward * (face.distance + OntoTop);
        land.y = surface.point.y;
        if (Physics.CheckCapsule(land + Vector3.up * (StandRadius + 0.05f), land + Vector3.up * (StandHeight - StandRadius),
                StandRadius, LayerManager.MASK_WORLD)) return false;
        if (Physics.Raycast(feet + Vector3.up * (eye - 0.1f), Vector3.up, height + 0.2f, LayerManager.MASK_WORLD)) return false;

        top = land;
        return true;
    }
}
