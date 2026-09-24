using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;

namespace RGM.Modes.Abilities.Unique.Scp096.Normal;

[Ability("격노", "분노 시 받는 피해가 30% 줄어듭니다.", AbilityCategory.Normal, AbilityType.NORMAL_SCP096_RAGE, RoleAbility.Scp096)]
public class Rage : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        if (Owner.Role is not Scp096Role scp096) return;
        if (scp096.RageManager.IsEnraged)
        {
            ev.Amount *= 0.7f;
        }
    }
}
