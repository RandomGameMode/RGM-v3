using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Normal;

[Ability("단련", "공격력이 16%p 추가됩니다. 자신이 SCP 진영일 경우 효율이 40% 감소합니다.", 
    AbilityCategory.Normal, AbilityType.NORMAL_TRAINING)]

public class Training : Ability
{
    private const float DamageMultiplier = 0.16f;
    private const float ScpRoleMultiplierReduction = 0.6f;
    
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker != Owner)
            return;

        if (ABattle.Instance.GetAbility(Owner, AbilityType.NORMAL_TRAINING) != this)
            return;

        ev.DamageHandler.Damage *= 1.0f +
                                   (Owner.IsScpRole()
                                       ? DamageMultiplier * ScpRoleMultiplierReduction
                                       : DamageMultiplier) * Owner.AbilityCount(AbilityType.NORMAL_TRAINING);
    }
}
