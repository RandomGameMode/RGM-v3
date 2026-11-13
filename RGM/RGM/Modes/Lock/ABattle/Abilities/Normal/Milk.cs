using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Normal;

[Ability("우유", "지급된 동전을 튕기면 현재 자신에게 적용된 모든 효과를 제거합니다.", 
    AbilityCategory.Normal, AbilityType.NORMAL_MILK)]
public class Milk : Ability
{
    private ushort _coinSerial;
    private bool _isConsumed;

    public override void OnEnabled()
    {
        Item item = Owner.AddItem(ItemType.Coin);
        _coinSerial = item.Serial;
        _isConsumed = false;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.FlippingCoin -= OnFlippingCoin;
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Player == null || ev.Player.IsNPC || _isConsumed || ev.Item?.Serial != _coinSerial) return;
        ev.Player.AddHint("동전 사용 설명", $"이 동전을 튕기면 <b><color={ABattle.RatingColor["일반"]}>우유</color></b> 능력을 사용할 수 있습니다.");
    }

    private void OnFlippingCoin(FlippingCoinEventArgs ev)
    {
        if (_isConsumed ||
            ev.Player == null ||
            ev.Item?.Serial != _coinSerial ||
            ev.Player.CurrentRoom.Type == RoomType.Pocket)
            return;

        _isConsumed = true;
        ev.Player.DisableAllEffects();
        ev.Item.Destroy();

        OnDisabled();
    }
}
