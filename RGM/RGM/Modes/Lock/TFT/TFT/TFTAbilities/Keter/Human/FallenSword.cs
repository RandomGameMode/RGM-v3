using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Keter.Human;

[TFTAbility("몰락한 왕의 검", "SCP 대상 1.2%, 인간 대상 6%만큼 최대 HP 비례 피해를 입힙니다.", 
    TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.FallenSword, "👿")]
public class FallenSword : TFTAbility
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

        var ratio = ev.Player.IsScpRole() ? 0.012f : 0.06f;
        ev.DamageHandler.Damage += ev.Player.MaxHealth * ratio;
    }
}