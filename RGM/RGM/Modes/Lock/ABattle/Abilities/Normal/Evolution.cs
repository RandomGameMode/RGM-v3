using System.Linq;
using UnityEngine;

using static RGM.Variables.Variable;

namespace RGM.Modes.Abilities.Normal;

[Ability("진화", "몸의 크기가 8%p 작아집니다. (최대 10회까지 적용)", AbilityCategory.Normal, AbilityType.NORMAL_EVOLUTION)]
public class Evolution : Ability
{
    private const float Scale = 0.08f;
    private const int MaxCount = 10;

    public override void OnEnabled()
    {
        if (Owner.AbilityCount(AbilityType.NORMAL_EVOLUTION) >= MaxCount)
            return;
        if (EnabledModeList.Any(x => x.Data.Type == ModeType.GulliversTravels))
            return;

        Owner.Scale = new Vector3(Owner.Scale.x - Scale, Owner.Scale.y - Scale, Owner.Scale.z - Scale);
    }
}
