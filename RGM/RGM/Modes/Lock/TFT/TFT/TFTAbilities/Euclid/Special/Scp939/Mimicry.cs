using Exiled.API.Features.Roles;

namespace DAONTFT.Core.TFT.Euclid.Scp939;

[TFTAbility("흉내쟁이", "흉내ㅣ능력의 쿨타임이 75% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp939, TFTAbilityPoint.Once, TFTAbilityType.Mimicry, "🔊")]
public class Mimicry : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp939Role scp939)
        {
            scp939.MimicryCooldown *= 0.25f;
        }
    }
}
