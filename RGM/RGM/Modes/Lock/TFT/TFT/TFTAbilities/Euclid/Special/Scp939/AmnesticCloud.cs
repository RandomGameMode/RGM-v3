using Exiled.API.Features.Roles;

namespace RGM.Modes.Lock.TFT.Euclid.Scp939;

[TFTAbility("황혼의 숲", "망각 안개ㅣ능력의 쿨타임이 25% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp939, TFTAbilityPoint.Once, TFTAbilityType.AmnesticCloud, "🌴")]
public class AmnesticCloud : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp939Role scp939)
        {
            scp939.AmnesticCloudCooldown *= 0.75f;
        }
    }
}
