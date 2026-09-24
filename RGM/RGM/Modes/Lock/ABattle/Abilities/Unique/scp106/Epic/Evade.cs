using Exiled.Events.EventArgs.Scp106;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp106.Epic;

[Ability("긴급 탈출", 
    "SCP-106의 스킬에 『긴급 탈출』 효과가 적용됩니다.", 
    AbilityCategory.Epic,
    AbilityType.EPIC_SCP106_EVADE,
    RoleAbility.Scp106)]

public class Evade : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp106.Stalking +=  OnStalking;
        Exiled.Events.Handlers.Scp106.Teleporting += OnTeleporting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp106.Stalking -= OnStalking;
        Exiled.Events.Handlers.Scp106.Teleporting -= OnTeleporting;
    }

    private void OnStalking(StalkingEventArgs ev)
    {
        if (ev.Player != Owner || !ev.IsAllowed) return;
        Owner.ApplyGodMode(2.4f);
    }

    private void OnTeleporting(TeleportingEventArgs ev)
    {
        if (ev.Player != Owner || !ev.IsAllowed) return;
        Owner.ApplyGodMode(2.4f);
    }
}