using System.Reflection;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using PlayerStatsSystem;
using UnityEngine;

namespace RGM.Modes.Abilities.Legend;

[Ability("현을 푸는 제 0법칙",
    """
    특수한 시야로 적의 생명줄을 포착합니다.
    공격 시 10% 확률로 618.03의 피해를 입힙니다. 해당 피해는 『관통』 효과가 적용됩니다.
    """, 
    AbilityCategory.Legend,
    AbilityType.LEGEND_ZERORULE)]

public class ZeroRule : Ability
{
    const float FixedDamage = 618.03f;

    static HurtingEventArgs _ignoreDefensesEvent;

    static readonly FieldInfo PenetrationField = typeof(FirearmDamageHandler).GetField(
        nameof(FirearmDamageHandler._penetration),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    public static bool ShouldIgnoreDefenses(HurtingEventArgs ev) =>
        ev != null && ReferenceEquals(_ignoreDefensesEvent, ev);
    
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
        if (ev.Attacker == null ||
            ev.Attacker != Owner ||
            !HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub))
            return;

        if (Random.Range(1, 101) > 10)
            return;

        _ignoreDefensesEvent = ev;

        HitboxType hitbox = ev.DamageHandler.Base is StandardDamageHandler standard
            ? standard.Hitbox
            : HitboxType.Body;

        // Hurting 이후 ApplyDamage → ProcessDamage가 다시 돌므로,
        // 고정 피해를 넣은 뒤 이후 적용될 감소를 미리 상쇄한다.
        ev.DamageHandler.Damage = FixedDamage;

        if (ev.DamageHandler.Base is FirearmDamageHandler firearm)
        {
            // SCP-173 Reinforced Concrete / 방어구 감소 무시 (penetration 100%)
            // _penetration은 init-only라 reflection으로 설정
            PenetrationField?.SetValue(firearm, 1f);

            // ProcessDamage의 히트박스 배율 상쇄
            if (firearm._useHumanHitboxes &&
                FirearmDamageHandler.HitboxDamageMultipliers.TryGetValue(hitbox, out float hitboxMultiplier) &&
                hitboxMultiplier > 0f)
                ev.DamageHandler.Damage /= hitboxMultiplier;
        }

        // ProcessDamage의 DamageReduction / BodyshotReduction 상쇄
        IgnoreDamageModifier(ev, EffectType.DamageReduction, hitbox);
        IgnoreDamageModifier(ev, EffectType.BodyshotReduction, hitbox);
    }

    private static void IgnoreDamageModifier(HurtingEventArgs ev, EffectType effectType, HitboxType hitbox)
    {
        if (!ev.Player.TryGetEffect(effectType, out StatusEffectBase effect) ||
            effect is not IDamageModifierEffect { DamageModifierActive: true } modifier)
            return;

        float damageModifier = modifier.GetDamageModifier(ev.DamageHandler.Damage, ev.DamageHandler.Base, hitbox);
        if (damageModifier is > 0f and < 1f)
            ev.DamageHandler.Damage /= damageModifier;
    }
}
