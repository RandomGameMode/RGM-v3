using System.Linq;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp173.Legend;

[Ability("파편", 
    "실드가 파괴될 시, 주변에 파편을 뿌려 범위 내의 적에게 『죽음에 이르는 공격』 을 가합니다.", 
    AbilityCategory.Legend, 
    AbilityType.LEGEND_SCP173_DEBRIS,
    RoleAbility.Scp173)]

public class Debris : Ability
{
    private const float Radius = 40f;
    private const float CooldownDuration = 30f;

    private bool _isCoolingDown;
    private int _cooldownVersion;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;

        _isCoolingDown = false;
        _cooldownVersion++;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player != Owner ||
            !ev.IsAllowed ||
            _isCoolingDown ||
            Owner.HumeShield <= 0f ||
            ev.DamageHandler.Damage < Owner.HumeShield ||
            ApplyFixedDamage.IsApplying)
            return;

        _isCoolingDown = true;
        int cooldownVersion = ++_cooldownVersion;

        foreach (var target in PlayerManager.List.Where(player =>
                     player.IsAlive &&
                     HitboxIdentity.IsEnemy(Owner.ReferenceHub, player.ReferenceHub) &&
                     Vector3.Distance(player.Position, Owner.Position) <= Radius))
        {
            ApplyLethalDamage.Apply(Owner, target);
        }

        Timing.CallDelayed(CooldownDuration, () =>
        {
            if (_cooldownVersion == cooldownVersion)
                _isCoolingDown = false;
        });
    }
}