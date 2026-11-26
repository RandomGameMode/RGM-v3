using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using System.Collections.Generic;

namespace RGM.Modes.Lock.TFT.Safe.All;

[TFTAbility("단련 · 입문", "피해 증폭을 얻습니다. (+15%)", TFTAbilityLevel.Safe, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.AddPower1, "💪")]
public class AddPower1 : TFTAbility
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
        if (ev.Attacker != Owner)
            return;

        ev.DamageHandler.Damage += ev.DamageHandler.Damage * 0.15f;
    }
}
