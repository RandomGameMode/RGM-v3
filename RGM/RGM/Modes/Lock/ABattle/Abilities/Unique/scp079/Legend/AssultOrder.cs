using System.Linq;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Legend;

[Ability("돌격 명령", "아군들에게 [<color=#FF00FF>영웅</color>]람보, [<color=#FF00FF>영웅</color>]샤프 아이즈, [<color=#FF00FF>영웅</color>]몰락한 왕의 검, [<color=#FF00FF>영웅</color>]홀리 프로텍션 능력을 지급합니다.", 
    AbilityCategory.Legend, AbilityType.LEGEND_SCP079_ASSULTORDER, RoleAbility.Scp079)]

public class AssultOrder : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive && x.Role != RoleTypeId.Scp079)) {
            p.AddAbility(AbilityType.EPIC_RAMBO);
            p.AddAbility(AbilityType.EPIC_FALLENKINGSSWORD);
            p.AddAbility(AbilityType.EPIC_SHARPEYES);
            p.AddAbility(AbilityType.EPIC_HOLYPROTECTION);
        }
    }
}