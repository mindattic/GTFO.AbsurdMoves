using BepInEx.Configuration;
using Gear;
using HarmonyLib;
using Player;

namespace GTFO.AbsurdMoves;

// The game's gun logic (BulletWeaponArchetype.Update) drops the trigger whenever its owner's
// locomotion is in the Jump or Fall state. For the local player's own gun, it's shown a grounded
// state for the length of that one call, then the real state goes straight back.
[HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.Update))]
internal static class FireMidAir
{
    internal static ConfigEntry<bool> Enabled = null!;

    internal static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("Fire On The Move", "Fire Mid-Air Enabled", true,
            "Turns firing in the air on or off. On: your guns fire while jumping or falling. Off: vanilla (the trigger " +
            "does nothing until you land).");
    }

    private const byte Untouched = byte.MaxValue;

    [HarmonyPrefix]
    private static void Prefix(BulletWeaponArchetype __instance, out byte __state)
    {
        __state = Untouched;
        if (!Enabled.Value) return;
        var loc = FireGate.LocalLocomotion(__instance);
        if (loc == null || !Moves.InAir(loc)) return;
        __state = (byte)loc.m_currentStateEnum;
        loc.m_currentStateEnum = PlayerLocomotion.PLOC_State.Stand;
    }

    [HarmonyPostfix]
    private static void Postfix(BulletWeaponArchetype __instance, byte __state)
    {
        if (__state == Untouched) return;
        var loc = __instance.m_owner?.Locomotion;
        if (loc != null) loc.m_currentStateEnum = (PlayerLocomotion.PLOC_State)__state;
    }
}

internal static class FireGate
{
    // The local player's locomotion, when this gun is theirs (not a teammate's or a bot's).
    internal static PlayerLocomotion? LocalLocomotion(BulletWeaponArchetype archetype)
    {
        var owner = archetype.m_owner;
        if (owner == null || !PlayerManager.TryGetLocalPlayerAgent(out var local) || local == null) return null;
        return owner.Pointer == local.Pointer ? owner.Locomotion : null;
    }
}
