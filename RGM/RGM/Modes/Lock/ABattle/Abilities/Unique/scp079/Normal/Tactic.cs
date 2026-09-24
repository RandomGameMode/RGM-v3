using System.Linq;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Common;

[Ability("전략", "아군들에게 [일반] 유전 능력을 지급합니다.", 
    AbilityCategory.Normal, AbilityType.NORMAL_SCP079_TACTIC, RoleAbility.Scp079)]

public class Tactic : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive && x.Role != RoleTypeId.Scp079))
        {
            p.AddAbility(AbilityType.NORMAL_HEREDITY);
        }
    }
}