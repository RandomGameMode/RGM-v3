using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerStatsSystem;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Mythic;

[Ability("솔져: 76",
    """
    적군 주변 35m 이내에 발사된 총알은 모두 맞은 판정으로 처리하는 E11SR를 획득합니다. SCP 진영이 사용 시 최종 데미지가 35% 감소합니다.
    30초마다 50발의 5.56x45mm 탄이 장전되며 최대 150발까지 충전 가능합니다.
    추가로, 적의 움직임을 억제합니다.
    """, 
    AbilityCategory.Mythic, 
    AbilityType.MYTHIC_SOLDIER76)]

public class Soldier76 : Ability
{
    private static CoroutineHandle _ammoCoroutine;
    
    private ushort _serial;
    private Item _item;
    
    public  override void OnEnabled()
    {
        _item = Owner.AddItem(ItemType.GunE11SR);
        _serial = _item.Serial;
        
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;

        if (!Timing.IsRunning(_ammoCoroutine))
            _ammoCoroutine = Timing.RunCoroutine(AmmoGiver());
    }

    private IEnumerator<float> AmmoGiver()
    {
        var firearm = _item.As<Firearm>();
        while (true)
        {
            yield return Timing.WaitForSeconds(30f);
            if (firearm.MagazineAmmo >= 150) continue;
            firearm.MagazineAmmo += 50;
        }
    }

    private void OnChangedItem(ChangedItemEventArgs e)
    {
        if (e.Player == null || e.Player.IsDead) return;
        
        if (e.Player.CurrentItem?.Serial == _serial)
            e.Player.AddEffect(EffectType.Scp1344, 1);
        else 
            e.Player.RemoveEffect(EffectType.Scp1344, 1);
    }
    
    private void OnShooting(ShootingEventArgs ev)
    {
        if (ev.Item == null || ev.Firearm.Serial != _serial) return;
        
        if (!ev.Player.TryGetNearestVisiblePlayer(out var player, out _, 35f, 30f, [.. PlayerManager.List.Where(x => 
                x == ev.Player || 
                x.IsDead || 
                !HitboxIdentity.IsEnemy(ev.Player.ReferenceHub, x.ReferenceHub))]))
        {
            ev.IsAllowed = false;
            return;
        }
        var multiplier = player.IsScpRole() ? 0.65f : 1f;
        var check = Tools.TryGetLookPlayers(ev.Player, 35f, out var victims, out var hit);
        if (!check ||
            !victims.Contains(player) ||
            (hit != null && 
             hit.Value.collider.gameObject != player.GameObject))
        {
            player.Hurt(new FirearmDamageHandler(ev.Firearm.Base, ev.Firearm.Damage * multiplier,
                ev.Firearm.Penetration));
            ev.Player.ShowHitMarker();
        }
    }
}