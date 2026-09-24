using Exiled.Events.EventArgs.Scp173;

namespace RGM.Modes.Abilities.Unique.Scp173.Normal;

[Ability("괴이", "순간이동한 방이 3초 동안 정전됩니다.", AbilityCategory.Normal, AbilityType.NORMAL_SCP173_ABERRATION, RoleAbility.Scp173)]
public class Aberration : Ability
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
        if (ev.Player != Owner)
            return;

        if (!ev.Player.CurrentRoom.AreLightsOff)
            ev.Player.CurrentRoom.TurnOffLights(3 * ev.Player.AbilityCount(AbilityType.NORMAL_SCP173_ABERRATION));
    }
}
