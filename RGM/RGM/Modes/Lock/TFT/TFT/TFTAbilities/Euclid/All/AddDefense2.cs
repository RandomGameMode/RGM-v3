using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Euclid.All;

[TFTAbility("방어 · 숙련", "방어력을 15% 얻습니다. 추가로, 바디백 효과가 적용됩니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.AddDefense2, "⛔")]
public class AddDefense2 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.DamageReduction, 30);
        Owner.AddEffect(EffectType.BodyshotReduction, 4);
    }

    public override void OnDisabled()
    {
    }
}
