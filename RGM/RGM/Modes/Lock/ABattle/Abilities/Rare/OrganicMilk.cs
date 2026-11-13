using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Rare;

[Ability("유기농 우유", "자신에게 해로운 효과만 제거합니다.", AbilityCategory.Rare, AbilityType.RARE_ORGANICMILK)]
public class OrganicMilk : Ability
{
    private ushort _coinSerial;

    public override void OnEnabled()
    {
        Item item = Owner.AddItem(ItemType.Coin);
        _coinSerial = item.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _coinSerial) return;
        ev.Player.AddHint("동전 사용 설명", $"이 동전을 튕기면 <b><color={ABattle.RatingColor["희귀"]}>유기농 우유</color></b> 능력을 사용할 수 있습니다.");
    }

    private void OnFlippingCoin(FlippingCoinEventArgs ev)
    {
        if (ev.Player == null || ev.Player.IsNPC || _coinSerial != ev.Item.Serial || ev.Player.CurrentRoom.Type == RoomType.Pocket) return;
        foreach (var effectType in ev.Player.ActiveEffects
                     .Select(effect => effect.GetEffectType())
                     .Where(effectType => !EffectManager.IsKeptBuff(effectType))
                     .ToList())
        {
            ev.Player.DisableEffect(effectType);
        }

        ev.Item.Destroy();
    }
}
