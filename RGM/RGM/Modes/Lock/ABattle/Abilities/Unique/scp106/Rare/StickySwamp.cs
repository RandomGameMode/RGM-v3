using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp106.Rare;

[Ability("끈적한 늪", "4.5m 내의 인간들을 느리게 만듭니다.",
    AbilityCategory.Rare, AbilityType.RARE_SCP106_STICKYSWAMP, RoleAbility.Scp106)]
public class StickySwamp : Ability
{
    private CoroutineHandle _stickySwamp1;

    public override void OnEnabled()
    {
        _stickySwamp1 = Timing.RunCoroutine(StickySwamp1());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_stickySwamp1);
    }

    private IEnumerator<float> StickySwamp1()
    {
        while (true)
        {
            foreach (var near in PlayerManager.List.Where(x => x.IsAlive && Vector3.Distance(x.Position, Owner.Position) <= 4.5f))
            {
                if (Owner != near && HitboxIdentity.IsEnemy(Owner.ReferenceHub, near.ReferenceHub))
                    near.EnableEffect(EffectType.SinkHole, 1, 0.3f);
            }

            yield return Timing.WaitForSeconds(1f);
        }
    }
}
