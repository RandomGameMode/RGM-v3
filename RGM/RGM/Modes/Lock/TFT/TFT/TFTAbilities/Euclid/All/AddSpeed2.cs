using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Euclid.All;

[TFTAbility("경공 · 숙련", "더 빠르게 이동합니다. (+25%)", TFTAbilityLevel.Euclid, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.AddSpeed2, "🏃")]
public class AddSpeed2 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.MovementBoost, 25);
    }

    public override void OnDisabled()
    {
    }
}
