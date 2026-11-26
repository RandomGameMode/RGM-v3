using System.Collections.Generic;
using Exiled.API.Features.Roles;
using MEC;

namespace RGM.Modes.Lock.TFT.Safe.Scp079;

[TFTAbility("발전기Ⅰ", "초당 전력이 1만큼 충전됩니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Scp079, TFTAbilityPoint.Continuous, TFTAbilityType.Generator1, "🔌")]
public class Generator1 : TFTAbility
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
                scp079Role.Energy += 1f;
            }

            yield return Timing.WaitForSeconds(1);
        }
    }
}
