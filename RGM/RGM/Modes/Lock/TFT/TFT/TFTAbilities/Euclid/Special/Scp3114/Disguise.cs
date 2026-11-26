using Exiled.API.Features.Roles;

namespace DAONTFT.Core.TFT.Euclid.Scp3114;

[TFTAbility("변장술사", "변장ㅣ능력의 지속 시간이 25% 증가합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp3114, TFTAbilityPoint.Once, TFTAbilityType.Disguise, "🎭")]
public class Disguise : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp3114Role scp3114)
        {
            scp3114.DisguiseDuration *= 1.25f;
        }
    }
}
