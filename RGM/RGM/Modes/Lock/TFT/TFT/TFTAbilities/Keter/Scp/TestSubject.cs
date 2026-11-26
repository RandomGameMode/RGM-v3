using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Keter.Scp;

[TFTAbility("실험체", "크기가 32% 작아집니다. 다른 실험체와 가까이 있으면 5초당 16의 체력을 회복합니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Scp, TFTAbilityPoint.Continuous, TFTAbilityType.TestSubject, "😡")]
public class TestSubject : TFTAbility
{
    private CoroutineHandle _test;

    public override void OnEnabled()
    {
        Owner.Scale = new Vector3(0.68f, 0.68f, 0.68f);

        _test = Timing.RunCoroutine(test());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_test);
    }

    private IEnumerator<float> test()
    {
        while (true)
        {
            if (Owner.TryGetNearestPlayer(out var player, out var radius))
                if (player.HasTftAbility(TFTAbilityType.TestSubject))
                    Owner.Heal(16);

            yield return Timing.WaitForSeconds(5);
        }
    }
}
