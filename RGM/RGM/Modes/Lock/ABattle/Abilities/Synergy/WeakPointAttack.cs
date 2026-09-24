using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerStatsSystem;
using RGM.Modes.Abilities.Unique.Scp106.Mythic;
using RGM.Modes;

namespace RGM.Modes.Abilities.Synergy;

[RequiresAbility(AbilityType.EPIC_SHARPEYES, AbilityType.EPIC_SHARPEYES, AbilityType.RARE_BULLSEYE)]
[Ability("약점 공격", """
                  <샤프 아이즈 x2, 불스아이> 뛰어난 통찰력으로 적군의 약점을 간파합니다.
                  자신의 모든 공격에 『파열』 효과가 적용됩니다.
                  """,
    AbilityCategory.Synergy, AbilityType.SYNERGY_WEAKPOINTATTACK)]
public class WeakPointAttack : Ability
{
    public static bool ShouldIgnoreDefenses(Player attacker) =>
        attacker != null &&
        (Reminiscence.IsIgnoringDefenses(attacker) ||
         (ABattle.Instance != null && attacker.HasAbility(AbilityType.SYNERGY_WEAKPOINTATTACK)));

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

        // ApplyFixedDamage already compensates these modifiers before the
        // native damage handler runs.
        if (ApplyFixedDamage.IsApplying)
            return;

        HitboxType hitbox = ev.DamageHandler.Base is StandardDamageHandler standard
            ? standard.Hitbox
            : HitboxType.Body;

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
