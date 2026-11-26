using System.Collections.Generic;
using Exiled.API.Features.Roles;
using MEC;

namespace RGM.Modes.Lock.TFT.Euclid.Scp079;

[TFTAbility("발전기Ⅱ", "초당 전력이 2만큼 충전됩니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp079, TFTAbilityPoint.Continuous, TFTAbilityType.Generator2, "🔌")]
public class Generator2 : TFTAbility
{
    CoroutineHandle _generatorLoop;
    public override void OnEnabled()
    {
        _generatorLoop = Timing.RunCoroutine(generatorLoop());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_generatorLoop);
    }

    IEnumerator<float> generatorLoop()
    {
        while (true)
        {
            if (Owner.Role is Scp079Role scp079Role)
            {
                scp079Role.Energy += 2f;
            }

            yield return Timing.WaitForSeconds(1);
        }
    }
}
