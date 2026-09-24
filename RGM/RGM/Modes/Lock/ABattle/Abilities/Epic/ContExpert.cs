using System.Collections.Generic;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;

namespace RGM.Modes.Abilities.Epic;

[Ability("격리 전문가", "SCP 개체에 가하는 데미지가 110%p 증가합니다(999, 035는 제외).",
    AbilityCategory.Epic, AbilityType.EPIC_CONTEXPERT)]
public class ContExpert : Ability
{
    private readonly List<RoleTypeId> _scpRoles =
    [
        RoleTypeId.Scp049,
        RoleTypeId.Scp096,
        RoleTypeId.Scp106,
        RoleTypeId.Scp173,
        RoleTypeId.Scp939,
        RoleTypeId.Scp3114,
        RoleTypeId.Scp0492
    ];  

    public override void OnEnabled() => Exiled.Events.Handlers.Player.Hurting += OnHurting;

    public override void OnDisabled() => Exiled.Events.Handlers.Player.Hurting -= OnHurting;

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker != Owner || _scpRoles.Contains(ev.Attacker.Role)) return;
        if (!_scpRoles.Contains(ev.Player.Role)) return;
        if (ABattle.Instance.GetAbility(Owner, AbilityType.EPIC_CONTEXPERT) != this) return;
        
        ev.DamageHandler.Damage *= 1 + 1.1f * Owner.AbilityCount(AbilityType.EPIC_CONTEXPERT);
    }
}