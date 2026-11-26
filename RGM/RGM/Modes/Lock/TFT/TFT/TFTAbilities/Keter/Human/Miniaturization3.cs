using UnityEngine;

namespace RGM.Modes.Lock.TFT.Keter.Human;

[TFTAbility("소인화 · 통달", "몸의 크기가 45% 줄어듭니다.",
    TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Miniaturization3, "👤")]
public class Miniaturization3 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.Scale = new Vector3(0.55f, 0.55f, 0.55f);
    }

    public override void OnDisabled()
    {
    }
}