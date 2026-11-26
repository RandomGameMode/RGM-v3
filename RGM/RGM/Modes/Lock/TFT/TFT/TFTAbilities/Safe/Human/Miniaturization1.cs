using UnityEngine;

namespace RGM.Modes.Lock.TFT.Safe.Human;

[TFTAbility("소인화 · 입문", "몸의 크기가 10% 줄어듭니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Miniaturization1, "👤")]
public class Miniaturization1 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.Scale = new Vector3(0.9f, 0.9f, 0.9f);
    }
}
