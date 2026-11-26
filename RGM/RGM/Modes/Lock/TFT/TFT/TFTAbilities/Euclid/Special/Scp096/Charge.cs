using Exiled.API.Features.Roles;

namespace DAONTFT.Core.TFT.Euclid.Scp096;

[TFTAbility("5톤 트럭", "돌격ㅣ능력의 쿨타임이 25% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp096, TFTAbilityPoint.Once, TFTAbilityType.Charge, "🚂")]
public class Charge : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp096Role scp096)
        {
            scp096.ChargeCooldown *= 0.75f;
        }
    }
}
