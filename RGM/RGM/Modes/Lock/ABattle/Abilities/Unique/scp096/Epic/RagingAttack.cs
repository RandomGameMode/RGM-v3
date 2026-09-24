using Exiled.API.Enums;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;

namespace RGM.Modes.Abilities.Unique.Scp096.Epic;

[Ability("분노의 일격", "SCP-096의 기본 공격에 추가 데미지 240이 적용됩니다.",
    AbilityCategory.Epic, AbilityType.EPIC_SCP096_RAGINGATTACK, RoleAbility.Scp096)]
public class RagingAttack : Ability
{
    private const float DamageIncrease = 240f;
    
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
        if (Owner.Role is not Scp096Role scp096) return;
        if (ev.Attacker == null || ev.Attacker.ReferenceHub != scp096.Owner.ReferenceHub) return;
        if (ev.DamageHandler.Type != DamageType.Scp096) return;

        ev.DamageHandler.Damage += DamageIncrease;
    }
}
