using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Safe.All;

[TFTAbility("경공 · 입문", "더 빠르게 이동합니다. (+10%)", TFTAbilityLevel.Safe, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.AddSpeed1, "🏃")]
public class AddSpeed1 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.MovementBoost, 10);
    }
}
