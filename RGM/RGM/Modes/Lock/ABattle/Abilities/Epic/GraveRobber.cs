using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;

namespace RGM.Modes.Abilities.Epic;

[Ability("도굴꾼", "적 처치 시 70% 확률로, 또는 사망한 아군에게서 100% 확률로 능력 중 하나를 랜덤으로 획득합니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_GRAVEROBBER)]
public class GraveRobber : Ability
{
    private readonly List<AbilityType> _exceptAbilities =
    [
        AbilityType.NORMAL_REROLL, AbilityType.NORMAL_EXCHANGE, AbilityType.NORMAL_SUSPICIOUSSTEW,
        AbilityType.RARE_DND, AbilityType.RARE_TELEPORTATION, AbilityType.RARE_UPGRADE,
        AbilityType.RARE_CONTRACT, AbilityType.EPIC_HALLOWEEN_MARSHMELLOW, AbilityType.LEGEND_HALLOWEEN_CANDYPOWER
    ];

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (!ev.IsAllowed || ev.Player == Owner)
            return;

        var isAlly = ev.Player.LeadingTeam == Owner.LeadingTeam;
        var killedEnemy = ev.Attacker == Owner &&
                          HitboxIdentity.IsEnemy(Owner.ReferenceHub, ev.Player.ReferenceHub);

        if (!isAlly && (!killedEnemy || Random.value >= 0.7f))
            return;

        List<AbilityType> abilityTypes =
        [
            .. ev.Player.GetAbilities().Where(x => x.Data.RoleAbility == RoleAbility.None &&
                                                   !_exceptAbilities.Contains(x.Data.AbilityType))
                .Select(x => x.Data.AbilityType)
        ];
        if (abilityTypes.Count == 0)
            return;

        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            if (!ev.Player.IsDead) return;
            Owner.AddAbility(abilityTypes.GetRandomValue());
        });
    }
}
