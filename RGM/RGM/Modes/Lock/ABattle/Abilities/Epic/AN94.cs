using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Firearms.Modules;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Epic;

//[Ability("AN-94", "2점사 소총을 얻습니다.", AbilityCategory.Epic, AbilityType.EPIC_AN94)]

public class AN94 : Ability
{
    private const int MaxAutomaticReloads = 15;

    private ushort _an94Serial;
    private int _automaticReloads;
    private bool _isApplyingBurstDamage;
    private int _burstDamageToken;
    private int _pendingBurstDamageToken;

    public override void OnEnabled()
    {
        Item item = Owner.AddItem(ItemType.GunAK);
        for (int i = 0; i < 4; i++) {
            Owner.AddItem(ItemType.Ammo762x39);
        }
        _an94Serial = item.Serial;
        Firearm firearm = item.As<Firearm>();
        ApplyAn94Settings(firearm);
        LoadSingleRound(firearm);

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.ReloadingWeapon += OnReloadingWeapon;
        Exiled.Events.Handlers.Player.ReloadedWeapon += OnReloadedWeapon;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.ReloadingWeapon -= OnReloadingWeapon;
        Exiled.Events.Handlers.Player.ReloadedWeapon -= OnReloadedWeapon;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    public void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item == null || ev.Item.Serial != _an94Serial)
            return;

        ev.Player.AddHint("AN-94", $"<b><color={ABattle.RatingColor["영웅"]}>AN-94</color></b> 능력이 있는 AK 입니다");
    }
    public void OnShooting(ShootingEventArgs ev)
    {
        if (ev.Item.Serial != _an94Serial) return;

        Firearm firearm = ev.Item.As<Firearm>();
        ApplyAn94Settings(firearm);

        _pendingBurstDamageToken = 0;
        RegisterPendingBurstDamage();
        RefillSingleRoundAfterShot();
    }

    public void OnReloadingWeapon(ReloadingWeaponEventArgs ev)
    {
        if (ev.Item == null || ev.Item.Serial != _an94Serial)
            return;

        Firearm firearm = ev.Item.As<Firearm>();
        if (IsRoundChambered(firearm))
        {
            // 약실에 탄환이 남아 있으면 기본 재장전이 30발을 채우므로 막는다.
            // 이 경우에는 재장전 모션이나 탄약 소비도 발생하지 않는다.
            ev.IsAllowed = false;
            return;
        }

        // 빈 상태의 재장전은 기본 동작을 허용한다. 따라서 재장전 모션이 재생되고
        // 예비 탄약도 AK의 한 탄창 분량(30발)만큼 정상적으로 소모된다.
        _automaticReloads = 0;
    }

    public void OnReloadedWeapon(ReloadedWeaponEventArgs ev)
    {
        if (ev.Item == null || ev.Item.Serial != _an94Serial)
            return;

        _automaticReloads = 0;
        LoadSingleRound(ev.Item.As<Firearm>());
    }

    public void OnHurting(HurtingEventArgs ev)
    {
        if (_isApplyingBurstDamage ||
            ev.Attacker != Owner ||
            ev.Player == ev.Attacker ||
            ev.Attacker.CurrentItem == null ||
            ev.Attacker.CurrentItem.Serial != _an94Serial ||
            ev.DamageHandler.CustomBase is not FirearmDamageHandler firearmDamageHandler ||
            !TryConsumeBurstDamageToken())
            return;

        Player target = ev.Player;
        Player attacker = ev.Attacker;
        float damage = ev.DamageHandler.Damage;

        Timing.CallDelayed(0.034f, () =>
        {
            if (!ev.IsAllowed ||
                target == null ||
                attacker == null ||
                !target.IsAlive)
                return;

            try
            {
                _isApplyingBurstDamage = true;
                firearmDamageHandler.Damage = damage;
                firearmDamageHandler.Attacker = attacker;
                firearmDamageHandler.IsSuicide = false;
                target.Hurt(firearmDamageHandler);
            }
            finally
            {
                _isApplyingBurstDamage = false;
            }
        });
    }

    private void RefillSingleRoundAfterShot()
    {
        if (_automaticReloads >= MaxAutomaticReloads)
            return;

        // Shooting은 실제 탄환 소모 전에 호출된다. 실제 ServerShoot 처리가 끝난 뒤
        // 보충해야 클라이언트의 격발 예측과 서버의 약실 상태가 어긋나지 않는다.
        Timing.CallDelayed(0.05f, () =>
        {
            if (Item.Get(_an94Serial) is not Firearm { MagazineAmmo: 0 } firearm)
                return;

            LoadSingleRound(firearm);
            _automaticReloads++;
        });
    }

    private static void LoadSingleRound(Firearm firearm)
    {
        if (IsRoundChambered(firearm))
        {
            // 약실의 1발만 남기고 탄창 예비 탄약은 제거한다.
            firearm.MagazineAmmo = 0;
            return;
        }

        firearm.MagazineAmmo = 1;
        ChamberRound(firearm);
    }

    private static void ChamberRound(Firearm firearm)
    {
        if (firearm?.Base == null)
            return;

        foreach (ModuleBase module in firearm.Base.Modules)
        {
            if (module is AutomaticActionModule action)
            {
                // 빈 탄창에서 발생한 드라이 파이어는 노리쇠를 잠그고 격발 상태를 해제한다.
                // 탄약만 보충하면 다음 클릭이 거부되므로, 1발을 약실에 넣어
                // 노리쇠/격발 상태와 클라이언트 동기화를 함께 복구한다.
                action.ServerCycleAction();
                return;
            }
        }
    }

    private static bool IsRoundChambered(Firearm firearm)
    {
        if (firearm?.Base == null)
            return false;

        foreach (ModuleBase module in firearm.Base.Modules)
        {
            if (module is AutomaticActionModule action)
                return action.IsLoaded;
        }

        return firearm.MagazineAmmo > 0;
    }

    private void RegisterPendingBurstDamage()
    {
        int token = ++_burstDamageToken;
        _pendingBurstDamageToken = token;

        Timing.CallDelayed(0.1f, () =>
        {
            if (_pendingBurstDamageToken == token)
                _pendingBurstDamageToken = 0;
        });
    }

    private bool TryConsumeBurstDamageToken()
    {
        if (_pendingBurstDamageToken == 0)
            return false;

        _pendingBurstDamageToken = 0;
        return true;
    }

    private static void ApplyAn94Settings(Firearm firearm)
    {
        if (firearm == null)
            return;

        firearm.DamageFalloffDistance = 500f;
        firearm.Damage = 24.5f;
    }
}
