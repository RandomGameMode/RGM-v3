using System;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using ProjectMER.Features.Serializable;
using RGM.API.Features;
using UnityEngine;

using static RGM.Variables.Variable;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Mythic;

[Ability("딸깍", "지급된 동전을 튕기면 보는 방향에 워크스테이션을 설치합니다. 단, 3% 확률로 즉사합니다.",
    AbilityCategory.Mythic, AbilityType.MYTHIC_TOOLGUN)]
public class ToolGun : Ability
{
    private const float ForwardOffset = 1.5f;
    private const float DownwardOffset = 0.85f;

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
            ev.Player.AddHint("동전 사용 설명", $"이 동전을 튕기면 <b><color={ABattle.RatingColor["신화"]}>워크스테이션</color></b>을 설치합니다.");
        }
    }

    private void OnFlippingCoin(FlippingCoinEventArgs ev)
    {
        if (ev.Item.Serial != _coinSerial)
            return;

        Player player = ev.Player;
        if (Convert.ToByte(Random.Range(1, 101)) <= 3)
        {
            if (GodModePlayers.Contains(player))
                GodModePlayers.Remove(player);
            
            player.RemoveAllAbilities();
            player.Kill("욕심을 부리다가 아사했습니다.");
        }

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
            Scale = Vector3.one
        }.SpawnOrUpdateObject();
    }
}