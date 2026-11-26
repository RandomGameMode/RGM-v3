using Exiled.API.Enums;
using RGM.API.Features;

namespace DAONTFT.Core.TFT.Euclid.Scp096;

[TFTAbility("아웃사이더", "전신이 40% 투명해집니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp096, TFTAbilityPoint.Once, TFTAbilityType.OutSider, "🚫")]
public class OutSider : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.Fade, 102);
    }
}
