using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Euclid.All;

[TFTAbility("행운아", "대부분의 문에 엑세스할 수 있습니다.", 
    TFTAbilityLevel.Euclid, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.Fortunate, "🍀")]
public class Fortunate : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
        Exiled.Events.Handlers.Player.InteractingLocker += OnInteractingLocker;
        Exiled.Events.Handlers.Player.UnlockingGenerator += OnUnlockingGenerator;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
        Exiled.Events.Handlers.Player.InteractingLocker -= OnInteractingLocker;
        Exiled.Events.Handlers.Player.UnlockingGenerator -= OnUnlockingGenerator;
    }
    private void OnInteractingDoor(InteractingDoorEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        if (ev.Door.Type == DoorType.Scp079First)
        {
            ev.Player.AddHint("헤비도어", "이 헤비도어는 능력으로 개폐가 불가능합니다.", 1.2f);
            return;
        }

        if (Warhead.IsInProgress)
        {
            ev.Player.AddHint("알파 핵탄투", "알파 핵탄투가 작동 중일때는 문을 개폐할 수 없습니다.", 1.2f);
            return;
        }

        ev.IsAllowed = true;
    }

    private void OnInteractingLocker(InteractingLockerEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.IsAllowed = true;
    }

    private void OnUnlockingGenerator(UnlockingGeneratorEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.IsAllowed = true;
    }
}
