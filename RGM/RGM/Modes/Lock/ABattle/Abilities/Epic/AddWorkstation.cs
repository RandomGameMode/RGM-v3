using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using ProjectMER.Features.Serializable;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Epic;

[Ability("업무 증가", "1회성으로 워크스테이션을 설치할 수 있는 동전을 지급받습니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_ADDWORKSTATION)]
public class AddWorkstation : Ability
{
    private const float ForwardOffset = 1.5f;
    private const float DownwardOffset = 0.65f;

    private ushort _coinSerial;

    public override void OnEnabled()
    {
        Item coin = Owner.AddItem(ItemType.Coin);
        _coinSerial = coin.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
    }
    
    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial == _coinSerial)
        {
            ev.Player.AddHint("동전 사용 설명", $"이 동전을 튕기면 <b><color={ABattle.RatingColor["영웅"]}>워크스테이션</color></b>을 설치합니다.");
        }
    }

    private void OnFlippingCoin(FlippingCoinEventArgs ev)
    {
        Player player = ev.Player;
        
        if (ev.Item.Serial != _coinSerial)
            return;

        Vector3 forward = player.CameraTransform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 rayOrigin = player.Position + forward * ForwardOffset + Vector3.up;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100, (LayerMask)1))
            return;

        new SerializableWorkstation
        {
            IsInteractable = true,
            Position = hit.point + Vector3.down * DownwardOffset,
            Rotation = new Vector3(0, player.Rotation.eulerAngles.y, 0),
            Scale = new Vector3(0.8f, 0.8f, 0.8f)
        }.SpawnOrUpdateObject();
        
        ev.Item.Destroy();
    }
}