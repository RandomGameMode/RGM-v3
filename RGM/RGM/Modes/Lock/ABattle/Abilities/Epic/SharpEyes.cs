using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;

namespace RGM.Modes.Abilities.Epic;

[Ability("샤프 아이즈", """
                   크리티컬 확률이 50% 증가합니다.
                   크리티컬 발동 시 50%p의 추가 피해를 입히며, 추가 획득 시 크리티컬 데미지가 100%p씩 증가합니다.
                   """, AbilityCategory.Epic, AbilityType.EPIC_SHARPEYES)]
public class SharpEyes : Ability
{
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

        int count = Owner.AbilityCount(AbilityType.EPIC_SHARPEYES);
        int chance = Mathf.Min(100, 50 * count);

        if (Random.Range(1, 101) > chance)
            return;

        ev.DamageHandler.Damage *= 0.5f + 1f * count;

        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            ev.Attacker.ShowHitMarker(1.5f);
        });
    }
}
