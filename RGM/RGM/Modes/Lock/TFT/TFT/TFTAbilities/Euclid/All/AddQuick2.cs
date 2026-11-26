using Exiled.Events.EventArgs.Player;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Euclid.All;

[TFTAbility("순발력 · 숙련", "10% 확률로 공격을 회피합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.AddQuick2, "🍃")]
public class AddQuick2 : TFTAbility
{
    int count = 0;
    float damage = 0;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player == Owner)
        {
            if (Random.Range(0, 10) == 0)
            {
                ev.IsAllowed = false;
                count++;
                damage += ev.Amount;

                Data.Description = $"10% 확률로 공격을 회피합니다. (피한 횟수: {count}, 피한 데미지: {(int)damage})";
            }
        }
    }
}
