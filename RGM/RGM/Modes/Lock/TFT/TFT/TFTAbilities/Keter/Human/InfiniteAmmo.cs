using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;

namespace DAONTFT.Core.TFT.Keter.Human;

[TFTAbility("무한 탄환", "탄약이 무제한이 되며, 탄약을 소모하는 무기의 최종 데미지가 30% 증가합니다.",
    TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.InfiniteAmmo, "❓")]
public class InfiniteAmmo : TFTAbility
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
