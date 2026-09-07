using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Item;

namespace RGM.Modes.Abilities.Legend;

[Ability("무한 탄환", """
                  더 이상 탄약을 소모하지 않습니다.
                  탄약을 소모하는 무기로 공격 시 최종 데미지가 30% 증가합니다.
                  """,
    AbilityCategory.Legend, AbilityType.LEGEND_UNLIMITEDAMMO)]

public class UnlimitedAmmo : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.ChangingMicroHIDState += OnChangingMicroHIDState;
        Exiled.Events.Handlers.Player.UsingMicroHIDEnergy += OnUsingMicroHIDEnergy;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        
        Exiled.Events.Handlers.Item.ChargingJailbird += OnChargingJailbird;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.ChangingMicroHIDState -= OnChangingMicroHIDState;
        Exiled.Events.Handlers.Player.UsingMicroHIDEnergy -= OnUsingMicroHIDEnergy;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
        
        Exiled.Events.Handlers.Item.ChargingJailbird -= OnChargingJailbird;
    }

    private void OnShooting(ShootingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.Player.CurrentItem.As<Firearm>().MagazineAmmo = 101;
    }

    private void OnChangingMicroHIDState(ChangingMicroHIDStateEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.MicroHID.Energy += 100;
    }

    private void OnUsingMicroHIDEnergy(UsingMicroHIDEnergyEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.MicroHID.Energy += 100;
    }
    
    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player != Owner) return;
        
        if (ev.Attacker == null || 
            ev.Attacker != Owner || 
            ev.Player == ev.Attacker) return;
        if (!HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub)) return;
        if (ABattle.Instance.GetAbility(Owner, AbilityType.LEGEND_UNLIMITEDAMMO) != this) return;
        if (ev.Player.CurrentItem.Type is ItemType.SCP1509 or ItemType.Jailbird) return;

        ev.DamageHandler.Damage *= 1.3f;
    }
    
    private void OnChargingJailbird(ChargingJailbirdEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.Jailbird.TotalCharges = 0;
        ev.Jailbird.TotalDamageDealt = 0;
    }
}