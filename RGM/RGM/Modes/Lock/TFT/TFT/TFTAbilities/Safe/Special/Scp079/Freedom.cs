using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;

namespace RGM.Modes.Lock.TFT.Safe.Scp079;

[TFTAbility("자유", "이미 작동된 발전기를 제외한 나머지 발전기들은 2분 간 잠깁니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Scp079, TFTAbilityPoint.Once, TFTAbilityType.Freedom, "🔒")]
public class Freedom : TFTAbility
{
    public override void OnEnabled()
    {
        IEnumerator<float> enumerator()
        {
            for (int i = 0; i < 120; i++)
            {
                foreach (var generator in Generator.List)
                {
                    if (generator.State != GeneratorState.Engaged)
                        generator.State = GeneratorState.Unlocked;
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        Timing.RunCoroutine(enumerator());
    }
}
