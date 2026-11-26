using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Keter.Scp;

[TFTAbility("실험체", "크기가 35% 작아집니다. 추가로 3초당 5의 체력을 회복합니다.", 
    TFTAbilityLevel.Keter, TFTAbilityCategory.Scp, TFTAbilityPoint.Continuous, TFTAbilityType.TestSubject, "😡")]
public class TestSubject : TFTAbility
{
    private CoroutineHandle _regen;

    public override void OnEnabled()
    {
        Owner.Scale = new Vector3(0.65f, 0.65f, 0.65f);

        _regen = Timing.RunCoroutine(Regen());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_regen);
    }

    private IEnumerator<float> Regen()
    {
        while (Owner.IsAlive)
        {
            Owner.Heal(5);
            yield return Timing.WaitForSeconds(3);
        }
    }
}
