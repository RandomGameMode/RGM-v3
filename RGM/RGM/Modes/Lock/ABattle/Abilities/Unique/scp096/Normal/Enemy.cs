using RGM.API.Features;
using Exiled.Events.EventArgs.Scp096;

namespace RGM.Modes.Abilities.Unique.Scp096.Rare;

[Ability("원수", "분노 상태 돌입 시 무적 효과가 적용됩니다.",
    AbilityCategory.Normal, AbilityType.NORMAL_SCP096_ENEMY, RoleAbility.Scp096)]

public class Enemy : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp096.Enraging += OnEnraging;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp096.Enraging -= OnEnraging;
    }

    private void OnEnraging(EnragingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;
        
        ev.Player.ApplyGodMode(5);
    }
}
