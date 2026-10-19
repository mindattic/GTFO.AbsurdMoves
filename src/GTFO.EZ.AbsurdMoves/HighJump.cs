using BepInEx.Configuration;
using Player;
using UnityEngine;

namespace GTFO.EZ.AbsurdMoves;

/// <summary>
/// High jump: stay crouched on the ground until you start to flash (ChargeSeconds), then press Jump
/// and you're launched straight up about Height metres. It's the game's own jump state with the
/// upward speed raised to reach that height, so the game moves (and syncs) you as for any jump; the
/// way back down takes no fall damage. Leaving the crouch before jumping drops the charge.
/// </summary>
internal static class HighJump
{
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<float> ChargeSeconds = null!;
    internal static ConfigEntry<float> Height = null!;

    private static readonly Color FlashColor = new(1f, 1f, 1f);
    private const float FlashHz = 8f;

    internal static void Bind(ConfigFile config)
    {
        const string section = "High Jump";
        Enabled = config.Bind(section, "High Jump Enabled", true,
            "Turns the high jump on or off. On: stay crouched on the ground until you start to flash, then press Jump " +
            "to launch straight up. The landing takes no fall damage.");
        ChargeSeconds = config.Bind(section, "Charge Time (seconds)", 1f, "How long you stay crouched before you flash and the high jump is ready.");
        Height = config.Bind(section, "Height (meters)", 10f, "How high the high jump launches you.");
    }

    private static float _crouchedFor;
    private static bool _launched;
    private static GameObject? _flash;
    private static EffectLight? _light;

    internal static bool Ready => _crouchedFor >= Mathf.Max(0.1f, ChargeSeconds.Value);

    internal static void Reset()
    {
        _crouchedFor = 0f;
        _launched = false;
        SetFlash(null, false);
    }

    internal static void Tick(PlayerAgent agent, PlayerLocomotion loc)
    {
        // On the way back down from a high jump: the fall keeps starting where you are, so the
        // landing counts as no fall at all.
        if (_launched)
        {
            if (Moves.InAir(loc)) Moves.ResetFallStart(agent, loc);
            else _launched = false;
        }

        if (!Enabled.Value || loc.m_currentStateEnum != PlayerLocomotion.PLOC_State.Crouch)
        {
            _crouchedFor = 0f;
            SetFlash(agent, false);
            return;
        }

        _crouchedFor += Time.deltaTime;
        SetFlash(agent, Ready);
        if (!Ready || !Moves.InPlay || !InputMapper.GetButtonDown.Invoke(InputAction.Jump, eFocusState.FPS)) return;

        _crouchedFor = 0f;
        SetFlash(agent, false);
        _launched = true;
        loc.ChangeState(PlayerLocomotion.PLOC_State.Jump, false);
        // v = sqrt(2gh): straight up to Height under gravity.
        var height = Mathf.Max(0.5f, Height.Value);
        loc.m_verticalVelocity = Vector3.up * Mathf.Sqrt(2f * Physics.gravity.magnitude * height);
        Moves.ResetFallStart(agent, loc);
        Moves.JumpSway(agent);
    }

    // A white light blinking at the player while the high jump is charged.
    private static void SetFlash(PlayerAgent? agent, bool on)
    {
        if (!on || agent == null)
        {
            if (_light != null) _light.enabled = false;
            return;
        }
        if (_flash == null || _light == null)
        {
            _flash = new GameObject("GTFO.EZ.AbsurdMoves High Jump Flash");
            UnityEngine.Object.DontDestroyOnLoad(_flash);
            _light = _flash.AddComponent<EffectLight>();
            _light.Color = FlashColor;
            _light.Range = 4f;
        }
        _flash.transform.position = agent.Position + Vector3.up * 1.2f;
        _light.Intensity = Mathf.Repeat(Time.time * FlashHz, 1f) < 0.5f ? 3f : 0f;
        _light.enabled = true;
    }
}
