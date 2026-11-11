using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using PlayerStatsSystem;
using RGM.API.Features;
using RGM.Modes.Abilities.Rare;

namespace RGM.Modes.Abilities.Mythic;

[Ability("솔져: 76",
    """
    적군 주변 40m 이내에 발사된 총알은 모두 맞은 판정으로 처리하는 E11SR를 획득합니다. SCP 진영이 사용 시 최종 데미지가 35% 감소합니다.
    해당 무기로 적 공격시, 자신의 현재 헤드샷 데미지 배율 증가량 만큼 최종 데미지가 증가하며, 적 최대 HP의 5% 만큼 추가 피해를 입힙니다.
    추가로, [전설] 무한 탄환 능력을 획득하며, 적의 움직임을 억제합니다.
    """,
    AbilityCategory.Mythic,
    AbilityType.MYTHIC_SOLDIER76)]
public class Soldier76 : Ability
{
    private const float DefaultHeadshotMultiplier = 2f;

    private bool _isForcedHit;
    private ushort _serial;
    private Item _item;

    public override void OnEnabled()
    {
        _item = Owner.AddItem(ItemType.GunE11SR);
        _serial = _item.Serial;

        Owner.AddAbility(AbilityType.LEGEND_UNLIMITEDAMMO);

        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker != Owner ||
            ev.Player == null ||
            ev.DamageHandler?.CustomBase is not Exiled.API.Features.DamageHandlers.FirearmDamageHandler
            {
                Item: Firearm firearm
            } damageHandler ||
            firearm.Serial != _serial)
            return;

        // OnShooting의 맞은 판정 처리 중 발생한 피해는 이미 증가량이 반영되어 있으므로 중복 적용하지 않습니다.
        if (_isForcedHit || !HitboxIdentity.IsEnemy(Owner.ReferenceHub, ev.Player.ReferenceHub))
            return;

        damageHandler.Damage *= GetHeadshotDamageScale();
    }

    /// <summary>
    /// 현재 헤드샷 데미지 배율 증가량만큼 기본 데미지에 곱할 배율을 반환합니다.
    /// 헤드샷 배율은 기본 100%를 제외한 증가분만 계산합니다. (기본 200% -> 100% 증가)
    /// 여기에 불스아이 등으로 추가된 증가량을 합산합니다.
    /// </summary>
    private float GetHeadshotDamageScale()
    {
        var baseHeadshotMultiplier =
            FirearmDamageHandler.HitboxDamageMultipliers.TryGetValue(HitboxType.Headshot, out var value)
                ? value
                : DefaultHeadshotMultiplier;
        var headshotIncrease = baseHeadshotMultiplier - 1f
                               + BullsEye.HeadshotBonus * Owner.AbilityCount(AbilityType.RARE_BULLSEYE);

        return headshotIncrease > 0f ? 1f + headshotIncrease : 1f;
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Player == null || ev.Player.IsDead) return;

        if (ev.Item?.Serial == _serial)
        {
            ev.Player.AddEffect(EffectType.Scp1344, 1);
            ev.Player.AddHint("솔져76", $"<b><color={ABattle.RatingColor["신화"]}>솔져:76</color></b> 능력이 있는 E11sr 입니다.");
        }
        else
        {
            ev.Player.RemoveEffect(EffectType.Scp1344, 1);
        }
    }

    private void OnShooting(ShootingEventArgs ev)
    {
        if (ev.Item == null || ev.Firearm.Serial != _serial) return;

        if (!ev.Player.TryGetNearestVisiblePlayer(out var player, out _, 40f, 45f, [
                .. PlayerManager.List.Where(x =>
                    x == ev.Player ||
                    x.IsDead ||
                    !HitboxIdentity.IsEnemy(ev.Player.ReferenceHub, x.ReferenceHub))
            ]))
        {
            ev.IsAllowed = false;
            return;
        }

        var multiplier = player.IsScpRole() ? 0.65f : 1f;
        var check = Tools.TryGetLookPlayers(ev.Player, 40f, out var victims, out var hit);
        if (!check ||
            !victims.Contains(player) ||
            (hit != null &&
             hit.Value.collider.gameObject != player.GameObject))
        {
            // 직접 맞지 않은 총알을 맞은 판정으로 처리하므로, 이 경로에서 헤드샷 배율 증가분을 직접 반영합니다.
            _isForcedHit = true;
            try
            {
                player.Hurt(new FirearmDamageHandler(ev.Firearm.Base,
                    ev.Firearm.Damage * multiplier * GetHeadshotDamageScale() + player.MaxHealth * 0.05f,
                    ev.Firearm.Penetration));
            }
            finally
            {
                _isForcedHit = false;
            }

            ev.Player.ShowHitMarker();
        }
    }
}