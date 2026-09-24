using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp079;

namespace RGM.Modes.Abilities.Unique.Scp079.Common;

[Ability("랜덤 함수", "정전 시, 랜덤한 방 7개를 추가로 정전합니다.", AbilityCategory.Normal, AbilityType.NORMAL_SCP079_RANDOMFUNCTION, RoleAbility.Scp079)]
public class RandomFunction : Ability
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

        for (int i = 1; i < 8; i++)
        {
            Room SelectedRoom = Room.List.ToList().GetRandomValue();

            SelectedRoom.TurnOffLights(10);
        }
    }
}
