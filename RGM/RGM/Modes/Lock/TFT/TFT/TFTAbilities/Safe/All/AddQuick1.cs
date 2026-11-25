using Exiled.Events.EventArgs.Player;
using UnityEngine;

namespace DAONTFT.Core.TFT.Safe.All;

[TFTAbility("순발력 · 입문", "5% 확률로 공격을 회피합니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.AddQuick1, "🍃")]
public class AddQuick1 : TFTAbility
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

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player != Owner) return;
        if (Random.Range(1, 101) > 5) return;
        ev.IsAllowed = false;
        count++;
        damage += ev.Amount;

        Data.Description = $"5% 확률로 공격을 회피합니다. (피한 횟수: {count}, 피한 데미지: {(int)damage})";
    }
}
