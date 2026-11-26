using Exiled.API.Features.Roles;

namespace RGM.Modes.Lock.TFT.Safe.Scp079;

[TFTAbility("간이 충전기", "즉시 70의 경험치를 획득합니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Scp079, TFTAbilityPoint.Once, TFTAbilityType.Charger, "✨")]
public class Charger : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp079Role scp079Role)
        {
            scp079Role.AddExperience(70);
        }
    }
}
