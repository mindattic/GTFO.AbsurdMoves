using BepInEx.Configuration;
using Enemies;
using GTFO.API;
using Il2CppInterop.Runtime;
using Player;
using UnityEngine;

namespace GTFO.EZ.AbsurdMoves;

/// <summary>
/// Max Payne bullet time: a tap of the key slows every enemy (their animation and their navmesh
/// movement) for a few seconds while players keep full speed - the game clock is never touched.
/// The world's sound drops to a hollow, muffled mix with a deep boom going in and a heartbeat under
/// it. A press while it's running or cooling down does nothing. The trigger is broadcast so the host,
/// who drives the enemies, slows them for everyone.
/// </summary>
internal sealed class BulletTime : MonoBehaviour
{
    private const string StartEvent = "MindAttic.GTFO.EZ.AbsurdMoves.BulletTime";
    private const float RampIn = 0.3f;
    private const float RampOut = 0.6f;
    private const float RescanEvery = 0.5f;

    private static ConfigEntry<bool> featureOn = null!;
    private static ConfigEntry<KeyCode> key = null!;
    private static ConfigEntry<float> duration = null!;
    private static ConfigEntry<float> cooldown = null!;
    private static ConfigEntry<float> enemySpeed = null!;
    private static ConfigEntry<string> muffle = null!;
    private static ConfigEntry<string> enterSound = null!;
    private static ConfigEntry<string> exitSound = null!;
    private static ConfigEntry<string> loopStart = null!;
    private static ConfigEntry<string> loopStop = null!;

    private sealed class Slowed
    {
        public EnemyAgent Enemy = null!;
        public float BaseAnim, AppliedAnim = -1f, BaseNav, AppliedNav = -1f;
    }

    private static readonly Dictionary<IntPtr, Slowed> slowed = new();
    private static readonly List<(uint Id, float Target, float Before)> rtpcs = new();
    private static float startedAt = -1f;
    private static float readyAt;
    private static float nextScan;

    public BulletTime(IntPtr ptr) : base(ptr) { }

    internal static void Bind(ConfigFile config)
    {
        const string s = "Bullet Time";
        featureOn = config.Bind(s, "Enabled", true, "Turns bullet time on or off. On: tap the key to slow every enemy to a crawl for a few seconds; players keep full speed.");
        key = config.Bind(s, "Key", KeyCode.LeftAlt,
            "Starts bullet time as you let go of it. A press with another key in it (Alt+Tab, Alt+Enter) or one that loses the game focus doesn't count.");
        duration = config.Bind(s, "DurationSeconds", 5f, "How long bullet time lasts.");
        cooldown = config.Bind(s, "CooldownSeconds", 10f, "Seconds after bullet time ends before it can start again.");
        enemySpeed = config.Bind(s, "EnemySpeed", 0.25f, "Enemy speed during bullet time (1 = normal, 0.25 = quarter speed).");
        muffle = config.Bind(s, "SoundMuffle", "SOUND_EFFECT_MUFFLE=100, GAME_MENU_MUFFLE=100",
            "The game's Wwise mix parameters (NAME=value, comma-separated) pushed during bullet time for the hollow, underwater sound; each goes back to what it was after. Empty = no muffle.");
        enterSound = config.Bind(s, "EnterSound", "STINGER_LOW_BOOM", "Played as bullet time starts (a deep boom). Empty = silent.");
        exitSound = config.Bind(s, "ExitSound", "COMBAT_FADING_PULSE_LEVEL_1", "Played as bullet time ends. Empty = silent.");
        loopStart = config.Bind(s, "LoopSound", "ENEMYHEARTBEATLOOP", "A loop under bullet time (a slow heartbeat). Empty = none.");
        loopStop = config.Bind(s, "LoopStopSound", "ENEMYHEARTBEATLOOPEND", "Stops the loop.");
    }

    // A teammate's trigger always starts it here, even if this machine is still cooling down from its
    // own (clocks and cooldown settings differ between players): otherwise the host could ignore it
    // and keep the enemies at full speed while the teammate sees bullet time.
    // The handler runs inside GTFO-API's prefix on the game's packet receive, so nothing escapes it
    // (see GroundPound.Init); a start that fails halfway is ended so nothing stays slowed or muffled.
    internal static void Init() => NetworkAPI.RegisterEvent<Trigger>(StartEvent, (_, _) =>
    {
        try
        {
            Begin(fromTeammate: true);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdMoves teammate's bullet time failed: {e}");
            try
            {
                if (Active) End();
            }
            catch (Exception) { }
        }
    });

    private static bool Active => startedAt >= 0f;

    private void Update()
    {
        try
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            {
                if (Active) End();
                readyAt = 0f;
                armed = false;
                return;
            }

            if (KeyTapped() && Begin()) NetworkAPI.InvokeEvent(StartEvent, new Trigger());

            if (Active) Tick();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdMoves bullet time failed: {e}");
            if (Active) End();
        }
    }

    // The key counts on release, and only if nothing else was pressed while it was held and the game
    // kept focus: the default Left Alt is also the Alt of Alt+Tab, and the Alt press reaches the game
    // before the window loses focus, so a press-to-start would fire on every Alt+Tab.
    private static bool armed;

    private static bool KeyTapped()
    {
        if (!featureOn.Value || FocusStateManager.CurrentState != eFocusState.FPS || !Application.isFocused)
        {
            armed = false;
            return false;
        }
        if (Input.GetKeyDown(key.Value)) armed = true;
        else if (armed && Input.anyKeyDown) armed = false; // a chord: Alt+Tab, Alt+F4, Alt+Enter
        if (!armed || !Input.GetKeyUp(key.Value)) return false;
        armed = false;
        return true;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) armed = false;
    }

    private static bool Begin(bool fromTeammate = false)
    {
        if (Active || (!fromTeammate && Time.time < readyAt)) return false;
        startedAt = Time.time;
        nextScan = 0f;
        rtpcs.Clear();
        foreach (var pair in muffle.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var value)) continue;
            var id = AkSoundEngine.GetIDFromString(parts[0]);
            rtpcs.Add((id, value, CellSound.GetGlobalRTPCValue(id)));
        }
        if (PlayerManager.GetLocalPlayerAgent() is { } player) Sounds.Play(enterSound.Value, player.Position);
        return true;
    }

    // 0 -> 1 over the ramp-in, 1 for the body, back to 0 over the ramp-out.
    private static float Strength(float elapsed)
    {
        var total = Mathf.Max(duration.Value, RampIn + RampOut);
        if (elapsed < RampIn) return Mathf.SmoothStep(0f, 1f, elapsed / RampIn);
        if (elapsed > total - RampOut) return Mathf.SmoothStep(0f, 1f, (total - elapsed) / RampOut);
        return 1f;
    }

    private static void Tick()
    {
        var elapsed = Time.time - startedAt;
        if (elapsed >= Mathf.Max(duration.Value, RampIn + RampOut))
        {
            End();
            return;
        }
        var strength = Strength(elapsed);

        foreach (var (id, target, before) in rtpcs) CellSound.SetGlobalRTPCValue(id, Mathf.Lerp(before, target, strength));

        var player = PlayerManager.GetLocalPlayerAgent();
        if (player != null) LoopSounds.Touch("bullet-time", loopStart.Value, loopStop.Value, player.Position);

        if (Time.time >= nextScan)
        {
            nextScan = Time.time + RescanEvery;
            foreach (var found in UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<EnemyAgent>()))
            {
                var enemy = found.TryCast<EnemyAgent>();
                if (enemy != null && !slowed.ContainsKey(enemy.Pointer)) slowed[enemy.Pointer] = new Slowed { Enemy = enemy };
            }
        }

        var factor = Mathf.Lerp(1f, Mathf.Clamp(enemySpeed.Value, 0.01f, 1f), strength);
        List<IntPtr>? gone = null;
        foreach (var (ptr, s) in slowed)
        {
            try
            {
                if (s.Enemy == null || s.Enemy.WasCollected) { (gone ??= new()).Add(ptr); continue; }
                Apply(s, factor);
            }
            catch (Exception)
            {
                (gone ??= new()).Add(ptr);
            }
        }
        if (gone != null) foreach (var ptr in gone) slowed.Remove(ptr);
    }

    // The game sets enemy speeds itself (state changes, running): a value that isn't the one we last
    // wrote is the game's new base, and ours is applied on top of it.
    private static void Apply(Slowed s, float factor)
    {
        var anim = s.Enemy.Anim;
        if (anim != null)
        {
            if (!Mathf.Approximately(anim.speed, s.AppliedAnim)) s.BaseAnim = anim.speed;
            s.AppliedAnim = s.BaseAnim * factor;
            anim.speed = s.AppliedAnim;
        }
        var nav = s.Enemy.AI?.m_navMeshAgent;
        if (nav != null)
        {
            if (!Mathf.Approximately(nav.speed, s.AppliedNav)) s.BaseNav = nav.speed;
            s.AppliedNav = s.BaseNav * factor;
            nav.speed = s.AppliedNav;
        }
    }

    private static void End()
    {
        foreach (var s in slowed.Values)
        {
            try
            {
                if (s.Enemy == null || s.Enemy.WasCollected) continue;
                Apply(s, 1f);
            }
            catch (Exception) { }
        }
        slowed.Clear();
        foreach (var (id, _, before) in rtpcs) CellSound.SetGlobalRTPCValue(id, before);
        rtpcs.Clear();
        startedAt = -1f;
        readyAt = Time.time + cooldown.Value;
        if (GameStateManager.CurrentStateName == eGameStateName.InLevel && PlayerManager.GetLocalPlayerAgent() is { } player)
            Sounds.Play(exitSound.Value, player.Position);
    }

    // GTFO-API sends structs as raw bytes; this one carries nothing but needs a size.
#pragma warning disable CS0649
    private readonly struct Trigger
    {
        public readonly byte Unused;
    }
#pragma warning restore CS0649
}
