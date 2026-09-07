using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace RGM.Modes.Abilities.Epic;

[Ability("수어사이드 봄버맨", "사망할 경우 즉시 폭발합니다. 44% 확률로 연쇄 폭발이 일어납니다.", AbilityCategory.Epic, AbilityType.EPIC_SUICIDEBOMBER)]
public class SuicideBomber : Ability
{
    public override void OnEnabled()
        => Exiled.Events.Handlers.Player.Dying += OnDying;

    public override void OnDisabled()
        => Exiled.Events.Handlers.Player.Dying -= OnDying;

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        Vector3 pos = Owner.Position;
        RoleTypeId roleId = Owner.Role.Type;

        Timing.CallDelayed(0.1f, () =>
        {
            if (!Owner.IsDead) return;
            Owner.Role.Set(roleId, RoleSpawnFlags.None);
            Owner.Position = pos;

            var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Owner);
            g.FuseTime = 0.1f;
            g.SpawnActive(pos, Owner);

            while (Random.Range(1, 101) <= 44)
            {
                var chain = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Owner);
                chain.FuseTime = 0.1f;
                chain.SpawnActive(pos, Owner);
            }

            Owner.Kill(ev.DamageHandler);
        });
    }
}