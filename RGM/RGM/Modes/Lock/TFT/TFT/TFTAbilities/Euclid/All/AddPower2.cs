using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using System.Collections.Generic;

namespace RGM.Modes.Lock.TFT.Euclid.All;

[TFTAbility("단련 · 숙련", "피해 증폭을 얻습니다. (+30%)", TFTAbilityLevel.Euclid, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.AddPower2, "💪")]
public class AddPower2 : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    public void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker != Owner )
            return;

        ev.DamageHandler.Damage += ev.DamageHandler.Damage * 0.3f;
    }
}
