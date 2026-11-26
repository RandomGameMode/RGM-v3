using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp079;

namespace RGM.Modes.Lock.TFT.Keter.Scp079;

[TFTAbility("랜덤 함수", "정전ㅣ다른 10개의 방을 추가로 정전합니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Scp079,
    TFTAbilityPoint.Continuous, TFTAbilityType.RandomFunction, "🔗")]
public class RandomFunction : TFTAbility
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

        for (var i = 1; i < 11; i++)
        {
            var SelectedRoom = Room.List.GetRandomValue();

            SelectedRoom.TurnOffLights(10);
        }
    }
}
