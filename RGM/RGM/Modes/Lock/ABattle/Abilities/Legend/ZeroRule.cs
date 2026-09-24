using Exiled.Events.EventArgs.Player;
using Random = UnityEngine.Random;

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
    private const float FixedDamage = 618.03f;
    
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
            !HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub) ||
            ApplyFixedDamage.IsApplying)
            return;

        if (Random.Range(1, 101) > 10)
            return;

        ev.IsAllowed = false;
        if (ApplyFixedDamage.Apply(Owner, ev.Player, FixedDamage))
            Owner.ShowHitMarker(1.25f);
    }
}
