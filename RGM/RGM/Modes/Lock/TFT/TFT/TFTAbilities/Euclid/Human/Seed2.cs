using Exiled.API.Features;
using PlayerRoles;

namespace RGM.Modes.Lock.TFT.Euclid.Human;

[TFTAbility("꿈나무+", "자신 진영의 지원 토큰을 1 추가합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Seed2, "🌳")]
public class Seed2 : TFTAbility
{
    public override void OnEnabled()
    {
        Respawn.GrantTokens(Owner.Role.Team.GetFaction(), 1);
    }
}
