using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp079;
using Player = Exiled.API.Features.Player;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Exiled.API.Features.Roles;

namespace RGM.Modes.Abilities.Unique.Scp079.Epic;

[Ability("끈질김", "사망 시 부활합니다. SCP가 모두 죽으면 부활하지 않습니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_SCP079_IMPORTUNITY, RoleAbility.Scp079)]
public class Importunity : Ability
{


    int tier;
    int experience;
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void OnDisabled()
    {

        Exiled.Events.Handlers.Player.Died -= OnDied;
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        if (Owner.Role is not Scp079Role scp079) return;
        tier = scp079.Level;
        experience = scp079.Experience;
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Player != Owner)
            return;
        Timing.CallDelayed(3f, () =>
        {
            List<Player> ScpListWithOut79 = PlayerManager.List.Where(x => x.IsScpRole() && x.Role.Type != RoleTypeId.Scp079).ToList();
            if (ScpListWithOut79.Count <= 0)
            {
                return;
            }

            Owner.Role.Set(RoleTypeId.Scp079, RoleSpawnFlags.None);

            if (Owner.Role is not Scp079Role scp079) return;
            scp079.Level = tier;
            scp079.Experience = experience;
        });
    }
}
