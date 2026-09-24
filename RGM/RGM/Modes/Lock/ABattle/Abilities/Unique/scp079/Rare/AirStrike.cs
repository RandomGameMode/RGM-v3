using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Scp079;
using MEC;
using UnityEngine;
using LabApi.Features.Wrappers;

namespace RGM.Modes.Abilities.Unique.Scp079.Rare;

[Ability("폭격", "다음 핑의 위치에 고폭 수류탄을 투척합니다.", AbilityCategory.Rare, AbilityType.RARE_SCP079_AIRSTRIKE, RoleAbility.Scp079)]
public class AirStrike : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp079.Pinging += OnPinging;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;
    }

    private void OnPinging(PingingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        Timing.CallDelayed(0.1f, () =>
        {
            var g = (ExplosiveGrenade)Exiled.API.Features.Items.Item.Create(ItemType.GrenadeHE, ev.Player);
            g.SpawnActive(ev.Position, ev.Player);
            if (!Owner.HasAbility(AbilityType.EPIC_SCP079_SURPRISEATTACK))
            {
                LightSourceToy light = LightSourceToy.Create(ev.Position);
                light.Position = ev.Position;
                light.Range = 5;
                light.Color = new Color(1, 0, 0, 1);
                light.Rotation = Quaternion.Euler(0, 0, 0);

                Timing.CallDelayed(5, light.Destroy);
            }
            OnDisabled();
        });
    }
}
