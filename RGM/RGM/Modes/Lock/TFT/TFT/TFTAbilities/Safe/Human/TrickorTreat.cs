using InventorySystem.Items.Usables.Scp330;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Safe.Human;

[TFTAbility("트릭 오어 트릿", "무작위 사탕을 받습니다(20% 확률로 Pink, 5% 확률로 Evil 획득).", TFTAbilityLevel.Safe, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.TrickOrTreat, "🍬")]
public class TrickorTreat : TFTAbility
{
    public override void OnEnabled()
    {
        if (Random.Range(1, 101) <= 20)
        {
            if (Random.Range(1, 11) <= 4)
            {
                Owner.AddCandy(CandyKindID.Evil); // 5%
            }
            Owner.AddCandy(CandyKindID.Pink); // 20%
        }
        else
        {
            Owner.AddRandomCandy();
        }
    }
}
