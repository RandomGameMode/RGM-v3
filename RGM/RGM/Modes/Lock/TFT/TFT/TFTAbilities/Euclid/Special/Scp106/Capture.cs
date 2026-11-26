using Exiled.API.Features.Roles;

namespace RGM.Modes.Lock.TFT.Euclid.Scp106;

[TFTAbility("잡았다 요놈", "포착ㅣ능력의 쿨타임이 25% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp106, TFTAbilityPoint.Once, TFTAbilityType.Capture, "📷")]
public class Capture : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp106Role scp106)
        {
            scp106.CaptureCooldown *= 0.75f;
        }
    }
}
