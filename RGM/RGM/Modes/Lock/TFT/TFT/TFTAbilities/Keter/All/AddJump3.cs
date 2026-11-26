using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Keter.All;

[TFTAbility("종아리 · 통달", "50%만큼 더 높이 점프할 수 있습니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.AddJump3, "🐎")]
public class AddJump3 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.Lightweight, 50);
    }
}
