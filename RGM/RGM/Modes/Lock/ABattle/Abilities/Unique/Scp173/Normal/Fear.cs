using System.Linq;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Scp173;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp173.Normal;

[Ability("공포", "텔레포트 시 주변 상대를 1초간 속박시킵니다.", 
    AbilityCategory.Normal, AbilityType.NORMAL_SCP173_FEAR, RoleAbility.Scp173)]
public class Fear : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp173.Blinking += OnBlinking;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp173.Blinking -= OnBlinking;
    }

    private void OnBlinking(BlinkingEventArgs ev)
    {
        if (ev.Player != Owner || !ev.IsAllowed)
            return;

        Timing.CallDelayed(Timing.WaitForOneFrame, () => ApplyFear(Owner.Position));
    }

    private void ApplyFear(Vector3 position)
    {
        foreach (var player in PlayerManager.List.Where(x => !x.IsNPC && !x.IsScpRole()))
        {
            if (Vector3.Distance(player.Position, position) <= 12)
            {
                player.EnableEffect(EffectType.Ensnared, 1, 1f * Owner.AbilityCount(AbilityType.NORMAL_SCP173_FEAR));
            }
        }
    }
}
