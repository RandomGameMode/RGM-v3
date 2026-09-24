using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Scp049;
using MEC;
using PlayerStatsSystem;

namespace RGM.Modes.Abilities.Unique.Scp049.Epic;

[Ability("의료 사고",
    """
    SCP-049의 F 스킬 사용 시, 대상에게 『죽음에 이르는 공격』을 가합니다.
    <color=#DF0101>[전용 신화]</color> PANDEMIC 능력을 보유 중인 경우, <color=#BF40BF>[전용 영웅]</color> 의료 사고 능력을 획득할 수 없습니다.
    """,
    AbilityCategory.Epic,
    AbilityType.EPIC_SCP049_MEDICALACCIDENT,
    RoleAbility.Scp049)]

public class MedicalAccident : Ability
{
    private const float CardiacArrestDuration = 60f;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp049.ActivatingSense += OnActivatingSense;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp049.ActivatingSense -= OnActivatingSense;
    }

    private void OnActivatingSense(ActivatingSenseEventArgs ev)
    {
        if (!ev.IsAllowed || ev.Player != Owner || ev.Target is null)
            return;

        ev.Target.EnableEffect(EffectType.CardiacArrest, 1, CardiacArrestDuration);

        if (ev.Target.TryGetEffect<CardiacArrest>(out var cardiacArrest))
            cardiacArrest.SetAttacker(Owner.ReferenceHub);

        Timing.CallDelayed(0.1f, () =>
        {
            if (!ev.Target.IsAlive)
                return;

            ApplyLethalDamage.Apply(Owner, ev.Target);
        });
    }
}