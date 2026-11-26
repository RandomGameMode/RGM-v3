using Exiled.API.Features.Roles;

namespace RGM.Modes.Lock.TFT.Euclid.Scp079;

[TFTAbility("폐소공포증", "폐쇄ㅣ능력의 쿨타임이 25% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp079, TFTAbilityPoint.Once, TFTAbilityType.RoomLockdown, "🔒")]
public class RoomLockdown : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp079Role scp079)
        {
            scp079.RoomLockdownCooldown *= 0.75f;
        }
    }
}
