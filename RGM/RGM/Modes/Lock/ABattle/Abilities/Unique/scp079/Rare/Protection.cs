using PlayerRoles;
using RGM.API.Features;
using System.Linq;

namespace RGM.Modes.Abilities.Unique.Scp079.Rare;

[Ability("보호", "아군들에게 [<color=#2ECCFA>희귀</color>] 하이패스, [<color=#2ECCFA>희귀</color>] 강첩껍질 능력 3개 지급합니다.",
    AbilityCategory.Rare, AbilityType.RARE_SCP079_PROTECTION, RoleAbility.Scp079)]
public class Protection : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive && x.Role != RoleTypeId.Scp079))
        {
            for (int i = 0; i < 3; i++)
            {
                p.AddAbility(AbilityType.RARE_STEELSHELL);
            }
            p.AddAbility(AbilityType.RARE_HYPASS);
        }
    }
}
