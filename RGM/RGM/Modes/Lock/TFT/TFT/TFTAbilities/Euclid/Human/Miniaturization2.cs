using UnityEngine;

namespace RGM.Modes.Lock.TFT.Euclid.Human;

[TFTAbility("소인화 · 숙련", "몸의 크기가 20% 줄어듭니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Miniaturization2, "👤")]
public class Miniaturization2 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.Scale = new Vector3(0.8f, 0.8f, 0.8f);
    }

    public override void OnDisabled()
    {
    }
}