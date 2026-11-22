using Exiled.API.Enums;

namespace DAONTFT.Core.TFT.Keter.Human;

[TFTAbility("악마와의 계약", "죽기 전까지 투명해지는 대신 최대 체력이 60% 감소합니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.DevilContract, "👿")]
public class DevilContract : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.MaxHealth *= 0.4f;
        Owner.Health *= 0.4f;
        Owner.EnableEffect(EffectType.Fade, 255);
        Owner.EnableEffect(EffectType.SilentWalk, 11);
    }

    public override void OnDisabled()
    {
        Owner.DisableEffect(EffectType.Fade);
        Owner.DisableEffect(EffectType.SilentWalk);
    }
}
