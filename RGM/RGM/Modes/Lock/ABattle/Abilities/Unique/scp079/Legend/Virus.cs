using RGM.API.Features;
using System.Linq;
using Exiled.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Legend;

[Ability("바이러스", "자신을 제외한 아군들에게 랜덤 <color=#FF00FF>영웅</color>능력 3개를 지급하며, [<color=#FF00FF>영웅</color>] 변이 능력을 1개 지급합니다.",
    AbilityCategory.Legend, AbilityType.LEGEND_SCP079_VIRUS, RoleAbility.Scp079)]
public class Virus : Ability
{
    public override void OnEnabled()
    {
        foreach (var player in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive && x != Owner))
        {
            GetRandomAbility(player);
            player.AddAbility(AbilityType.EPIC_TRANSITION);
        }
    }

    private static void GetRandomAbility(Player player)
    {
        if (player.IsNonePlayer()) return;
        var randomAbility = ABattle.Instance.GetRandomAbilities(player, AbilityCategory.Epic, 3);
        
        foreach (var ability in randomAbility) player.AddAbility(ability);
    }
}