using Exiled.API.Features.Roles;

namespace DAONTFT.Core.TFT.Euclid.Scp096;

[TFTAbility("분노 조절 문제", "격노ㅣ능력의 쿨타임이 50% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp096, TFTAbilityPoint.Once, TFTAbilityType.Enrage, "😠")]
public class Enrage : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp096Role scp096)
        {
            scp096.EnrageCooldown *= 0.5f;
        }
    }
}
