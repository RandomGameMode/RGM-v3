using System.Linq;
using Exiled.API.Features;
using PlayerRoles;
using Respawning;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Human.Epic;

[Ability("긴급 지원", """
                  즉시 본인 진영의 정규 지원을 소환합니다. 해당 지원은 지원 토큰을 소모하지 않습니다.
                  단, 20% 확률로 진영을 잘못 고를 수가 있으며, 5% 확률로 뱀의 손이 도착합니다!
                  """,
    AbilityCategory.Epic, AbilityType.EPIC_HUMAN_URGENTSUPPORT, RoleAbility.Human)]
public class UrgentSupport : Ability
{
    public override void OnEnabled()
    {
        if (Owner.Role.Type == RoleTypeId.Tutorial)
        {
            EventArgs.ServerEvents.CallTutorialSupport(Player.List.Where(player => player.IsDead));
            return;
        }

        var faction = Owner.Role.Type.GetFaction();
        
        if (Random.Range(1, 101) <= 20 && faction == Faction.FoundationStaff)
        {
            faction = Faction.FoundationEnemy;
        }
        else if (Random.Range(1, 101) <= 20 && faction == Faction.FoundationEnemy)
        {
            faction = Faction.FoundationStaff;
        }
        
        if (Owner.IsScpRole()) return;

        Respawn.GrantTokens(faction, 1);

        if (WaveManager.TryGet(faction, out var wave))
            WaveManager.Spawn(wave);
    }
}