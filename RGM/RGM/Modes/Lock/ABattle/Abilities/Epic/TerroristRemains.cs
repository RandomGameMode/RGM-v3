using System;
using InventorySystem.Items.Usables.Scp330;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Epic;

[Ability("테러리스트의 유품", "핑크 사탕(20% 확률로 사악한 사탕)이 포함된 SCP-330을 지급받습니다.", AbilityCategory.Epic, AbilityType.EPIC_TERRORISTREMAINS)]
public class TerroristRemains : Ability
{
    public override void OnEnabled()
    {
        Owner.AddCandy(Convert.ToByte(Random.Range(1, 101)) <= 20 ? CandyKindID.Evil : CandyKindID.Pink);
    }
}
