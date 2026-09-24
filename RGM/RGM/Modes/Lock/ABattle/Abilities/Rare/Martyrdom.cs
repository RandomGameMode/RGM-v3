using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;

namespace RGM.Modes.Abilities.Rare;

[Ability("순교", "사망할 시 해당 지역에 점화된 수류탄을 떨굽니다.", AbilityCategory.Rare, AbilityType.RARE_MARTYRDOM)]
public class Martyrdom : Ability
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
        if (ev.Player != Owner)
            return;

        Vector3 pos = ev.Player.Position;

        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            if (!ev.Player.IsDead) return;
            var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, ev.Player);
            g.FuseTime = 2f;
            g.SpawnActive(pos, ev.Player);
        });
    }
}
