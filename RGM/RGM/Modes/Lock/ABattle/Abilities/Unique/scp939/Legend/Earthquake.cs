using System.Linq;
using Exiled.API.Enums;
using PlayerRoles.PlayableScps.Scp939;
using RGM.API.Features;
using RGM.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp939.Legend;

[Ability("지진", $"""
               SCP-939의 런지 착지 지점에 지진을 일으킵니다.
               지진 공격에 노출된 모든 적은 『불안정』 효과를 받습니다.
               """,
    AbilityCategory.Legend, AbilityType.LEGEND_SCP939_EARTHQUAKE, RoleAbility.Scp939)]

public class Earthquake : Ability
{
    private const float Radius = 12f;
    private const float DamageRatio = 0.7f;
    private const float DefaultUnstableDuration = 3f;

    private Scp939LungeAbility _lungeAbility;

    public override void OnEnabled()
    {
        if (Owner.Role is not Exiled.API.Features.Roles.Scp939Role scp939
            || scp939.LungeAbility == null) return;

        _lungeAbility = scp939.LungeAbility;
        _lungeAbility.OnStateChanged += OnLungeStateChanged;
    }

    public override void OnDisabled()
    {
        if (_lungeAbility != null)
            _lungeAbility.OnStateChanged -= OnLungeStateChanged;

        _lungeAbility = null;
    }

    private void OnLungeStateChanged(Scp939LungeState state)
    {
        if (state is not (Scp939LungeState.LandHit or Scp939LungeState.LandRegular or Scp939LungeState.LandHarsh))
            return;

        foreach (var target in PlayerManager.List.Where(player =>
                     player.IsAlive &&
                     HitboxIdentity.IsEnemy(Owner.ReferenceHub, player.ReferenceHub) &&
                     Vector3.Distance(player.Position, Owner.Position) <= Radius))
        {
            ApplyUnstable(target);
            ApplyFixedDamage.Apply(Owner, target, target.MaxHealth * DamageRatio);
        }
    }

    private static void ApplyUnstable(Exiled.API.Features.Player target)
    {
        target.AddEffect(EffectType.Ensnared, 1, DefaultUnstableDuration);
        target.AddEffect(EffectType.SinkHole, 1, DefaultUnstableDuration);
        target.AddEffect(EffectType.Slowness, 60, DefaultUnstableDuration + 2f);
        target.AddEffect(EffectType.AmnesiaItems, 1, DefaultUnstableDuration + 7f);
        target.AddEffect(EffectType.AmnesiaVision, 1, DefaultUnstableDuration + 7f);

        if (target.IsInventoryEmpty)
            return;

        var item = target.CurrentItem;
        if (item == null)
        {
            var items = target.Items.ToList();
            item = items[Random.Range(0, items.Count)];
        }

        target.DropItem(item);
    }
}
