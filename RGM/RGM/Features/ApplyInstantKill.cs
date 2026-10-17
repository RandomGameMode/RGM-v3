using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerStatsSystem;
using static RGM.Variables.Variable;

namespace RGM.Features;

/// <summary>
/// Immediately kills a target and attributes the kill to the supplied attacker.
/// </summary>
public static class ApplyInstantKill
{
    private const int RetryFrameCount = 3;
    private static readonly HashSet<Player> PendingRetries = [];

    public static bool Apply(Player attacker, Player target)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (!target.IsAlive)
            return false;

        bool applied = ApplyOnce(attacker, target, out bool dyingRaised);

        // Damage requests made from a Hurting callback can be discarded while
        // PlayerStatsSystem is still resolving the original hit. Retrying on a
        // later server frame avoids that re-entrancy without turning a genuine
        // Dying-event cancellation (such as a resurrection passive) into a kill.
        if (!applied && !dyingRaised && target.IsAlive)
            QueueRetry(attacker, target);

        return applied;
    }

    private static bool ApplyOnce(Player attacker, Player target, out bool dyingRaised)
    {
        dyingRaised = false;

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
        bool detectedDying = false;

        void OnDying(DyingEventArgs ev)
        {
            if (ev.Player == target)
                detectedDying = true;
        }

        Exiled.Events.Handlers.Player.Dying += OnDying;
        try
        {
            target.IsGodModeEnabled = false;

            if (spawnProtection.IsEnabled)
                spawnProtection.ServerSetState(0);

            return target.ReferenceHub.playerStats.DealDamage(handler);
        }
        finally
        {
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            dyingRaised = detectedDying;

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

    private static void QueueRetry(Player attacker, Player target)
    {
        if (!PendingRetries.Add(target))
            return;

        Timing.RunCoroutine(RetryOnLaterFrames(attacker, target));
    }

    private static IEnumerator<float> RetryOnLaterFrames(Player attacker, Player target)
    {
        try
        {
            for (int attempt = 0; attempt < RetryFrameCount; attempt++)
            {
                yield return Timing.WaitForOneFrame;

                if (attacker == null || target == null || !target.IsAlive)
                    yield break;

                if (ApplyOnce(attacker, target, out bool dyingRaised) || dyingRaised)
                    yield break;
            }
        }
        finally
        {
            PendingRetries.Remove(target);
        }
    }
}
