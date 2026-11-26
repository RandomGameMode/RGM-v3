using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Keter.All;

[TFTAbility("단련 · 통달", "피해 증폭이 60% 증가되고, 공격 시 일정 확률로 60%가 추가 증가합니다.", 
    TFTAbilityLevel.Keter, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.AddPower3, "💪")]
public class AddPower3 : TFTAbility
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
            !HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub))
            return;

        var chance = Random.Range(1, 101);
        
        ev.DamageHandler.Damage *= chance < 50 ? 1.6f : Mathf.Pow(1.6f , 2);
        
        if (chance < 50) return;
        ev.Attacker.ShowHitMarker(1.5f);
    }
}
