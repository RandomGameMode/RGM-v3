using System;
using Exiled.API.Features;
using PlayerStatsSystem;

namespace RGM.Modes;

/// <summary>
/// Deals damage equal to the target's maximum health, ignoring damage modifiers.
/// </summary>
public static class ApplyLethalDamage
{
    public static bool Apply(Player attacker, Player target) =>
        Apply(attacker, target, DeathTranslations.Unknown);

    public static bool Apply(
        Player attacker,
        Player target,
        DeathTranslation deathReason)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (!target.IsAlive || target.MaxHealth <= 0f)
            return false;

        return ApplyFixedDamage.Apply(
            attacker,
            target,
            target.MaxHealth,
            deathReason);
    }
}
