using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using RGM.Features;

namespace RGM.Modes.Abilities.Unique.Scp173.Epic;

[Ability("확인 사살", 
    "SCP-173의 기본 공격에 『사망』 효과가 적용됩니다.", 
    AbilityCategory.Legend, AbilityType.LEGEND_SCP173_DEBRIS, RoleAbility.Scp173)]

public class ComfirmKill : Ability
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
        if (ev.Attacker != Owner ||
            ev.Player == Owner ||
            ev.DamageHandler.Type != DamageType.Scp173)
            return;

        ev.IsAllowed = false;
        ApplyInstantKill.Apply(Owner, ev.Player);
    }
}