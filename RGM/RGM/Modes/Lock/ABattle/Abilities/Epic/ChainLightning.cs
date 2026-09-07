using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Epic;

[Ability("체인 라이트닝", """
                  적 공격 시, 피해량의 일부를 대상 기준 주변 10m 적에게 전이합니다.
                  전이 시 마다 최종 데미지가 15%p씩 감소하고 최대 75%p까지 적용되며, 최대 8명까지만 전이 가능합니다.
                  """,
    AbilityCategory.Epic, AbilityType.EPIC_CHAINLIGHTNING)]

public class ChainLightning : Ability
{
    private const float ChainRange = 10f;
    private const int MaxChainTargets = 8;
    private const float DamageReductionPerChain = 0.15f;
    private const float MinimumDamageMultiplier = 0.25f;

    private bool _isApplyingChainDamage;

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
        if (_isApplyingChainDamage ||
            !ev.IsAllowed ||
            ev.Attacker != Owner ||
            ev.Player == Owner ||
            !HitboxIdentity.IsEnemy(Owner.ReferenceHub, ev.Player.ReferenceHub) ||
            ABattle.Instance.GetAbility(Owner, AbilityType.EPIC_CHAINLIGHTNING) != this)
        {
            return;
        }

        float originalDamage = ev.DamageHandler.Damage;
        if (originalDamage <= 0f)
            return;

        var affectedPlayers = new HashSet<Player> { ev.Player };
        Player currentTarget = ev.Player;

        for (int chainIndex = 1; chainIndex <= MaxChainTargets; chainIndex++)
        {
            Player nextTarget = FindNearestChainTarget(currentTarget, affectedPlayers);
            if (nextTarget == null)
                return;

            affectedPlayers.Add(nextTarget);

            float multiplier = Mathf.Max(
                MinimumDamageMultiplier,
                1f - DamageReductionPerChain * chainIndex);

            _isApplyingChainDamage = true;
            try
            {
                nextTarget.Hurt(Owner, originalDamage * multiplier, ev.DamageHandler.Type);
            }
            finally
            {
                _isApplyingChainDamage = false;
            }

            currentTarget = nextTarget;
        }
    }

    private Player FindNearestChainTarget(Player origin, HashSet<Player> affectedPlayers)
    {
        return PlayerManager.List
            .Where(player =>
                player.IsAlive &&
                !affectedPlayers.Contains(player) &&
                HitboxIdentity.IsEnemy(Owner.ReferenceHub, player.ReferenceHub) &&
                Vector3.Distance(origin.Position, player.Position) <= ChainRange)
            .OrderBy(player => Vector3.Distance(origin.Position, player.Position))
            .FirstOrDefault();
    }
}