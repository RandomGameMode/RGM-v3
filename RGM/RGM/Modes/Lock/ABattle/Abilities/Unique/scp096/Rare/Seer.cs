using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Scp096;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp096.Rare;

[Ability("천리안", "분노 중 36m 내의 인간들을 목격자에 포함시킵니다.", 
    AbilityCategory.Rare, AbilityType.RARE_SCP096_SEER, RoleAbility.Scp096)]

public class Seer : Ability
{
    private readonly HashSet<Exiled.API.Features.Player> _targets = [];
    private CoroutineHandle _targetRefreshCoroutine;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp096.Enraging += OnEnraging;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp096.Enraging -= OnEnraging;
        Timing.KillCoroutines(_targetRefreshCoroutine);
        _targets.Clear();
    }

    private void OnEnraging(EnragingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        if (Timing.IsRunning(_targetRefreshCoroutine))
            Timing.KillCoroutines(_targetRefreshCoroutine);

        _targets.Clear();
        AddTargets(ev.Scp096);
        _targetRefreshCoroutine = Timing.RunCoroutine(RefreshTargets());
    }

    private IEnumerator<float> RefreshTargets()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(1f);

            if (Owner.Role is not Scp096Role scp096 || !scp096.RageManager.IsEnraged)
                yield break;

            AddTargets(scp096);
        }
    }

    private void AddTargets(Scp096Role scp096)
    {
        int count = 0;

        foreach (var player in PlayerManager.List.Where(player =>
                     player.IsHuman &&
                     Vector3.Distance(player.Position, Owner.Position) < 37 &&
                     _targets.Add(player)))
        {
            count++;
            scp096.AddTarget(player);
            player.AddHint("천리안", $"<color={ABattle.RatingColor["희귀"]}>천리안</color>에 의해 강제로 목격자에 포함되었습니다. 도망가세요!");
        }

        if (count > 0)
            Owner.AddHint("천리안", $"<color={ABattle.RatingColor["희귀"]}>천리안</color> 능력으로 {count}명의 인간들을 추가로 탐색했습니다.");
    }
}