using Exiled.API.Extensions;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;

namespace DAONTFT.Core.TFT.Keter.Human;

[TFTAbility("마약 중독자", "20초마다 랜덤한 사탕을 획득합니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.CandyAddict, "🍭")]
public class CandyAddict : TFTAbility
{
    private CoroutineHandle _candyParty;

    public override void OnEnabled()
    {
        if (!Timing.IsRunning(_candyParty))
            _candyParty = Timing.RunCoroutine(candyParty());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_candyParty);
    }

    IEnumerator<float> candyParty()
    {
        Owner.AddCandy(Tools.EnumToList<CandyKindID>().GetRandomValue());

        while (true)
        {
            if (Owner.IsAlive)
                Owner.AddCandy(Tools.EnumToList<CandyKindID>().GetRandomValue());

            for (int i = 0; i < 20; i++)
            {
                Data.Description = $"다음 사탕 획득까지: {20 - i}초";

                yield return Timing.WaitForSeconds(1f);
            }
        }
    }
}
