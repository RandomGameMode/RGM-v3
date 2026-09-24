using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;

using static RGM.Variables.Variable;

namespace RGM.Modes.Abilities.Unique.Scp0492.Mythic;

[Ability("ONE PUNCH MAN",
    """
    자신의 일반 공격에 『사망』 효과가 적용됩니다.
    추가로, [전설] 스피드왜건, [영웅] 거북 도사, [전용 희귀] 급식, [전용 희귀] 보호막 능력을 획득합니다.
    """,
    AbilityCategory.Mythic,
    AbilityType.MYTHIC_SCP0492_ONEPUNCH,
    RoleAbility.Scp0492)]

public class OnePunch : Ability
{
    public override void OnEnabled()
    {
        Timing.CallDelayed(0.1f, () =>
        {
            Owner.AddAbility(AbilityType.LEGEND_SPEEDWAGON);
            Owner.AddAbility(AbilityType.EPIC_TURTLE);
            Owner.AddAbility(AbilityType.RARE_SCP0492_SHIELD);
            Owner.AddAbility(AbilityType.RARE_SCP0492_MEALS);
        });
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

        if (ApplyInstantKill.Apply(Owner, ev.Player))
            Owner.ShowHitMarker(1.5f);
    }
}