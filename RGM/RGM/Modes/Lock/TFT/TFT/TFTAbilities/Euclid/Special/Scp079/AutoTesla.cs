using System.Collections.Generic;
using Exiled.API.Extensions;
using MEC;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Euclid.Scp079;

[TFTAbility("자동 방어 시스템", "테슬라가 가끔씩 자동으로 작동됩니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp079, TFTAbilityPoint.Continuous, TFTAbilityType.AutoTesla, "📡")]
public class AutoTesla : TFTAbility
{
    CoroutineHandle _autoTesla;

    public override void OnEnabled()
    {
        _autoTesla = Timing.RunCoroutine(autoTesla());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_autoTesla);
    }

    IEnumerator<float> autoTesla()
    {
        while (true)
        {
            LabApi.Features.Wrappers.Tesla tesla = LabApi.Features.Wrappers.Tesla.List.GetRandomValue();
            tesla.Trigger();
            tesla.InstantTrigger();

            yield return Timing.WaitForSeconds(Random.Range(1, 60));
        }
    }
}
