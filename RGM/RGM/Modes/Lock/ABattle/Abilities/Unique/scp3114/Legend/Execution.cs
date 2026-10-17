using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using RGM.Features;

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
        if (ev.Attacker == null || ev.Attacker != Owner)
            return;

        if (ev.DamageHandler.Type == DamageType.Strangled)
        {
            ev.IsAllowed = false;
            ApplyInstantKill.Apply(ev.Attacker, ev.Player);
        }
    }
}
