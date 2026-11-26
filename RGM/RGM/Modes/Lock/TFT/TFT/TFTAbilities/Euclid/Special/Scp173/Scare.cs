using Exiled.API.Enums;
using Exiled.Events.EventArgs.Scp173;

namespace RGM.Modes.Lock.TFT.Euclid.Scp173;

[TFTAbility("괴이", "순간이동할 때마다 해당 방이 1.6초 동안 정전됩니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp173,
    TFTAbilityPoint.Continuous, TFTAbilityType.Scare, "💡")]
public class Scare : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp173.Blinking += OnBlinking;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp173.Blinking -= OnBlinking;
    }

    private static void OnBlinking(BlinkingEventArgs ev)
    {
        ev.Player.CurrentRoom.Blackout(1.6f, DoorLockType.AdminCommand);
    }
}
