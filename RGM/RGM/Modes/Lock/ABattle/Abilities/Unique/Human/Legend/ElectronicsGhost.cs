using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp079;
using Interactables.Interobjects.DoorUtils;
using MapGeneration.Holidays;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Human.Legend;

[Ability("전자 유령",
    """
    전자 유령이 되어 시설 시스템에 간섭합니다.
    지급된 무전기를 우클릭 시, 모든 적 세력 인원에게서 각 인원 마다 능력을 1개(21% 확률로 연쇄) 강탈합니다. 강탈한 능력은 반사경 및 복제로 능력 복제가 불가합니다.
    추가로, 6초간 모든 시설 시스템을 정지시키며, SCP-079의 신호를 15초간 차단시키고, 레벨을 2 감소시킵니다.
    사용 시 재사용 대기시간 120초가 적용됩니다.
    """, 
    AbilityCategory.Legend, AbilityType.LEGEND_HUMAN_ELECTRONICSGHOST, RoleAbility.Human, isUnique:true)]

public class ElectronicsGhost : Ability
{
    private const float ElectronicsGhostCooldown = 120f;
    private const float BlackoutDuration = 6f;
    private const float Scp079SignalLostDuration = 15f;
    private const int Scp079LevelPenalty = 2;
    private const float ExtraStealChance = 0.21f;
    private const float RadioCheckInterval = 0.1f;

    private ushort _electronicsGhostSerial;
    private bool _isCoolingDown;
    private float _cooldownEndTime;
    private bool _suppressScp2176Effect;
    private CoroutineHandle _radioRoutine;

    private readonly List<AbilityType> _exceptAbilities =
    [
        AbilityType.NORMAL_REROLL, AbilityType.NORMAL_EXCHANGE, AbilityType.NORMAL_SUSPICIOUSSTEW,
        AbilityType.RARE_DND, AbilityType.RARE_TELEPORTATION, AbilityType.RARE_UPGRADE,
        AbilityType.RARE_CONTRACT, AbilityType.EPIC_HALLOWEEN_MARSHMELLOW, AbilityType.LEGEND_HALLOWEEN_CANDYPOWER
    ];
    
    public override void OnEnabled()
    {
        GiveRadio();

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
        Exiled.Events.Handlers.Player.TogglingRadio += OnTogglingRadio;
        Exiled.Events.Handlers.Scp079.LosingSignal += OnLosingSignal;

        _radioRoutine = Timing.RunCoroutine(RadioGuardRoutine());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
        Exiled.Events.Handlers.Player.TogglingRadio -= OnTogglingRadio;
        Exiled.Events.Handlers.Scp079.LosingSignal -= OnLosingSignal;

        Timing.KillCoroutines(_radioRoutine);

        Item radio = GetRadio();
        if (radio != null)
            Owner.RemoveItem(radio);
    }

    #region 무전기

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Player != Owner || ev.Item?.Serial != _electronicsGhostSerial)
            return;
        
        ev.Player.AddHint("전자 유령", $"<b><color={ABattle.RatingColor["전설"]}>전자 유령</color></b> 능력이 있는 <b>무전기</b>입니다!");
    }

    private void OnDroppingItem(DroppingItemEventArgs ev)
    {
        if (ev.Player != Owner || ev.Item?.Serial != _electronicsGhostSerial)
            return;

        ev.IsAllowed = false;
        ev.Player.AddHint("전자 유령", "전자 유령의 무전기는 버릴 수 없습니다.", 1.5f);
    }

    /// <summary>
    /// 교환, 강화, 리롤 등으로 무전기가 강제로 사라지거나 다른 아이템으로 바뀌었으면 복구한다.
    /// DroppingItem 이벤트를 우회하는 경우를 대비해 소유 여부와 종류를 계속 검증한다.
    /// </summary>
    private IEnumerator<float> RadioGuardRoutine()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(RadioCheckInterval);

            if (Owner == null || !Owner.IsAlive)
                continue;

            EnsureRadio();
        }
    }

    private void EnsureRadio()
    {
        Item radio = GetRadio();
        if (radio?.Type == ItemType.Radio)
            return;

        // 동일 Serial의 아이템이 다른 종류로 바뀐 경우, 해당 아이템을 제거한 뒤 복구한다.
        if (radio != null)
            Owner.RemoveItem(radio);

        GiveRadio();
    }

    private void GiveRadio()
    {
        Item radio = Owner.AddItem(ItemType.Radio);
        if (radio != null)
            _electronicsGhostSerial = radio.Serial;
    }

    private Item GetRadio() => Owner?.Items.FirstOrDefault(x => x.Serial == _electronicsGhostSerial);

    #endregion

    #region 능력 발동

    private void OnTogglingRadio(TogglingRadioEventArgs ev)
    {
        if (ev.Player != Owner || ev.Item.Serial != _electronicsGhostSerial)
            return;

        ev.IsAllowed = false;

        if (_isCoolingDown)
        {
            float remaining = Mathf.Max(0f, _cooldownEndTime - Time.time);
            ev.Player.AddHint("전자 유령 재사용 대기시간", $"지금은 간섭할 수 없습니다. {remaining:0}초 후 재 침투가 가능합니다.");
            return;
        }

        _isCoolingDown = true;
        _cooldownEndTime = Time.time + ElectronicsGhostCooldown;

        StealAbilities();
        BlackoutFacility();
        StopWarhead();
        InterfereScp079();
        DetonateScp2176();

        ev.Player.AddHint("전자 유령 발동", $"{ElectronicsGhostCooldown:0}초 후 다시 사용할 수 있습니다.");
        Timing.CallDelayed(ElectronicsGhostCooldown, () => _isCoolingDown = false);
    }

    /// <summary>
    /// 모든 적 세력 인원에게서 능력을 1개 강탈하고, 25% 확률로 추가 강탈을 반복한다.
    /// </summary>
    private void StealAbilities()
    {
        List<Player> enemies = Player.List
            .Where(p => p != Owner && !p.IsNPC && p.IsAlive)
            .Where(p => HitboxIdentity.IsEnemy(Owner.ReferenceHub, p.ReferenceHub))
            .ToList();

        int totalStolen = 0;

        foreach (Player enemy in enemies)
        {
            if (!TryStealAbility(enemy))
                continue;

            totalStolen++;
            
            while (UnityEngine.Random.value < ExtraStealChance)
            {
                if (!TryStealAbility(enemy))
                    break;

                totalStolen++;
            }
        }

        Owner.AddHint("전자 유령", $"적 세력에게서 능력을 총 {totalStolen}개 강탈했습니다!");
    }

    private bool TryStealAbility(Player victim)
    {
        var candidates = victim.GetAbilities()
            .Where(a => a.Data.RoleAbility == RoleAbility.None &&
                        a.Data.Category is not (AbilityCategory.None or AbilityCategory.Dummy
                            or AbilityCategory.Ancient or AbilityCategory.Synergy))
            // 강탈 제외 목록에 있는 능력은 강탈하지 않는다.
            .Where(a => !_exceptAbilities.Contains(a.Data.AbilityType))
            // 유니크 능력은 항상 1개만 유지되므로, 이미 보유 중이면 강탈 대상에서 제외한다.
            .Where(a => !a.Data.IsUnique || !Owner.HasAbility(a.Data.AbilityType))
            .ToList();

        while (candidates.Count > 0)
        {
            Ability target = candidates.GetRandomValue();
            candidates.Remove(target);

            // 반사경 및 복제로 복제되지 않도록 allowReflector를 끈다.
            if (!ABattle.Instance.AddAbility(Owner, target.Data.AbilityType, allowReflector: false))
                continue;

            victim.RemoveAbility(target);
            victim.AddAbility(AbilityType.DUMMY_STOLENBYGHOST); // 강탈당할 때마다 1개씩 부여
            victim.AddHint("전자 유령", $"{target.Data.GetFormattedName()} 능력을 <b>전자 유령</b>에게 강탈당했습니다!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 모든 시설을 정전시키고, 문과 엘리베이터의 상호작용을 잠근다. (문은 닫지 않는다.)
    /// </summary>
    private static void BlackoutFacility()
    {
        Map.TurnOffAllLights(BlackoutDuration);

        List<Lift> unlockedLifts = Lift.List.Where(lift => !lift.IsLocked).ToList();
        foreach (Lift lift in unlockedLifts)
            lift.ChangeLock(DoorLockReason.AdminCommand);

        foreach (Door door in Door.List)
            door.Lock(BlackoutDuration, DoorLockType.AdminCommand);

        Timing.CallDelayed(BlackoutDuration, () =>
        {
            foreach (Lift lift in unlockedLifts)
                lift.ChangeLock(DoorLockReason.None);
        });
    }

    /// <summary>
    /// 알파 핵탄두가 작동 중이면 강제로 중지한다. 데드맨 스위치는 중지할 수 없다.
    /// </summary>
    private static void StopWarhead()
    {
        if (Warhead.IsInProgress && !DeadmanSwitch.IsDeadmanSwitchEnabled)
            Warhead.Stop();
    }

    /// <summary>
    /// SCP-079의 신호를 차단하고 레벨을 감소시킨다. (최소 레벨 1)
    /// </summary>
    private static void InterfereScp079()
    {
        foreach (Player player in Player.List)
        {
            if (player.Role is not Scp079Role scp079)
                continue;

            scp079.LoseSignal(Scp079SignalLostDuration);
            scp079.Level = Math.Max(1, scp079.Level - Scp079LevelPenalty); // 1 이하로 내려가면 1로 보정
        }
    }

    /// <summary>
    /// 본인 위치에 SCP-2176을 소환해 즉시 파괴한다. 2176 고유 효과(신호 차단)는 적용되지 않는다.
    /// </summary>
    private void DetonateScp2176()
    {
        _suppressScp2176Effect = true;

        Scp2176 scp2176 = (Scp2176)Item.Create(ItemType.SCP2176, Owner);
        scp2176.FuseTime = 0.1f;
        scp2176.SpawnActive(Owner.Position, Owner);

        Timing.CallDelayed(0.25f, () => _suppressScp2176Effect = false);
    }

    private void OnLosingSignal(LosingSignalEventArgs ev)
    {
        if (_suppressScp2176Effect)
            ev.IsAllowed = false;
    }

    #endregion
}
