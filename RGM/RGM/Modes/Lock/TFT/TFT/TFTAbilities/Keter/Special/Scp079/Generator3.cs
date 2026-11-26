using System.Collections.Generic;
using Exiled.API.Features.Roles;
using MEC;

namespace DAONTFT.Core.TFT.Keter.Scp079;

[TFTAbility("발전기Ⅲ", "초당 전력이 4만큼 충전됩니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Scp079, TFTAbilityPoint.Continuous, TFTAbilityType.Generator3, "🔌")]
public class Generator3 : TFTAbility
{
    private CoroutineHandle _generatorLoop;

    public override void OnEnabled()
    {
        _generatorLoop = Timing.RunCoroutine(generatorLoop());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_generatorLoop);
    }

    private IEnumerator<float> generatorLoop()
    {
        while (true)
        {
            if (Owner.Role is Scp079Role scp079Role) scp079Role.Energy += 1.2f;

            yield return Timing.WaitForSeconds(1);
        }
    }
}
