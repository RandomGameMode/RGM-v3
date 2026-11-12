using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using ProjectMER.Features.Serializable;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Mythic;

[Ability("딸깍", """
               지급된 동전을 튕기면 보는 방향에 워크스테이션을 설치합니다.
               초기 획득 시 15회가 충전되며, 10초당 1개씩 추가되며 최대 20개까지 보유 가능합니다.
               """,
    AbilityCategory.Mythic, AbilityType.MYTHIC_TOOLGUN)]
public class ToolGun : Ability
{
    private const float ForwardOffset = 1.5f;
    private const float DownwardOffset = 0.65f;
    private const byte InitialStacks = 15;
    private const byte MaxStacks = 20;
    private const float StackRecoveryInterval = 10f;

    private ushort _coinSerial;
    private byte _coinStacks;

    public override void OnEnabled()
    {
        var coin = Owner.AddItem(ItemType.Coin);
        _coinSerial = coin.Serial;
        _coinStacks = InitialStacks;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
        Timing.RunCoroutine(RecoverStacks());
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial == _coinSerial)
            ev.Player.AddHint("동전 사용 설명", $"""
                                           이 동전을 튕기면 <b><color={ABattle.RatingColor["신화"]}>워크스테이션</color></b>을 설치합니다,
                                           현재 충전량: <b><color=#FFFF00>{_coinStacks}</color></b>
                                           """);
    }

    private void OnFlippingCoin(FlippingCoinEventArgs ev)
    {
        var player = ev.Player;

        if (ev.Item.Serial != _coinSerial)
            return;

        if (_coinStacks == 0)
        {
            player.AddHint("", "현재 충전된 워크스테이션이 없습니다.");
            return;
        }

        var forward = player.CameraTransform.forward;
        forward.y = 0;
        forward.Normalize();
        _coinStacks--;

        var rayOrigin = player.Position + forward * ForwardOffset + Vector3.up;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out var hit, 100, (LayerMask)1))
            return;

        new SerializableWorkstation
        {
            IsInteractable = true,
            Position = hit.point + Vector3.down * DownwardOffset,
            Rotation = new Vector3(0, player.Rotation.eulerAngles.y, 0),
            Scale = new Vector3(0.8f, 0.8f, 0.8f)
        }.SpawnOrUpdateObject();
    }

    private IEnumerator<float> RecoverStacks()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(StackRecoveryInterval);

            if (_coinStacks < MaxStacks)
                _coinStacks++;
        }
    }
}