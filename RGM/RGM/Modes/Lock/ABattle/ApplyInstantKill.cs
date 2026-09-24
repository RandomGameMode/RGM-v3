using System;
using CustomPlayerEffects;
using Exiled.API.Features;
using PlayerStatsSystem;
using static RGM.Variables.Variable;

namespace RGM.Modes;

/// <summary>
/// Immediately kills a target and attributes the kill to the supplied attacker.
/// </summary>
public static class ApplyInstantKill
{
    public static bool Apply(Player attacker, Player target)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (!target.IsAlive)
            return false;

        // Crushed is ABattle's unblockable damage category. It bypasses damage
        // limits, reflection, and ability-based invulnerability while retaining
        // the normal Dying/Death event flow.
        var handler = new ScpDamageHandler(
            attacker.ReferenceHub,
            DeathTranslations.Crushed);

        bool nativeGodMode = target.IsGodModeEnabled;
        int temporaryGodModeEntries = 0;
        while (GodModePlayers.Remove(target))
            temporaryGodModeEntries++;

        SpawnProtected spawnProtection =
            target.ReferenceHub.playerEffectsController.GetEffect<SpawnProtected>();
        byte spawnProtectionIntensity = spawnProtection.Intensity;
        float spawnProtectionTimeLeft = spawnProtection.TimeLeft;

        try
        {
            target.IsGodModeEnabled = false;

            if (spawnProtection.IsEnabled)
                spawnProtection.ServerSetState(0);

            return target.ReferenceHub.playerStats.DealDamage(handler);
        }
        finally
        {
            // A resurrection/passive may cancel death. Restore protections only
            // when the original player is still alive.
            if (target.IsAlive)
            {
                target.IsGodModeEnabled = nativeGodMode;

                for (int i = 0; i < temporaryGodModeEntries; i++)
                    GodModePlayers.Add(target);

                if (spawnProtectionIntensity > 0)
                    spawnProtection.ServerSetState(
                        spawnProtectionIntensity,
                        spawnProtectionTimeLeft);
            }
        }
    }
}
