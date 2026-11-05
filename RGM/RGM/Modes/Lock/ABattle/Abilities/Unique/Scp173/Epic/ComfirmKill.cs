using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp173.Epic;

[Ability("확인 사살", 
    "SCP-173의 기본 공격에 『사망』 효과가 적용됩니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_SCP173_CONFIRMKILL, RoleAbility.Scp173)]

public class ComfirmKill : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        // ApplyInstantKill's own damage raises Hurting again; ignore it to avoid infinite recursion.
        if (ApplyInstantKill.IsApplying ||
            ev.Attacker != Owner ||
            ev.Player == Owner)
            return;

        Timing.RunCoroutine(KillProcess(ev.Player, ev.DamageHandler));
    }

    private static IEnumerator<float> KillProcess(Player player, CustomDamageHandler handler)
    {
        while (!player.IsDead)
        {
            yield return Timing.WaitForOneFrame;
            player.Kill(handler);
        }
    }
}