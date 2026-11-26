using System.Collections.Generic;
using Exiled.API.Extensions;
using Exiled.API.Features;
using PlayerRoles;

namespace RGM.Modes.Lock.TFT.Safe.Human;

[TFTAbility("꿈나무", "자신의 진영의 지원 영향력을 20 추가합니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Seed1, "🌳")]
public class Seed1 : TFTAbility
{
    public override void OnEnabled()
    {
        Respawn.GrantInfluence(Owner.Role.Team.GetFaction(), 20);
    }
}
