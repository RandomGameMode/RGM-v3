using System;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Normal;

[Ability("랜덤박스", "랜덤한 아이템을 지급받습니다. 확률에 따라 추가 아이템을 획득합니다.", AbilityCategory.Normal, AbilityType.NORMAL_RANDOMBOX)]
public class RandomBox : Ability
{
    public override void OnEnabled()
    {
        Owner.AddRandomItem();
        while (Convert.ToByte(Random.Range(1, 101)) <= 25) {
            Owner.AddRandomItem();
        }
    }
}
