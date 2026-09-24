using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;

namespace RGM.Modes.Abilities.Unique.Scp0492.Epic;

[Ability("작은 역병 의사",
    """
    자신의 공격에 심장 마비 효과가 적용됩니다.
    이미 심장 마비 효과가 있는 대상을 공격 시, 『죽음에 이르는 공격』을 가합니다.
    """,
    AbilityCategory.Epic,
    AbilityType.EPIC_SCP0492_MINIPLAGUEDOCTOR,
    RoleAbility.Scp0492)]

public class MiniPlagueDoctor : Ability
{
    private const float CardiacArrestDuration = 60f;

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
        if (ev.Attacker != Owner ||
            ev.Player == Owner ||
            ev.DamageHandler.Type != DamageType.Scp0492)
            return;

        if (ev.Player.TryGetEffect<CardiacArrest>(out var cardiacArrest) &&
            cardiacArrest.IsEnabled)
        {
            ApplyLethalDamage.Apply(Owner, ev.Player);
            return;
        }

        ev.Player.EnableEffect(
            EffectType.CardiacArrest,
            1,
            CardiacArrestDuration);

        if (ev.Player.TryGetEffect(out cardiacArrest))
            cardiacArrest.SetAttacker(Owner.ReferenceHub);
    }
}