using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Legend;

/*[Ability("리저렉션",
    """
    죽은 아군 전체를 되살립니다. 자신의 진영이 SCP인 경우, 관전자 중 1명을 079와 049-2를 제외한 랜덤한 SCP로 부활시킵니다.
    부활 후, 자신이 가진 업그레이드 목록을 그대로 전수합니다.
    전용 능력과 일부 능력들은 전수되지 않으며, 전설 등급까지만 전수할 수 있습니다.
    """,
    AbilityCategory.Legend, AbilityType.LEGEND_RESURRECTION)]*/
public class Resurrection : Ability
{
    private static readonly List<RoleTypeId> RandomScpRoles =
    [
        .. Tools.EnumToList<RoleTypeId>()
            .Where(role => role.IsScp() && role is not RoleTypeId.Scp0492 and not RoleTypeId.Scp079)
    ];

    private static readonly List<AbilityType> ExceptAbilities =
    [
        AbilityType.LEGEND_RESURRECTION,
        AbilityType.EPIC_PRIEST,
        AbilityType.LEGEND_CATACLYSMGENERATOR,
        AbilityType.LEGEND_REPLICATION
    ];

    public override void OnEnabled()
    {
        Timing.RunCoroutine(Resurrect());
    }

    private IEnumerator<float> Resurrect()
    {
        List<AbilityType> transferableAbilities = Owner.GetAbilities()
            .Where(ability => ability.Data.RoleAbility == RoleAbility.None &&
                              ability.Data.Category is >= AbilityCategory.Normal and <= AbilityCategory.Legend &&
                              !ExceptAbilities.Contains(ability.Data.AbilityType))
            .Select(ability => ability.Data.AbilityType)
            .ToList();

        List<Player> deadPlayers = PlayerManager.List.Where(player => player.IsDead).ToList();
        List<Player> reviveTargets = Owner.IsScpRole()
            ? deadPlayers.Count > 0 ? [deadPlayers.GetRandomValue()] : []
            : deadPlayers.Where(player =>
                ABattle.Instance.LastDeathRoles.TryGetValue(player, out RoleTypeId deathRole) &&
                RoleExtensions.GetTeam(deathRole) == RoleExtensions.GetTeam(Owner.Role.Type))
                .ToList();

        foreach (Player target in reviveTargets)
        {
            RoleTypeId role = Owner.IsScpRole()
                ? RandomScpRoles.GetRandomValue()
                : Owner.Role.Type;

            target.Role.Set(role, RoleSpawnFlags.None);
            target.Position = Owner.Position;
        }

        yield return Timing.WaitForSeconds(0.1f);

        foreach (Player target in reviveTargets)
        {
            if (!target.IsAlive)
                continue;

            foreach (AbilityType ability in transferableAbilities)
                ABattle.Instance.AddAbility(target, ability, allowReflector: false);
        }
    }
}