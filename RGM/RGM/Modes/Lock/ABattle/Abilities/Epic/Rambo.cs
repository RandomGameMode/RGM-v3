using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Epic;

[Ability("람보", "탄약이 무제한인 로지카를 받습니다. 단, 최종 데미지가 15% 감소되어 지급됩니다.", AbilityCategory.Epic, AbilityType.EPIC_RAMBO)]
public class Rambo : Ability
{
    private ushort _infinityGunSerial;

    public override void OnEnabled()
    {
        Item ig = Owner.AddItem(ItemType.GunLogicer);

        _infinityGunSerial = ig.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _infinityGunSerial)
            return;

        ev.Player.AddHint("람보", $"<b><color={ABattle.RatingColor["영웅"]}>람보</color></b> 능력이 있는 Logicer입니다");
    }

    private void OnShooting(ShootingEventArgs ev)
    {
        if (ev.Item.Serial == _infinityGunSerial) {
            ev.Player.CurrentItem.As<Firearm>().MagazineAmmo = 101;
        }
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker == null || ev.Player == ev.Attacker) return;
        if (ev.Attacker.CurrentItem != null && _infinityGunSerial == ev.Attacker.CurrentItem.Serial) {
            ev.DamageHandler.Damage *= 0.85f;
        }
    }
}