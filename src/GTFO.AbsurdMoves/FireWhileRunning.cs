using BepInEx.Configuration;
using Gear;
using HarmonyLib;

namespace GTFO.AbsurdMoves;

// The game's gun logic (BulletWeaponArchetype.Update) drops the trigger whenever its owner's
// locomotion says it's running. For the local player's own gun, the running flag reads false for
// the length of that one call, then goes straight back - the sprint itself carries on.
[HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.Update))]
internal static class FireWhileRunning
{
    internal static ConfigEntry<bool> Enabled = null!;

    internal static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("Fire On The Move", "Fire While Running Enabled", true,
            "Turns firing while sprinting on or off. On: your guns fire while you sprint, without slowing you down. " +
            "Off: vanilla (the trigger does nothing while sprinting).");
    }

    [HarmonyPrefix]
    private static void Prefix(BulletWeaponArchetype __instance, out bool __state)
    {
        __state = false;
        if (!Enabled.Value) return;
        var loc = FireGate.LocalLocomotion(__instance);
        if (loc == null || !loc.IsRunning) return;
        __state = true;
        loc.IsRunning = false;
    }

    [HarmonyPostfix]
    private static void Postfix(BulletWeaponArchetype __instance, bool __state)
    {
        if (!__state) return;
        var loc = __instance.m_owner?.Locomotion;
        if (loc != null) loc.IsRunning = true;
    }
}
