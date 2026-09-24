using System;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Normal;

[Ability("트릭 오어 트릿", "랜덤한 SCP-330을 받습니다. 운이 좋다면 더 받을수도 있겠죠..", AbilityCategory.Normal, AbilityType.NORMAL_RANDOMCANDY)]
public class RandomCandy : Ability
{
    public override void OnEnabled()
    {
        Owner.AddRandomCandy();
        while (Convert.ToByte(Random.Range(1, 101)) <= 25) {
            Owner.AddRandomCandy();
        }
    }
}
