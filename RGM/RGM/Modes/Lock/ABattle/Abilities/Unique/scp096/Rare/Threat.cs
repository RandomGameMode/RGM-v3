using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features.Roles;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp096.Rare;

[Ability("위협", "SCP-096 주변 12m에 접근한 적을 강제로 목격 대상으로 포함합니다.",
    AbilityCategory.Rare, AbilityType.RARE_SCP096_THREAT, RoleAbility.Scp096)]

public class Threat : Ability
{
    private const float Range = 12f;
    private readonly HashSet<Exiled.API.Features.Player> _targets = [];
    private CoroutineHandle _targetRefreshCoroutine;

    public override void OnEnabled()
    {
        _targetRefreshCoroutine = Timing.RunCoroutine(RefreshTargets());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_targetRefreshCoroutine);
        _targets.Clear();
    }

    private IEnumerator<float> RefreshTargets()
    {
        while (true)
        {
            if (Owner.Role is Scp096Role scp096)
                AddNearbyTargets(scp096);

            yield return Timing.WaitForSeconds(0.25f);
        }
    }

    private void AddNearbyTargets(Scp096Role scp096)
    {
        foreach (var player in PlayerManager.List.Where(player =>
                     player.IsHuman &&
                     Vector3.Distance(player.Position, Owner.Position) <= Range &&
                     _targets.Add(player)))
        {
            scp096.AddTarget(player);
            player.AddHint("위협",
                $"<color={ABattle.RatingColor["희귀"]}>위협</color>에 의해 강제로 목격자에 포함되었습니다. 도망가세요!");
        }
    }
}