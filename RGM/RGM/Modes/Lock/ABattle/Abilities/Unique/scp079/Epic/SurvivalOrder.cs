using PlayerRoles;
using RGM.API.Features;
using System.Linq;

namespace RGM.Modes.Abilities.Unique.Scp079.Epic;

[Ability("생존 명령", "아군들에게 [<color=#FF00FF>영웅</color>]구사일생, [일반] 보험 능력을 지급합니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_SCP079_SURVIVALORDER, RoleAbility.Scp079)]
public class SurvivalOrder : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive && x.Role != RoleTypeId.Scp079))
        {
            p.AddAbility(AbilityType.EPIC_SURVIVOR);
            p.AddAbility(AbilityType.NORMAL_INSURANCE);
        }
    }
}
