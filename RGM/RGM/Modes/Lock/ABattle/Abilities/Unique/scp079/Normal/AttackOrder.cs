using System.Linq;
using RGM.API.Features;
using PlayerRoles;

namespace RGM.Modes.Abilities.Unique.Scp079.Common;

[Ability("공격 명령", "아군들에게 [일반] 단련 능력을 지급합니다.", 
    AbilityCategory.Normal, AbilityType.NORMAL_SCP079_ATTACKORDER, RoleAbility.Scp079)]
public class AttackOrder : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive && x.Role != RoleTypeId.Scp079))
        {
            p.AddAbility(AbilityType.NORMAL_TRAINING);
        }
    }
}

