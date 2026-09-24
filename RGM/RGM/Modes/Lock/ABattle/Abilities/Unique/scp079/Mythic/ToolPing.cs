using Exiled.Events.EventArgs.Scp079;
using MEC;
using ProjectMER.Features.Serializable;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp079.Mythic;

[Ability("따아알깍", "핑을 찍으면 워크스테이션을 설치합니다. (쿨타임 8초)", AbilityCategory.Mythic, AbilityType.MYTHIC_SCP079_TOOLPING, RoleAbility.Scp079)]
public class ToolPing : Ability
{
    private bool _isScp079Cooldown;
    
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
        if (ev.Player == null || ev.Player != Owner) return;
        if (!_isScp079Cooldown)
        {
            Timing.CallDelayed(0.1f, () =>
            {
                new SerializableWorkstation
                {
                    IsInteractable = true,
                    Position = ev.Position,
                    Rotation = new Vector3(0, 0, 0),
                    Scale = Vector3.one
                }.SpawnOrUpdateObject();
                _isScp079Cooldown = true;

                Timing.CallDelayed(8f, () =>
                {
                    _isScp079Cooldown = false;
                });
            });
        }
    }
}