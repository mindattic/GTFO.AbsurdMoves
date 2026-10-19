using BepInEx.Configuration;
using Enemies;
using FX_EffectSystem;
using GTFO.API;
using Il2CppInterop.Runtime.Attributes;
using Player;
using SNetwork;
using UnityEngine;
using UnityEngine.AI;

namespace GTFO.AbsurdMoves;

/// <summary>
/// Ground pound: press Crouch in mid-air, high enough above the floor, and you slam straight down
/// at twice the fall speed with no fall damage, landing in a concussion wave that shoves every
/// enemy around you back (and staggers them) in a spray of debris. The landing is broadcast: every
/// client shows the debris, and the host, who owns enemy positions, does the shoving.
/// </summary>
internal static class GroundPound
{
    private const string ImpactEvent = "MindAttic.GTFO.AbsurdMoves.GroundPound";
    private const float FallDamageGraceSeconds = 0.5f;
    // Each burst is a full impact effect; a mistyped count shouldn't spawn thousands at once.
    private const int MaxDebris = 50;

    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<float> MinHeight = null!;
    internal static ConfigEntry<float> MinSpeed = null!;
    internal static ConfigEntry<float> Radius = null!;
    internal static ConfigEntry<float> PushDistance = null!;
    internal static ConfigEntry<float> Damage = null!;
    internal static ConfigEntry<float> Stagger = null!;
    internal static ConfigEntry<int> DebrisCount = null!;
    internal static ConfigEntry<string> ImpactSound = null!;

    internal static void Bind(ConfigFile config)
    {
        const string s = "Ground Pound";
        Enabled = config.Bind(s, "Ground Pound Enabled", true,
            "Turns the ground pound on or off. On: press Crouch in mid-air to slam down at double fall speed, taking no " +
            "fall damage, and land in a shockwave that pushes enemies back. Only works high enough above the floor (below).");
        MinHeight = config.Bind(s, "Min Height (meters)", 2f, "How far above the floor you must be for a ground pound to start.");
        MinSpeed = config.Bind(s, "Min Slam Speed (m/s)", 12f, "Slowest the slam drops at, even if you were barely falling yet. It doubles your fall speed when that's faster.");
        Radius = config.Bind(s, "Shockwave Radius (meters)", 5f, "How far from where you land the shockwave reaches.");
        PushDistance = config.Bind(s, "Pushback Distance (meters)", 4f, "How far an enemy right next to you is shoved (less further out).");
        Damage = config.Bind(s, "Shockwave Damage", 10f, "Damage to each enemy in the shockwave (less further out). 0 = push only.");
        Stagger = config.Bind(s, "Shockwave Stagger", 3f, "Stagger multiplier of the shockwave's hit.");
        DebrisCount = config.Bind(s, "Debris Count", 10, "Dust-and-scorch bursts scattered around the landing - a crater, not a single puff. At most 50.");
        ImpactSound = config.Bind(s, "Impact Sound", "STINGER_LOW_BOOM", "Played on landing (a game sound event name). Empty = silent.");
    }

    // The handler runs inside GTFO-API's prefix on the game's packet receive (SNet_Replication.RecieveBytes):
    // an exception escaping it would hand GTFO-API's packet on to the game as if it were its own.
    internal static void Init()
    {
        NetworkAPI.RegisterEvent<ImpactMessage>(ImpactEvent, (sender, message) =>
        {
            try
            {
                Land(new Vector3(message.X, message.Y, message.Z), PlayerFor(sender));
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"GTFO.AbsurdMoves teammate's ground pound failed: {e.Message}");
            }
        });
    }

    private static bool _pounding;
    private static float _speed;

    internal static bool Active => _pounding;

    // A pound still falling after this long is stuck (an unstandable slope keeps the game in its
    // fall state): it's dropped rather than pinning you in place.
    private const float MaxPoundSeconds = 6f;

    private static float _graceUntil;
    private static float _startedAt;
    // Whether the game itself had fall damage switched off before the pound, so ending the pound
    // puts it back as it was instead of always switching it back on.
    private static bool _gameImmunity;

    internal static void Reset(PlayerLocomotion? loc)
    {
        if (!_pounding && _graceUntil <= 0f) return;
        _pounding = false;
        _graceUntil = 0f;
        try
        {
            if (loc != null && !loc.WasCollected) loc.DisableFallDamageTemporarily = _gameImmunity;
        }
        catch (Exception)
        {
            // Gone with the level.
        }
    }

    // Landing on your feet - not grabbing a ladder, getting downed or grabbed mid-fall.
    private static bool OnFeet(PlayerLocomotion loc) => loc.m_currentStateEnum is
        PlayerLocomotion.PLOC_State.Stand or PlayerLocomotion.PLOC_State.Crouch or PlayerLocomotion.PLOC_State.Run or
        PlayerLocomotion.PLOC_State.Land or PlayerLocomotion.PLOC_State.StandStill;

    internal static void Tick(PlayerAgent agent, PlayerLocomotion loc)
    {
        if (!_pounding && _graceUntil > 0f && Time.time > _graceUntil)
        {
            _graceUntil = 0f;
            loc.DisableFallDamageTemporarily = _gameImmunity;
        }

        if (_pounding)
        {
            if (!Moves.InAir(loc))
            {
                if (!OnFeet(loc))
                {
                    Reset(loc);
                    return;
                }
                _pounding = false;
                _graceUntil = Time.time + FallDamageGraceSeconds;
                var at = agent.Position;
                Land(at, agent);
                NetworkAPI.InvokeEvent(ImpactEvent, new ImpactMessage(at));
                return;
            }
            if (Time.time - _startedAt > MaxPoundSeconds)
            {
                Reset(loc);
                return;
            }
            // Gravity counts twice while slamming: the fall speed keeps doubling up on vanilla's.
            var v = loc.m_verticalVelocity;
            v.y = Mathf.Min(v.y + Physics.gravity.y * Time.deltaTime, -_speed);
            loc.m_verticalVelocity = v;
            loc.m_horizontalVelocity = Vector3.zero;
            loc.DisableFallDamageTemporarily = true;
            Moves.ResetFallStart(agent, loc);
            return;
        }

        if (!Enabled.Value || Mantle.Active || !Moves.InAir(loc) || !Moves.InPlay) return;
        if (!InputMapper.GetButtonDown.Invoke(InputAction.Crouch, eFocusState.FPS)) return;
        if (Physics.Raycast(agent.Position + Vector3.up * 0.2f, Vector3.down, MinHeight.Value + 0.2f, LayerManager.MASK_WORLD)) return;

        _pounding = true;
        _startedAt = Time.time;
        _speed = Mathf.Max(MinSpeed.Value, -loc.m_verticalVelocity.y * 2f);
        // Inside the last pound's landing grace the immunity on now is the mod's own, not the game's:
        // keep the value captured before that pound.
        if (_graceUntil <= 0f) _gameImmunity = loc.DisableFallDamageTemporarily;
        _graceUntil = 0f;
        loc.DisableFallDamageTemporarily = true;
    }

    // Everyone: the debris and the boom. The host: the shove and the hit, credited to the pounder.
    [HideFromIl2Cpp]
    private static void Land(Vector3 at, PlayerAgent? pounder)
    {
        Sounds.Play(ImpactSound.Value, at);
        SpawnDebris(at);
        if (!SNet.IsMaster) return;

        var radius = Radius.Value;
        if (radius <= 0f) return; // no shockwave (and no divide by zero below)
        var seen = new HashSet<IntPtr>();
        foreach (var collider in Physics.OverlapSphere(at, radius, LayerManager.MASK_ENEMY_DAMAGABLE))
        {
            var limb = collider.GetComponent<Dam_EnemyDamageLimb>();
            var enemy = limb?.m_base?.Owner;
            if (enemy == null || !enemy.Alive || !seen.Add(enemy.Pointer)) continue;

            var away = enemy.Position - at;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = UnityEngine.Random.insideUnitSphere;
            away.y = 0f;
            var falloff = Mathf.Clamp01(1f - away.magnitude / radius);
            away.Normalize();

            PoundPushes.Start(enemy, away, Mathf.Max(0f, PushDistance.Value) * Mathf.Lerp(0.35f, 1f, falloff));
            if (Damage.Value > 0f && pounder != null)
            {
                try
                {
                    limb!.BulletDamage(Damage.Value * Mathf.Lerp(0.5f, 1f, falloff), pounder.Cast<Agents.Agent>(),
                        limb.transform.position, away, -away, allowDirectionalBonus: false, Stagger.Value, 1f, 0u);
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogWarning($"GTFO.AbsurdMoves ground pound hit failed: {e.Message}");
                }
            }
        }
    }

    [HideFromIl2Cpp]
    private static void SpawnDebris(Vector3 center)
    {
        try
        {
            FX_Manager.PlayEffect(false, FX_GroupName.Impact_Concrete, center + Vector3.up * 0.05f, Quaternion.LookRotation(Vector3.up), true);
            var spread = Mathf.Max(1f, Radius.Value * 0.5f);
            var count = Mathf.Clamp(DebrisCount.Value, 0, MaxDebris);
            for (var i = 0; i < count; i++)
            {
                var offset = UnityEngine.Random.insideUnitCircle * spread;
                var spot = center + new Vector3(offset.x, 1f, offset.y);
                var point = Physics.Raycast(spot, Vector3.down, out var hit, 2.5f, LayerManager.MASK_WORLD) ? hit.point : center + new Vector3(offset.x, 0f, offset.y);
                FX_Manager.PlayEffect(false, FX_GroupName.Impact_Concrete, point + Vector3.up * 0.05f, Quaternion.LookRotation(Vector3.up), true);
            }
        }
        catch (Exception)
        {
            // The debris is a nicety; the shockwave itself still lands.
        }
    }

    private static PlayerAgent? PlayerFor(ulong lookup)
    {
        foreach (var player in PlayerManager.PlayerAgentsInLevel)
            if (player != null && player.Owner != null && player.Owner.Lookup == lookup) return player;
        return null;
    }

    // GTFO-API sends structs as raw bytes.
    private readonly struct ImpactMessage
    {
        public readonly float X, Y, Z;

        public ImpactMessage(Vector3 at) => (X, Y, Z) = (at.x, at.y, at.z);
    }
}

/// <summary>
/// Host only: slides shoved enemies across their walkable ground, fast then easing off, stopping at
/// walls and ledges so nothing goes through geometry. Teammates see it through the game's own enemy
/// position sync.
/// </summary>
internal sealed class PoundPushes : MonoBehaviour
{
    private const int AllAreas = -1; // NavMesh.AllAreas, missing from the interop build
    private const float Sharpness = 12f;
    private const float MaxSeconds = 0.6f;

    private sealed class Push
    {
        public EnemyAgent Enemy = null!;
        public Vector3 Direction;
        public float Remaining, Age;
    }

    private static readonly Dictionary<IntPtr, Push> pushes = new();

    public PoundPushes(IntPtr ptr) : base(ptr) { }

    [HideFromIl2Cpp]
    internal static void Start(EnemyAgent enemy, Vector3 direction, float distance) =>
        pushes[enemy.Pointer] = new Push { Enemy = enemy, Direction = direction, Remaining = distance };

    private void Update()
    {
        if (pushes.Count == 0) return;
        List<IntPtr>? done = null;
        foreach (var (key, push) in pushes)
        {
            try
            {
                var enemy = push.Enemy;
                push.Age += Time.deltaTime;
                if (enemy == null || !enemy.Alive || push.Remaining < 0.02f || push.Age > MaxSeconds)
                {
                    (done ??= new()).Add(key);
                    continue;
                }

                var step = push.Remaining * (1f - Mathf.Exp(-Sharpness * Time.deltaTime));
                var from = enemy.Position;
                if (!NavMesh.SamplePosition(from, out var fromHit, 1f, AllAreas)) { (done ??= new()).Add(key); continue; }
                var to = from + push.Direction * step;
                if (NavMesh.Raycast(fromHit.position, to, out var blocked, AllAreas))
                {
                    to = blocked.position;
                    push.Remaining = 0f;
                }
                else
                {
                    push.Remaining -= step;
                }
                if (NavMesh.SamplePosition(to, out var toHit, 1f, AllAreas)) enemy.AI.NavmeshAgentWarp(toHit.position);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"GTFO.AbsurdMoves ground pound push failed: {e.Message}");
                (done ??= new()).Add(key);
            }
        }
        if (done != null) foreach (var key in done) pushes.Remove(key);
    }
}
