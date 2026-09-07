using System.Collections.Generic;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Epic;

[Ability("몰락한 왕의 검", """
                     공격 시 대상 최대 HP의 1.0%만큼 추가 데미지를 입힙니다. 
                     대상이 인간진영인 경우 6.9%로 적용됩니다.
                     """,
    AbilityCategory.Epic, AbilityType.EPIC_FALLENKINGSSWORD)]
public class FallenKingsSword : Ability
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
            ev.Player == ev.Attacker) return;
        if (!HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub)) return;
        if (ABattle.Instance.GetAbility(Owner, AbilityType.EPIC_FALLENKINGSSWORD) != this) return;

        float ratio = ev.Player.IsScpRole() ? 0.01f : 0.069f;
        ev.DamageHandler.Damage += ev.Player.MaxHealth * ratio * Owner.AbilityCount(AbilityType.EPIC_FALLENKINGSSWORD);
    }
}
