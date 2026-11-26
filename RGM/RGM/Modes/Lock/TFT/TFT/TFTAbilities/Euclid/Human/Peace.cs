using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;

namespace RGM.Modes.Lock.TFT.Euclid.Human;

[TFTAbility("평화주의자", "무기를 소지할 수 없게 됩니다. 대신 초당 최대 체력이 1씩 증가합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human,
    TFTAbilityPoint.Continuous, TFTAbilityType.Peace, "💞")]
public class Peace : TFTAbility
{
    private CoroutineHandle _peaceLoop;
    private CoroutineHandle _checkInventoryLoop;

    public override void OnEnabled()
    {
        foreach (var item in Owner.Items.ToList())
            if (item.Type.IsWeapon())
                Owner.DropItem(item);

        Exiled.Events.Handlers.Player.ItemAdded += OnItemAdded;

        _peaceLoop = Timing.RunCoroutine(PeaceLoop());
        _checkInventoryLoop = Timing.RunCoroutine(CheckInventory());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ItemAdded -= OnItemAdded;

        Timing.KillCoroutines(_peaceLoop);
        Timing.KillCoroutines(_checkInventoryLoop);
    }

    private IEnumerator<float> PeaceLoop()
    {
        while (!Round.IsEnded)
        {
            Owner.MaxHealth += 1;
            Owner.Health += 1;

            yield return Timing.WaitForSeconds(1f);
        }
    }

    private IEnumerator<float> CheckInventory()
    {
        while (!Round.IsEnded)
        {
            foreach (var item in Owner.Items.ToList())
                if (item.Type.IsWeapon() || item.Type.IsThrowable())
                    Owner.RemoveItem(item);

            yield return Timing.WaitForSeconds(0.2f);
        }
    }

    private void OnItemAdded(ItemAddedEventArgs ev)
    {
        if (ev.Player != Owner) return;
        if (ev.Item.Type.IsWeapon() || ev.Item.Type.IsThrowable())
            ev.Player.DropItem(ev.Item);
    }
}
