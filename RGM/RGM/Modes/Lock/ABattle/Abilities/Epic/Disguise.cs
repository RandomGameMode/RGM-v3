using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using MEC;

using PlayerRoles;
using Respawning.Objectives;
using RGM.API.DataBases;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Epic;

[Ability("위장술", "항상 게임에서 유리한 세력의 모습을 지니게 됩니다.", AbilityCategory.Epic, AbilityType.EPIC_DISGUISE)]
public class Disguise : Ability
{
    private readonly List<RoleTypeId> _blockedRoles =
    [
        RoleTypeId.Spectator,
        RoleTypeId.Scp079,
        RoleTypeId.Overwatch,
        RoleTypeId.Filmmaker,
        RoleTypeId.CustomRole,
        RoleTypeId.Destroyed,
        RoleTypeId.Scp3114,
        RoleTypeId.Flamingo,
        RoleTypeId.AlphaFlamingo,
        RoleTypeId.ChaosFlamingo,
        RoleTypeId.NtfFlamingo,
        RoleTypeId.ZombieFlamingo
    ];
    
    private CoroutineHandle _disguise;

    public override void OnEnabled()
        => _disguise = Timing.RunCoroutine(disguise());

    public override void OnDisabled() => Timing.KillCoroutines(_disguise);

    private IEnumerator<float> disguise()
    {
        Team? currentTeam = null;
        
        while (Owner.IsAlive)
        {
            var teams = new Dictionary<Team, int>();

            foreach (var player in PlayerManager.List.Where(x => x.IsAlive))
            {
                if (player.Role.Team is Team.Dead or Team.OtherAlive) continue;
                if (!teams.ContainsKey(player.Role.Team))
                    teams[player.Role.Team] = 0;

                teams[player.Role.Team]++;
            }

            var mostCommonTeam = teams.OrderByDescending(fc => fc.Value).FirstOrDefault().Key;

            if (currentTeam != mostCommonTeam)
            {
                currentTeam = mostCommonTeam;

                RoleTypeId role = Tools.EnumToList<RoleTypeId>().Where(x => RoleExtensions.GetTeam(x) == mostCommonTeam && !_blockedRoles.Contains(x)).ToList().GetRandomValue();

                Owner.ChangeAppearance(role);
                Owner.AddBroadcast(10, $"<size=20><color={role.GetRoleColor().ToHex()}>{Trans.Role[role]}</color>(으)로 변장했습니다.</size>");
            }
            yield return Timing.WaitForSeconds(1);
        }
    }
}
