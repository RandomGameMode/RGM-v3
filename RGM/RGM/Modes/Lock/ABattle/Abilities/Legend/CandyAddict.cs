using System.Collections.Generic;
using Exiled.API.Extensions;
using InventorySystem.Items.Usables.Scp330;
using MapGeneration.Holidays;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Legend;

[Ability("마약 중독자", """
                   6초마다 랜덤한 사탕이 지급됩니다.
                   Halloween 기간에는 지급 시간이 3초 감소되고, 지급 개수가 1개 증가합니다.
                   """, AbilityCategory.Legend, AbilityType.LEGEND_CANDYADDICT)]
public class CandyAddict : Ability
{
    private CoroutineHandle _candyAddict;
    private readonly bool _isHalloween = HolidayUtils.IsHolidayActive(HolidayType.Halloween);

    public override void OnEnabled() => _candyAddict = Timing.RunCoroutine(CandyParty());

    public override void OnDisabled() => Timing.KillCoroutines(_candyAddict);

    private IEnumerator<float> CandyParty()
    {
        while (true)
        {
            Owner.AddCandy(Tools.EnumToList<CandyKindID>().GetRandomValue());
            if (_isHalloween) Owner.AddCandy(Tools.EnumToList<CandyKindID>().GetRandomValue());

            yield return Timing.WaitForSeconds(_isHalloween ? 3 : 6);
        } 
    }
}
