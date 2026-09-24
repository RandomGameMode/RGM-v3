using System.Linq;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Common;

[Ability("지도", "아군들에게 [일반] 공부 능력을 지급합니다.", 
    AbilityCategory.Normal, AbilityType.NORMAL_SCP079_INSTRUCT, RoleAbility.Scp079)]

public class Instruct : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive && x.Role != RoleTypeId.Scp079))
        {
            p.AddAbility(AbilityType.NORMAL_STUDY);
        }
    }
}