using Exiled.API.Features.Roles;

namespace RGM.Modes.Lock.TFT.Euclid.Scp173;

[TFTAbility("눈 깜빡할 사이에", "깜빡ㅣ능력의 쿨타임이 25% 감소합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp173, TFTAbilityPoint.Once, TFTAbilityType.Blink, "👀")]
public class Blink : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.Role is Scp173Role scp173)
        {
            scp173.BlinkCooldown *= 0.75f;
        }
    }
}
