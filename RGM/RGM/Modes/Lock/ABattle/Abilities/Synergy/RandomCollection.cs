using System.Collections.Generic;
using Exiled.API.Extensions;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Synergy;

[RequiresAbility(AbilityType.NORMAL_RANDOMBOX, AbilityType.EPIC_RANDOMCHEST, AbilityType.LEGEND_RANDOMPACKAGE)]
[Ability("랜덤 컬렉션", "<랜덤박스, 수집가, 랜덤상자, 랜덤택배> 3+1! 셋 중 하나를 더 받으세요!", AbilityCategory.Synergy, AbilityType.SYNERGY_RANDOMCOLLECTION)]

public class RandomCollection : Ability
{
    private readonly List<AbilityType> _randoms =
    [
        AbilityType.NORMAL_RANDOMBOX,
        AbilityType.RARE_COLLECTOR,
        AbilityType.EPIC_RANDOMCHEST,
        AbilityType.LEGEND_RANDOMPACKAGE
    ];
    public override void OnEnabled()
    {
        Owner.AddAbility(_randoms.GetRandomValue());
    }
}
