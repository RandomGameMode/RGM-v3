using Exiled.Events.EventArgs.Player;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Keter.All;

[TFTAbility("순발력 · 통달", "25% 확률로 공격을 회피합니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.AddQuick3, "🍃")]
public class AddQuick3 : TFTAbility
{
    private int _count;
    private float _damage;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player != Owner) return;
        if (Random.Range(1, 101) > 25) return;
        ev.IsAllowed = false;
        _count++;
        _damage += ev.Amount;

        Data.Description = $"20% 확률로 공격을 회피합니다. (피한 횟수: {_count}, 피한 데미지: {(int)_damage})";
    }
}
