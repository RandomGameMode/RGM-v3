using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups.Projectiles;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Mythic;

[Ability("워 머신", """
                 발사할 때마다 최종 데미지가 15% 감소된 고폭 수류탄을 투하하는, 탄약이 무제한인 Com15를 얻습니다.
                 능력 획득 시 자신이 받는 폭발 데미지가 98% 감소합니다.
                 """, AbilityCategory.Mythic, AbilityType.MYTHIC_BOMBGUN)]
public class BombGun : Ability
{
    private const float OwnerExplosionDamageMultiplier = 0.02f;
    private const float WarMachineGrenadeDamageMultiplier = 0.85f;

    private ushort _itemSerial;
    private readonly Dictionary<ExplosionGrenadeProjectile, Vector3> _bombGunGrenadePositions = new();

    public override void OnEnabled()
    {
        Item item = Owner.AddItem(ItemType.GunCOM15);

        _itemSerial = item.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        _bombGunGrenadePositions.Clear();
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _itemSerial)
            return;
        
        ev.Player.AddHint("워 머신", $"<b><color={ABattle.RatingColor["신화"]}>워 머신</color></b> 능력이 있는 <b>리볼버</b>입니다!");
    }

    private void OnShooting(ShootingEventArgs ev)
    {
        if (ev.Item.Serial != _itemSerial) return;
        Throwable throwable = ev.Player.ThrowGrenade(ProjectileType.FragGrenade);

        Timing.RunCoroutine(ExplodeOnImpact(throwable));

        Timing.CallDelayed(0.1f, () =>
        {
            ev.Item.As<Firearm>().MagazineAmmo = 18;
        });
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.DamageHandler.Type != DamageType.Explosion) return;

        if (ABattle.Instance.HasAbility(ev.Player, AbilityType.MYTHIC_BOMBGUN))
            ev.DamageHandler.Damage *= OwnerExplosionDamageMultiplier;

        if (ev.Attacker == Owner &&
            _bombGunGrenadePositions.Values.Any(position => Vector3.Distance(position, ev.Player.Position) <= 10f))
            ev.DamageHandler.Damage *= WarMachineGrenadeDamageMultiplier;
    }

    private IEnumerator<float> ExplodeOnImpact(Throwable throwable)
    {
        yield return Timing.WaitForSeconds(0.3f);

        if (throwable.Projectile is not ExplosionGrenadeProjectile grenade)
            yield break;

        if (!TryGetPosition(grenade, out Vector3 position))
            yield break;

        _bombGunGrenadePositions[grenade] = position;

        while (!grenade.IsAlreadyDetonated)
        {
            if (!TryGetPosition(grenade, out position))
                break;

            _bombGunGrenadePositions[grenade] = position;

            if (Physics.OverlapSphere(position, 0.3f).Count() > 4)
            {
                grenade.Base.Network_syncTargetTime = 0.1f;
            }

            yield return Timing.WaitForOneFrame;
        }

        Timing.CallDelayed(0.5f, () => _bombGunGrenadePositions.Remove(grenade));
    }

    private static bool TryGetPosition(ExplosionGrenadeProjectile grenade, out Vector3 position)
    {
        try
        {
            position = grenade.Position;
            return true;
        }
        catch (MissingReferenceException)
        {
            position = default;
            return false;
        }
        catch (System.NullReferenceException)
        {
            position = default;
            return false;
        }
    }
}
