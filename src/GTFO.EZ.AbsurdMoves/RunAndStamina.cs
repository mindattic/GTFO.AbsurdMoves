using BepInEx.Configuration;
using GameData;
using HarmonyLib;
using Player;

namespace GTFO.EZ.AbsurdMoves;

/// <summary>
/// Faster sprinting and more stamina, as multipliers on the game's own player data (applied once
/// as the game loads its data, so changes take effect on restart). Stamina in GTFO is a 0-1 meter
/// with no maximum to raise, so "more stamina" divides what every action costs and speeds regen up
/// by the same factor.
/// </summary>
[HarmonyPatch(typeof(GameDataInit), nameof(GameDataInit.Initialize))]
internal static class RunAndStamina
{
    internal static ConfigEntry<bool> RunSpeedEnabled = null!;
    internal static ConfigEntry<float> RunSpeedMultiplier = null!;
    internal static ConfigEntry<bool> StaminaEnabled = null!;
    internal static ConfigEntry<float> StaminaMultiplier = null!;

    internal static void Bind(ConfigFile config)
    {
        RunSpeedEnabled = config.Bind("Run Speed", "Run Speed Enabled", true,
            "Turns the faster sprint on or off. Off: vanilla sprint speed. Takes effect on restart.");
        RunSpeedMultiplier = config.Bind("Run Speed", "Max Run Speed Multiplier", 1.25f,
            "Multiplies top sprint speed. 1.25 = 25% faster. Stacks with GTFO.EZ's Run Speed Multiplier if both are installed. Takes effect on restart.");

        StaminaEnabled = config.Bind("Stamina", "Stamina Enabled", true,
            "Turns the extra stamina on or off. Off: vanilla stamina. Takes effect on restart.");
        StaminaMultiplier = config.Bind("Stamina", "Stamina Multiplier", 1.5f,
            "How much further stamina goes: every action's stamina cost (sprinting, jumping, walking, crouching, " +
            "strafing) is divided by this and stamina regen is multiplied by it. 1.5 = half as much again. Takes effect on restart.");
    }

    [HarmonyPostfix]
    private static void Postfix()
    {
        try
        {
            var player = GameDataBlockBase<PlayerDataBlock>.GetBlock(1);
            if (player == null)
            {
                Plugin.Logger.LogWarning("PlayerDataBlock (id 1) not found; run speed and stamina left at vanilla.");
                return;
            }

            if (RunSpeedEnabled.Value && RunSpeedMultiplier.Value > 0f)
                player.runMoveSpeed *= RunSpeedMultiplier.Value;

            var mult = StaminaMultiplier.Value;
            if (StaminaEnabled.Value && mult > 0f)
            {
                player.StaminaJumpCost = Cheaper(player.StaminaJumpCost, mult);
                player.StaminaSneakCost = Cheaper(player.StaminaSneakCost, mult);
                player.StaminaWalkCost = Cheaper(player.StaminaWalkCost, mult);
                player.StaminaRunCost = Cheaper(player.StaminaRunCost, mult);
                player.StaminaCrouchEnterCost = Cheaper(player.StaminaCrouchEnterCost, mult);
                player.StaminaQuickStrafeCost = Cheaper(player.StaminaQuickStrafeCost, mult);
                player.StaminaRegenRestingInCombat *= mult;
                player.StaminaRegenNotRestingInCombat *= mult;
                player.StaminaRegenRestingOutOfCombat *= mult;
                player.StaminaRegenNotRestingOutOfCombat *= mult;
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"GTFO.EZ.AbsurdMoves couldn't apply run speed / stamina: {e}");
        }
    }

    private static PlayerStamina.ActionCost Cheaper(PlayerStamina.ActionCost cost, float mult)
    {
        cost.baseStaminaCostInCombat /= mult;
        cost.baseStaminaCostOutOfCombat /= mult;
        return cost;
    }
}
