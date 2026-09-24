using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp079.Legend;


[Ability("폭발은 예술이다", $"30초마다 <color=#2ECCFA>[전용 희귀]</color> 폭격 능력을 1 ~ 2개 획득합니다.", AbilityCategory.Legend, AbilityType.LEGEND_SCP079_EXPLOSIONISART, RoleAbility.Scp079)]
public class ExplosionIsArt : Ability
{
    private CoroutineHandle _airstrike;
    public override void OnEnabled()
    {
        _airstrike = Timing.RunCoroutine(AirstrikeCoroutine());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_airstrike);
    }

    private IEnumerator<float> AirstrikeCoroutine()
    {
        while (Owner.IsAlive && Owner != null)
        {
            if (Random.Range(1, 3) == 1)
            {
                Owner.AddAbility(AbilityType.RARE_SCP079_AIRSTRIKE);
            }
            Owner.AddAbility(AbilityType.RARE_SCP079_AIRSTRIKE);
            yield return Timing.WaitForSeconds(30f);
        }
    }
}