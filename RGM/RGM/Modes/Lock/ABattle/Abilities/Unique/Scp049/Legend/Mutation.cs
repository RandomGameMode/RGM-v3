using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp049;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Unique.Scp049.Legend;

[Ability("돌연변이",
    """
    049-2 소생 시, 해당 개체가 15% 확률로 자기 자신과 SCP-079를 제외한 랜덤한 SCP로 변이합니다.
    이 효과는 [전용 희귀] 능수능란 으로 생성한 049-2 에도 적용됩니다.
    """,
    AbilityCategory.Legend,
    AbilityType.LEGEND_SCP049_MUTATION,
    RoleAbility.Scp049)]

public class Mutation : Ability
{
    private const byte MutationChance = 15;
    private const float ProficiencyReviveDelay = 0.2f;

    private static readonly List<RoleTypeId> MutationRoles = Tools.EnumToList<RoleTypeId>()
        .Where(role => role.IsScp() && role is not RoleTypeId.Scp0492 and not RoleTypeId.Scp079)
        .ToList();

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp049.FinishingRecall += OnFinishingRecall;
        Exiled.Events.Handlers.Player.Died += OnDied;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp049.FinishingRecall -= OnFinishingRecall;
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnFinishingRecall(FinishingRecallEventArgs ev)
    {
        if (ev.Player == Owner)
            TryMutate(ev.Target);
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Attacker != Owner ||
            ev.Player == Owner ||
            !Owner.HasAbility(AbilityType.RARE_SCP049_PROFICIENCY))
            return;

        if (Convert.ToByte(Random.Range(1, 101)) > MutationChance)
            return;

        RoleTypeId mutationRole = MutationRoles.GetRandomValue();

        // 능수능란은 사망 이벤트 후 0.1초에 049-2로 변경하므로, 그 뒤에 변이시킨다.
        Timing.CallDelayed(ProficiencyReviveDelay, () => SetMutationRole(ev.Player, mutationRole));
    }

    private static void TryMutate(Exiled.API.Features.Player target)
    {
        if (Convert.ToByte(Random.Range(1, 101)) > MutationChance)
            return;

        RoleTypeId mutationRole = MutationRoles.GetRandomValue();
        Timing.CallDelayed(Timing.WaitForOneFrame, () => SetMutationRole(target, mutationRole));
    }

    private static void SetMutationRole(Exiled.API.Features.Player target, RoleTypeId mutationRole)
    {
        if (target is { IsAlive: true } && target.Role.Type == RoleTypeId.Scp0492)
            target.Role.Set(mutationRole, RoleSpawnFlags.None);
    }
}