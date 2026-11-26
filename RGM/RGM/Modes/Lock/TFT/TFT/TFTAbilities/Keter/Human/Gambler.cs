using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Keter.Human;

[TFTAbility("도박꾼", "능력 키(ALT)를 누르면 들고 있는 아이템이 랜덤하게 변경됩니다. (1% 확률로 모든 아이템을 잃습니다.)", 
    TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.ALT, TFTAbilityType.Gambler, "🎰")]
public class Gambler : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;
    }

    private void OnTogglingNoClip(TogglingNoClipEventArgs ev)
    {
        if (Owner != ev.Player || ev.Player.CurrentItem == null) return;
        ev.Player.RemoveHeldItem();

        if (Random.Range(1, 101) == 77) {
            Owner.ClearInventory();
        }
        else {
            Owner.AddItem(Tools.EnumToList<ItemType>().GetRandomValue());
        }
    }
}
