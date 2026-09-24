using PlayerRoles;
using RGM.API.Features;
using System.Linq;

namespace RGM.Modes.Abilities.Unique.Scp079.Legend;

[Ability("가호", "자신을 포함한 아군들에게 [<color=#FF00FF>영웅</color>]럭키비키, [일반] 행운 능력을 지급합니다.(79는 럭키비키만 지급)", 
    AbilityCategory.Legend, AbilityType.LEGEND_SCP079_BLESSING, RoleAbility.Scp079)]
public class Blessing : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive))
        {
            p.AddAbility(AbilityType.EPIC_LUCKYVIKEY);
            if (p.Role != RoleTypeId.Scp079)  p.AddAbility(AbilityType.NORMAL_LUCKY);
        }
    }
}
