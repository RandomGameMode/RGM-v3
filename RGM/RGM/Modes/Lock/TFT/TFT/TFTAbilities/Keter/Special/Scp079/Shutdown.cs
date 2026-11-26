using Exiled.API.Enums;
using Exiled.Events.EventArgs.Scp079;
using MEC;
using UnityEngine;

namespace DAONTFT.Core.TFT.Keter.Scp079;

[TFTAbility("셧다운제", "정전ㅣ해당 방들의 문들은 각각 50% 확률로 닫히고 잠기게 됩니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Scp079,
    TFTAbilityPoint.Continuous, TFTAbilityType.Shutdown, "🛃")]
public class Shutdown : TFTAbility
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
            if (Random.Range(1, 3) == 1)
            {
                door.IsOpen = false;
                door.Lock(DoorLockType.Lockdown079);

                Timing.CallDelayed(5f, () => { door.Unlock(); });
            }
    }
}
