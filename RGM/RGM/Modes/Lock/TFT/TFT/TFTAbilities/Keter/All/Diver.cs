using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Keter.All;

[TFTAbility("잠수부", "스태미나가 무제한이 되며, 발소리가 제거됩니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.All, TFTAbilityPoint.Continuous, TFTAbilityType.Diver, "🫁")]
public class Diver : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.IsUsingStamina = false;
        Owner.AddEffect(EffectType.SilentWalk, 11);
    }

    public override void OnDisabled()
    {
        Owner.IsUsingStamina = true;
        Owner.RemoveEffect(EffectType.SilentWalk, 11);
    }
}
