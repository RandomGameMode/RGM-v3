using System;
using InventorySystem.Items.Usables.Scp330;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Normal;

[Ability("체력 보충", "파란 사탕을 받습니다.", AbilityCategory.Normal, AbilityType.NORMAL_STAMINAREPLENISHMENT)]
public class StaminaReplenishment : Ability
{
    public override void OnEnabled()
    {
        Owner.AddCandy(CandyKindID.Blue);
        while (Convert.ToByte(Random.Range(1, 101)) <= 25)
        {
            Owner.AddCandy(CandyKindID.Blue);
        }
    }
}
