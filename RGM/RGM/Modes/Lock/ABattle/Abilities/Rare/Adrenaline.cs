using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Rare;

[Ability("아드레날린", """
                  지급된 동전을 사용 시 15초 간 이동 속도가 70%p 증가합니다. (재사용 대기시간 60초)
                  효과 종료 후, 약물 부작용으로 3초간 이동이 불가합니다.
                  """, 
    AbilityCategory.Rare, AbilityType.RARE_ADRENALINE)]
public class Adrenaline : Ability
{
    private ushort _coinSerial;
    private int _cooldown;
    
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
        ev.Player.AddHint("동전 사용 설명", $"이 동전을 튕기면 <b><color={ABattle.RatingColor["희귀"]}>아드레날린</color></b> 능력을 사용할 수 있습니다.");
    }
    
    private void OnFlippingCoin(FlippingCoinEventArgs ev)
    {
        if (ev.Item.Serial != _coinSerial)
            return;
        
        if (_cooldown > 0)
        {
            ev.Player.AddHint("동전 사용 실패", $"{_cooldown}초 뒤 다시 시도해주세요.");
            return;
        }
        
        ev.Player.AddEffect(EffectType.MovementBoost, 70, 15);
        Timing.CallDelayed(15f, () =>
        {
            ev.Player.AddEffect(EffectType.Ensnared, 1, 3);
            ev.Player.AddEffect(EffectType.SinkHole, 1, 3);
        });
        
        _cooldown = 60;
        Timing.RunCoroutine(CooldownTimer());
    }
    private IEnumerator<float> CooldownTimer()
    {
        while (_cooldown > 0)
        {
            _cooldown--;
            yield return Timing.WaitForSeconds(1f);
        }
    }
}
