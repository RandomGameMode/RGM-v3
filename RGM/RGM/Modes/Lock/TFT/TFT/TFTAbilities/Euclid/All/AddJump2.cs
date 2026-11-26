using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Euclid.All;

[TFTAbility("종아리 · 숙련", "23%만큼 더 높이 점프할 수 있습니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.AddJump2, "🐎")]
public class AddJump2 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.Lightweight, 23);
    }

    public override void OnDisabled()
    {
    }
}
