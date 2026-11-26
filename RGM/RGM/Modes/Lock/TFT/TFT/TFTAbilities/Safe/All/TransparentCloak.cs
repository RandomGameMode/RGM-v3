using Exiled.API.Enums;

namespace RGM.Modes.Lock.TFT.Safe.All;

[TFTAbility("투명 망토", "25초간 투명해집니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.TransparentCloak, "🪞")]
public class TransparentCloak : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.EnableEffect(EffectType.Invisible, 1, 25);
    }
}
