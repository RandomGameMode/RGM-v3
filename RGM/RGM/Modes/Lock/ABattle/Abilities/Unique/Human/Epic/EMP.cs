using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp079;
using Interactables.Interobjects.DoorUtils;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Human.Epic;

[Ability("EMP",
    """
    시설에 강력한 전자기장 공격을 가합니다.
    모든 시설이 10초 간 정지되며, SCP-079의 신호를 30초간 차단시키고, 레벨을 1 감소시킵니다.
    사용 시 재사용 대기시간 120초가 적용됩니다.
    """, 
    AbilityCategory.Epic, AbilityType.EPIC_HUMAN_EMP, RoleAbility.Human)]

public class EMP : Ability
{
    private ushort _empSerial;
    private const float EmpCooldown = 120f;
    private const float BlackoutDuration = 10f;
    private const float Scp079SignalLostDuration = 30f;
    private bool _isCoolingDown;
    private bool _suppressScp2176Effect;

    public override void OnEnabled()
    {
        Item emp = Owner.AddItem(ItemType.Radio);
        _empSerial = emp.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.TogglingRadio += OnTogglingRadio;
        Exiled.Events.Handlers.Scp079.LosingSignal += OnLosingSignal;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.TogglingRadio -= OnTogglingRadio;
        Exiled.Events.Handlers.Scp079.LosingSignal -= OnLosingSignal;
    }
    
    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Player != Owner || ev.Item?.Serial != _empSerial)
            return;
        
        ev.Player.AddHint("EMP", $"<b><color={ABattle.RatingColor["영웅"]}>EMP</color></b> 능력이 있는 <b>무전기</b>입니다!");
    }

    private void OnTogglingRadio(TogglingRadioEventArgs ev)
    {
        if (ev.Player != Owner || ev.Item.Serial != _empSerial)
            return;

        ev.IsAllowed = false;

        if (_isCoolingDown)
        {
            ev.Player.AddHint("EMP 재사용 대기시간", "EMP가 충전 중입니다.");
            return;
        }

        _isCoolingDown = true;

        Map.TurnOffAllLights(BlackoutDuration);

        List<Lift> unlockedLifts = Lift.List.Where(lift => !lift.IsLocked).ToList();
        foreach (Lift lift in unlockedLifts)
            lift.ChangeLock(DoorLockReason.AdminCommand);

        foreach (Door door in Door.List)
        {
            door.IsOpen = false;
            door.Lock(BlackoutDuration, DoorLockType.AdminCommand);
        }

        Timing.CallDelayed(BlackoutDuration, () =>
        {
            foreach (Lift lift in unlockedLifts)
                lift.ChangeLock(DoorLockReason.None);
        });

        if (Warhead.IsInProgress && !DeadmanSwitch.IsDeadmanSwitchEnabled) // 데드맨 스위치는 종료할 수 없음.
            Warhead.Stop();

        foreach (Player player in Player.List)
        {
            if (player.Role is not Scp079Role scp079)
                continue;

            scp079.LoseSignal(Scp079SignalLostDuration);
            scp079.Level = Math.Max(1, scp079.Level - 1);
        }

        _suppressScp2176Effect = true;
        Scp2176 scp2176 = (Scp2176)Item.Create(ItemType.SCP2176, Owner);
        scp2176.FuseTime = 0.1f;
        scp2176.SpawnActive(Owner.Position, Owner);
        Timing.CallDelayed(0.25f, () => _suppressScp2176Effect = false);

        ev.Player.AddHint("EMP 발동", $"{EmpCooldown:0}초 후 다시 사용할 수 있습니다.");
        Timing.CallDelayed(EmpCooldown, () => _isCoolingDown = false);
    }

    private void OnLosingSignal(LosingSignalEventArgs ev)
    {
        if (_suppressScp2176Effect)
            ev.IsAllowed = false;
    }
}