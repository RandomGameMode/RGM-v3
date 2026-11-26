using Exiled.API.Features.Roles;

namespace RGM.Modes.Lock.TFT.Euclid.Scp049;

[TFTAbility("심부름", "의사의 부름ㅣ능력의 쿨타임이 30% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp049, TFTAbilityPoint.Once, TFTAbilityType.Call, "👓")]
public class Call : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp049Role scp049)
        {
            scp049.CallCooldown *= 0.7f;
        }
    }
}
