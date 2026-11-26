using Exiled.API.Features.Roles;

namespace RGM.Modes.Lock.TFT.Euclid.Scp049;

[TFTAbility("날카로운 촉", "의사의 좋은 감각ㅣ능력의 쿨타임이 30% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp049, TFTAbilityPoint.Once, TFTAbilityType.GoodSense, "👓")]
public class GoodSense : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp049Role scp049)
        {
            scp049.GoodSenseCooldown *= 0.7f;
        }
    }
}
