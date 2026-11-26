using Exiled.API.Features.Roles;

namespace RGM.Modes.Lock.TFT.Euclid.Scp079;

[TFTAbility("어두컴컴", "구역 정전ㅣ능력의 쿨타임이 50% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp079, TFTAbilityPoint.Once, TFTAbilityType.BlackoutZone, "💡")]
public class BlackoutZone : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp079Role scp079)
        {
            scp079.BlackoutZoneCooldown *= 0.5f;
        }
    }
}
