using System;
using RGM.API.Features;
using System.Linq;
using Random = UnityEngine.Random;
    
namespace RGM.Modes.Abilities.Unique.Scp079.Mythic;

/*[Ability("치명적인 바이러스", """
                      아군들에게 랜덤 전설 등급 1개를 지급(25% 확률로 추가 지급)하며, [<color=#ffd700>전설</color>] 상급 변이 능력을 3개 지급합니다.
                      자신은 [<color=#FF00FF>영웅</color>]변이 능력을 3개 얻습니다.
                      """, 
    AbilityCategory.Mythic, AbilityType.MYTHIC_SCP079_SEVEREVIRUS, RoleAbility.Scp079)]*/
public class SevereVirus : Ability
{
    public override void OnEnabled()
    {
        foreach (var p in PlayerManager.List.Where(x => x.LeadingTeam == Owner.LeadingTeam && x.IsAlive))
        {
            if (p == Owner)
            {
                for (int i = 0; i < 3; i++)
                {
                    p.AddAbility(AbilityType.EPIC_TRANSITION);
                }
                continue;
            }

            for (int i = 0; i < 3; i++)
            {
                p.AddAbility(AbilityType.LEGEND_TRANSITION);
            }

            p.AddAbility(ABattle.Instance.GetRandomAbilities(p, AbilityCategory.Legend, 1)[0]);
            if (Convert.ToByte(Random.Range(1, 101)) <= 25)
            {
                p.AddAbility(ABattle.Instance.GetRandomAbilities(p, AbilityCategory.Legend, 1)[0]);
            }
        }
    }
}