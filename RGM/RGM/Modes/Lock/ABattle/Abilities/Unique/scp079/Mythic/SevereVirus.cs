using RGM.API.Features;
using System.Linq;
using Exiled.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Mythic;

[Ability("치명적인 바이러스", "아군들에게 랜덤 <color=#ffd700>전설</color> 능력 2개를 지급합니다.",
    AbilityCategory.Mythic, AbilityType.MYTHIC_SCP079_SEVEREVIRUS, RoleAbility.Scp079)]
public class SevereVirus : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive && x != Owner))
            GetRandomAbility(p);
    }

    private static void GetRandomAbility(Player player)
    {
        if (player.IsNonePlayer()) return;
        var randomAbility = ABattle.Instance.GetRandomAbilities(player, AbilityCategory.Legend, 2);

        foreach (var ability in randomAbility) player.AddAbility(ability);
    }
}