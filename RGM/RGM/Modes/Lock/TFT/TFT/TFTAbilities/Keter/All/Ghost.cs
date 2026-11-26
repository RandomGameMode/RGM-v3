using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Keter.All;

[TFTAbility("유령화", "문을 뚫고 다닐 수 있으며, 투명도가 45% 증가합니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.Ghost, "👻")]
public class Ghost : TFTAbility
{
    private CoroutineHandle _ghostLoop;
    public override void OnEnabled()
    {
        Owner.EnableEffect(EffectType.Fade, 115);
        _ghostLoop = Timing.RunCoroutine(Ghostloop());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_ghostLoop);
    }

    private IEnumerator<float> Ghostloop()
    {
        while (true) {
            Owner.EnableEffect(EffectType.Ghostly, 1);
            yield return Timing.WaitForSeconds(1);
        }
    }
}
