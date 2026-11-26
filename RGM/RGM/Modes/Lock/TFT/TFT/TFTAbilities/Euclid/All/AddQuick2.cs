using Exiled.Events.EventArgs.Player;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Euclid.All;

[TFTAbility("순발력 · 숙련", "10% 확률로 공격을 회피합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.AddQuick2, "🍃")]
public class AddQuick2 : TFTAbility
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
        if (Random.Range(1, 101) > 10) return;
        ev.IsAllowed = false;
        _count++;
        _damage += ev.Amount;

        Data.Description = $"10% 확률로 공격을 회피합니다. (피한 횟수: {_count}, 피한 데미지: {_damage:F2})";
    }
}
