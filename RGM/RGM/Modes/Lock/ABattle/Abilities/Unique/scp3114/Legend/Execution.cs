using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using PlayerStatsSystem;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp3114.Legend;

[Ability("참수", "자신의 교살 공격에 『사망』 효과가 적용됩니다.", 
    AbilityCategory.Legend, AbilityType.NORMAL_SCP3114_SKILLEDASSASSIN, RoleAbility.Scp3114)]
public class Execution : Ability
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
        if (ev.Attacker == null || ev.Attacker != Owner || ev.DamageHandler.Type != DamageType.Strangled)
            return;

        ev.IsAllowed = false;
        if (ApplyLethalDamage.Apply(ev.Attacker, ev.Player, DeathTranslations.Crushed))
            ev.Attacker.ShowHitMarker(1.5f);

        ev.Player.Role.Set(RoleTypeId.Spectator);
    }
}
