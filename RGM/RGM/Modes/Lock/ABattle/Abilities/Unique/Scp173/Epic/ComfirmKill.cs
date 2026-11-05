using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp173.Epic;

[Ability("확인 사살", 
    "SCP-173의 기본 공격에 『사망』 효과가 적용됩니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_SCP173_CONFIRMKILL, RoleAbility.Scp173)]

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
        // ApplyInstantKill's own damage raises Hurting again; ignore it to avoid infinite recursion.
        if (ApplyInstantKill.IsApplying ||
            ev.Attacker != Owner ||
            ev.Player == Owner ||
            !ev.IsInstantKill)
            return;

        ev.IsAllowed = false;
        ev.Player.Kill(ev.DamageHandler);
    }
}