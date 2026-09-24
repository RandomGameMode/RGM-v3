using System.Linq;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Epic;

[Ability("신속 지원", "아군들에게 [일반] 경공 능력 3개를 지급합니다.",
    AbilityCategory.Epic, AbilityType.EPIC_SCP079_SWIFTSUPPORT, RoleAbility.Scp079)]
public class SwiftSupport : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive && x.Role != RoleTypeId.Scp079))
        {
            for (int i = 0; i < 3; i++)
            {
                p.AddAbility(AbilityType.NORMAL_SWIFT);
            }
        }
    }
}
