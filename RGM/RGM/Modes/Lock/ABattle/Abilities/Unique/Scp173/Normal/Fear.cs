using System.Linq;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp173.Normal;

[Ability("공포", "적 처치 시 주변 상대를 1초간 속박시킵니다.", 
    AbilityCategory.Normal, AbilityType.NORMAL_SCP173_FEAR, RoleAbility.Scp173)]
public class Fear : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Attacker == null || ev.Attacker != Owner)
            return;

        foreach (var player in PlayerManager.List.Where(x => !x.IsNPC && !x.IsScpRole()))
        {
            if (Vector3.Distance(player.Position, ev.Attacker.Position) <= 12)
            {
                player.EnableEffect(EffectType.Ensnared, 1, 1f * Owner.AbilityCount(AbilityType.NORMAL_SCP173_FEAR));
            }
        }
    }
}
