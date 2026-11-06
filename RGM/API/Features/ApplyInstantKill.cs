using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;
using static RGM.Variables.Variable;

namespace RGM.API.Features;

/// <summary>
/// Immediately kills a target and attributes the kill to the supplied attacker.
/// </summary>
/// <remarks>
/// The kill is attempted synchronously first. If the damage pipeline discards the
/// request (typically because it was issued from inside a Hurting callback), the
/// kill is retried on later server frames until it lands.
///
/// The retry window is deliberately bounded by both a minimum attempt count and a
/// real-time deadline, so it holds up regardless of the server tick rate: at 1-5 TPS
/// a single frame can last up to a second, so a pure frame-count limit would expire
/// long before the damage pipeline has had a chance to settle.
///
/// This intentionally uses MEC coroutines rather than <see cref="System.Threading.Tasks.Task"/>:
/// all game state (damage, effects, events) may only be touched from Unity's main
/// thread, and a coroutine resumes there on the next frame.
/// </remarks>
public static class ApplyInstantKill
{
    /// <summary>Minimum number of retry frames, however slow the server is running.</summary>
    private const int MinRetryAttempts = 10;

    /// <summary>Real-time (seconds) the retries keep going for once the minimum is reached.</summary>
    private const float MaxRetryDuration = 10f;

    private static readonly HashSet<Player> PendingRetries = [];

    /// <summary>
    /// <see langword="true"/> while this class is itself dealing its kill damage.
    /// Hurting handlers that call <see cref="Apply"/> must ignore events raised in
    /// this state, otherwise the damage re-enters the handler and overflows the stack.
    /// </summary>
    public static bool IsApplying { get; private set; }

    /// <summary>
    /// Kills <paramref name="target"/>, crediting <paramref name="attacker"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the target was killed during this call. If the kill had
    /// to be deferred, <see langword="false"/> is returned and it is completed on a later frame.
    /// </returns>
    public static bool Apply(Player attacker, Player target)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (!target.IsAlive)
            return false;

        bool applied = TryApplyOnce(attacker, target, out bool dyingRaised);

        // Damage requests made from a Hurting callback can be discarded while
        // PlayerStatsSystem is still resolving the original hit. Retrying on a
        // later server frame avoids that re-entrancy without turning a genuine
        // Dying-event cancellation (such as a resurrection passive) into a kill.
        if (!applied && !dyingRaised && target.IsAlive)
            QueueRetry(attacker, target);

        return applied || !target.IsAlive;
    }

    private static bool TryApplyOnce(Player attacker, Player target, out bool dyingRaised)
    {
        try
        {
            return ApplyOnce(attacker, target, out dyingRaised);
        }
        catch (Exception e)
        {
            // Never let one failed attempt abort the retry loop.
            Log.Error($"[ApplyInstantKill] {e}");
            dyingRaised = false;
            return false;
        }
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

        bool previousIsApplying = IsApplying;
        IsApplying = true;
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
            IsApplying = previousIsApplying;
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
            RoleTypeId startRole = target.Role.Type;

            // realtimeSinceStartup is wall-clock time, so it is unaffected by the
            // server's TPS or by Time.timeScale.
            float deadline = Time.realtimeSinceStartup + MaxRetryDuration;

            for (int attempt = 0;
                 attempt < MinRetryAttempts || Time.realtimeSinceStartup < deadline;
                 attempt++)
            {
                yield return Timing.WaitForOneFrame;
                if (!IsRetryStillValid(attacker, target, startRole) ||
                    TryApplyOnce(attacker, target, out bool dyingRaised) ||
                    dyingRaised || 
                    !target.IsAlive) yield break;
            }

            Log.Warn(
                $"[ApplyInstantKill] Gave up killing {target.Nickname} " +
                $"after {MinRetryAttempts}+ attempts / {MaxRetryDuration}s.");
        }
        finally
        {
            PendingRetries.Remove(target);
        }
    }

    private static bool IsRetryStillValid(Player attacker, Player target, RoleTypeId startRole)
    {
        if (attacker == null || attacker.ReferenceHub == null || !attacker.IsConnected)
            return false;

        if (target == null || target.ReferenceHub == null || !target.IsConnected)
            return false;

        // A role change means the target died/respawned in the meantime; the
        // original request no longer applies to the new life.
        return target.IsAlive && target.Role.Type == startRole;
    }
}
