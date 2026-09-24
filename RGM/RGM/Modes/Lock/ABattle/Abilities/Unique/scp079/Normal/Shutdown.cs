using System;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Scp079;
using MEC;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Unique.Scp079.Common;

[Ability("셧다운제", "정전 시, 해당 방의 문들은 각각 51% 확률로 닫히고 잠기게 됩니다.", AbilityCategory.Normal, AbilityType.NORMAL_SCP079_SHUTDOWN, RoleAbility.Scp079)]
public class Shutdown : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp079.RoomBlackout += OnRoomBlackout;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp079.RoomBlackout -= OnRoomBlackout;
    }

    private void OnRoomBlackout(RoomBlackoutEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        foreach (var door in ev.Room.Doors)
        {
            if (Convert.ToByte(Random.Range(1, 101)) > 51) continue;
            door.IsOpen = false;
            door.Lock(DoorLockType.Lockdown079);

            Timing.CallDelayed(5f, () =>
            {
                door.Unlock();
            });
        }
    }
}
