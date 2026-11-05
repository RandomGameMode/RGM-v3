using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;
using System.Linq;
using UnityEngine;

namespace RGM.Modes.Abilities.Mythic;

[Ability("차원 강탈자", """
                   적 타격 시 30% 확률로 상대의 능력을 1개 강탈합니다.
                   적을 처치할 시, 상대의 능력을 모두 가져옵니다! (반사경 효과 미적용)
                   """,
    AbilityCategory.Mythic, AbilityType.MYTHIC_DIMENSIONTHIEF, isUnique: true)]

public class DimensionThief : Ability
{
    private readonly List<AbilityType> _exceptAbilities =
    [
        AbilityType.NORMAL_REROLL, AbilityType.NORMAL_EXCHANGE, AbilityType.NORMAL_SUSPICIOUSSTEW,
        AbilityType.RARE_DND, AbilityType.RARE_TELEPORTATION, AbilityType.RARE_UPGRADE,
        AbilityType.RARE_CONTRACT, AbilityType.EPIC_HALLOWEEN_MARSHMELLOW, AbilityType.LEGEND_HALLOWEEN_CANDYPOWER
    ];

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (!ev.IsAllowed || ev.Attacker != Owner || ev.Player == null ||
            !HitboxIdentity.IsEnemy(Owner.ReferenceHub, ev.Player.ReferenceHub) ||
            Random.value >= 0.3f)
        {
            return;
        }

        if (TryStealAbility(ev.Player))
            ev.Player.AddHint("차원 강탈자", "능력을 강탈당했습니다!");
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (!ev.IsAllowed || ev.Attacker != Owner || ev.Player == null ||
            !HitboxIdentity.IsEnemy(Owner.ReferenceHub, ev.Player.ReferenceHub))
        {
            return;
        }

        if (!ABattle.Instance.PlayerAbilities.TryGetValue(ev.Player, out List<Ability> abilities))
            return;

        int stolenCount = 0;
        foreach (Ability ability in abilities.ToList())
        {
            if (TryStealAbility(ev.Player, ability))
                stolenCount++;
        }

        if (stolenCount > 0)
            ev.Player.AddHint("차원 강탈자", "능력을 모두 강탈당했습니다!");
    }

    private bool TryStealAbility(Player victim, Ability target = null)
    {
        if (!ABattle.Instance.PlayerAbilities.TryGetValue(victim, out List<Ability> abilities))
            return false;

        if (target == null && abilities.Count == 0)
            return false;

        target ??= abilities[Random.Range(0, abilities.Count)];
        if (target == null || !abilities.Contains(target))
            return false;

        AbilityType type = target.Data.AbilityType;

        // 예외 능력은 대상에게서 제거만 하고, 자신에게는 부여하지 않는다.
        if (_exceptAbilities.Contains(type))
        {
            victim.RemoveAbility(target);
            return true;
        }

        // 강탈로 얻는 능력은 반사경 효과를 적용하지 않는다.
        if (!ABattle.Instance.AddAbility(Owner, type, allowReflector: false))
            return false;

        victim.RemoveAbility(target);
        return true;
    }
}
