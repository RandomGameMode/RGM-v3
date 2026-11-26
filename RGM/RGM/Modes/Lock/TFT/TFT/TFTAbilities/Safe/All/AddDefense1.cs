using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Safe.All;

[TFTAbility("방어 · 입문", "방어력을 얻습니다. (+10%)", TFTAbilityLevel.Safe, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.AddDefense1, "⛔")]
public class AddDefense1 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.DamageReduction, 20);
    }
}
