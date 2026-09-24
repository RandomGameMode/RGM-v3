using RGM.API.Features;
using System.Linq;
using UnityEngine;
using PlayerRoles;

namespace RGM.Modes.Abilities.Unique.Scp079.Legend;

[Ability("바이러스", "아군들에게 [<color=#FF00FF>영웅</color>] 변이 능력을 2개 지급합니다.",
    AbilityCategory.Legend, AbilityType.LEGEND_SCP079_VIRUS, RoleAbility.Scp079)]
public class Virus : Ability
{
    public override void OnEnabled()
    {
        foreach (var player in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive))
        {
            for (int i = 0; i < 2; i++)
            {
                player.AddAbility(AbilityType.EPIC_TRANSITION);
            }
        }
    }
}